using System;
using System.Collections.Generic;
using System.Xml;
using Multi;
using Unity.MLAgents;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class PlaneController : MonoBehaviour
    {
        public int maxEnvSteps = 25000;
        [SerializeField]
        [InspectorUtil.DisplayOnly]
        private int resetTimerStep = 0;

        [SerializeField]
        [InspectorUtil.DisplayOnly]
        private int deliveredCounts = 0;
        public static string configPath = "Assets/FactorProjects/MRP3D/Scenes/CMSv2/config.xml";
        //XML Config Element
        public static XmlNode envNode { get; private set; }
        
        //关联对象
        public GameObject ground;
        [HideInInspector]
        public Ground groundController;
        public GameObject importPrefab;
        public GameObject exportPrefab;
        public GameObject MFWMPrefab;
        public GameObject itemPrefab;
        public GameObject AGVPrefab;

        //Group
        public float timePenalty = 3f;
        private SimpleMultiAgentGroup _simpleMultiAgentGroup = new SimpleMultiAgentGroup();

        public HashSet<string> ItemSet { get; private set; }
        public Dictionary<int, Process> ProcessSet { get; private set; }
        public Dictionary<GameObject,ItemHolder> AvailableTargets { get; private set; }
        public List<Target> AvailableTargetCombination { get; private set; } = new List<Target>();
        public List<MFWSController> MfwsControllers { get; private set; } = new List<MFWSController>();

        public List<AGVController> AgentList { get; private set; } = new List<AGVController>();

        //Ground paras
        private float minX, maxX, minZ, maxZ;
        [InspectorUtil.DisplayOnly]
        public float maxDiameter;
        public bool randomGroundSize = false;
        public float curX=30f, curZ=20f;

        static PlaneController()
        {
            //加载XML配置文件
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(configPath);
            envNode = xmlDocument.SelectSingleNode("env");
            Debug.Log("XML LOADED");
        }
        
        //生产对象列表
        private Dictionary<GameObject, MonoBehaviour> EpisodeObjects = new Dictionary<GameObject, MonoBehaviour>(); 
        private void Start()
        {
            AvailableTargets = new Dictionary<GameObject, ItemHolder>();
            groundController = ground.GetComponent<Ground>();
            //配置ground随机尺寸范围
            XmlElement groundElement = (XmlElement) envNode.SelectSingleNode("ground");
            minX = float.Parse(groundElement.GetAttribute("minX")); 
            maxX = float.Parse(groundElement.GetAttribute("maxX")); 
            minZ = float.Parse(groundElement.GetAttribute("minZ")); 
            maxZ = float.Parse(groundElement.GetAttribute("maxZ"));
            //计算场地内最长对角线距离，用于归一化
            maxDiameter = (float)Math.Sqrt(Math.Pow(maxX, 2) + Math.Pow(maxZ, 2));
            
            //随机本episode的ground尺寸
            if (randomGroundSize)
            {
                curX = Random.Range(minX, maxX);
                curZ = Random.Range(minZ, maxZ);
            }
            
            groundController.changeSize(curX,curZ);
            //根据XML配置生成ItemType集合，用于场景中的物体生成
            ItemSet = new HashSet<string>();
            foreach (XmlNode node in envNode.SelectSingleNode("itemtypes").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                string type = node.InnerText;
                ItemSet.Add(type);
            }
            
            //根据XML配置的processes用三元组数组记录所有的process
            ProcessSet = new Dictionary<int, Process>();
            foreach (XmlNode node in envNode.SelectSingleNode("workflow").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                Process p = new Process(
                    Int32.Parse(e.GetAttribute("id")),
                    e.GetAttribute("input"),
                    e.GetAttribute("output"),
                    Int32.Parse(e.GetAttribute("duration")));
                ProcessSet.Add(p.pid, p);
            }
            
            
            //根据XML配置的raws和products生成原料口和交付口
            foreach (XmlNode node in envNode.SelectSingleNode("raws").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                GameObject g = Instantiate(importPrefab, this.transform);
                ImportController ic = g.GetComponent<ImportController>();
                ic.rawType = node.InnerText;
                ic._planeController = this;
                EpisodeObjects.Add(g,ic);
                AvailableTargets.Add(g, ic);
                AvailableTargetCombination.Add(new Target(g,ic.rawType));
            }
            foreach (XmlNode node in envNode.SelectSingleNode("products").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                GameObject g = Instantiate(exportPrefab, this.transform);
                ExportController ec = g.GetComponent<ExportController>();
                ec.productType = node.InnerText;
                ec._planeController = this;
                ec.supportInputs.Add(ec.productType);
                EpisodeObjects.Add(g,ec);
                AvailableTargets.Add(g, ec);
                AvailableTargetCombination.Add(new Target(g,null));
            }

            //根据XML配置的workstations生成场景中的Workstation
            foreach (XmlNode node in envNode.SelectSingleNode("workstations").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                GameObject g = Instantiate(MFWMPrefab, transform);
                MFWSController controller = g.GetComponent<MFWSController>();
                controller._planeController = this;
                //设置缓冲区容量
                controller.inputBufferCapacity = Int32.Parse(e.GetAttribute("inputcapacity"));
                controller.outputBufferCapacity = Int32.Parse(e.GetAttribute("outputcapacity"));
                //设置可执行process集合
                AvailableTargetCombination.Add(new Target(controller.inputPlate,null)); //代表把物体给这个WS的inputPlate
                foreach (XmlNode processNode in e.SelectSingleNode("processes").ChildNodes)
                {
                    XmlElement processElement = (XmlElement) processNode;
                    int pid = Int32.Parse(processElement.GetAttribute("id"));
                    controller.supportProcessId.Add(pid);
                    if (!controller.supportInputs.Contains(ProcessSet[pid].inputType))
                    {
                        controller.supportInputs.Add(ProcessSet[pid].inputType);
                        AvailableTargetCombination.Add(new Target(controller.outputPlate,ProcessSet[pid].outputType));  //代表从这个WS的outputPlate拿outputType的物体
                    }
                }
                EpisodeObjects.Add(g,controller);
                AvailableTargets.Add(controller.inputPlate,controller);
                AvailableTargets.Add(controller.outputPlate, controller);
                MfwsControllers.Add(controller);
                //AvailableTargetsItemHolderDict.Add(g, controller);
            }
            
            //根据XML配置的agvs生成场景中的AGV
            foreach (XmlNode node in envNode.SelectSingleNode("agvs").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                GameObject AGV = Instantiate(AGVPrefab, transform);
                AGVController da = AGV.GetComponent<AGVController>();
                AGVDispatcherAgent dpAgent = da.agvDispatcherAgent;
                _simpleMultiAgentGroup.RegisterAgent(dpAgent);
                da.planeController = this;
                EpisodeObjects.Add(AGV,da);
                AgentList.Add(da);
            }
            PrintAgentSpaceInfo();
            ResetGround(true);

            foreach (var a in AgentList)
            {
                a.activateAward = true;
            }
        }
        
        //TODO:计算并打印当前XML配置下各个Agent的观测和动作空间大小
        public void PrintAgentSpaceInfo()
        {
            Debug.Log("AGV Dispatcher Action Space: "+(AvailableTargetCombination.Count+1));
            Debug.Log("AGV Dispatcher Observ Space: "+(AvailableTargets.Keys.Count*2+MfwsControllers.Count*2));
            Debug.Log("MFWS Action Space: NOT IMPLEMENTED");
            Debug.Log("MFWS Observ Space: NOT IMPLEMENTED");
        }

        private void OnDrawGizmos()
        {
            // foreach (var obj in EpisodeObjects.Keys)
            // {
            //     Bounds bounds = GetEncapsulateBoxColliderBounds(obj);
            //     if(Physics.OverlapBox(bounds.center, bounds.extents).Length>3)
            //         Gizmos.DrawCube(bounds.center,bounds.size);
            //     Debug.Log(obj.transform.position+"|"+bounds.extents);
            // }
        }

        private void FixedUpdate()
        {
            resetTimerStep += 1;
            _simpleMultiAgentGroup.AddGroupReward(timePenalty/(float)maxEnvSteps);
            if (resetTimerStep >= maxEnvSteps && maxEnvSteps > 0)
            {
                ResetGround(false);
                resetTimerStep = 0;
            }
        }

        public void ResetGround(bool init)
        {
            if (!init)
            {
                //随机本episode的ground尺寸
                if (randomGroundSize)
                {
                    curX = Random.Range(minX, maxX);
                    curZ = Random.Range(minZ, maxZ);
                }
            
                groundController.changeSize(curX,curZ);
                _simpleMultiAgentGroup.GroupEpisodeInterrupted();
            }
            foreach (var kv in EpisodeObjects)
            {
                ResetToSafeRandomPosition(kv.Key);
                if (!init && kv.Value is Resetable)
                {
                    (kv.Value as Resetable).EpisodeReset();
                }
            }
            deliveredCounts = 0;
        }

        /// <summary>
        /// 获取该物体和该物体所有子物体的BoxCollider的外接正方体
        /// </summary>
        /// <param name="assetModel"></param>
        /// <returns></returns>
        private Bounds GetEncapsulateBoxColliderBounds(GameObject assetModel)
        {
            var pos = assetModel.transform.localPosition;
            var rot = assetModel.transform.localRotation;
            var scale = assetModel.transform.localScale;

            // need to clear out transforms while encapsulating bounds
            assetModel.transform.localPosition = Vector3.zero;
            assetModel.transform.localRotation = Quaternion.identity;
            assetModel.transform.localScale = Vector3.one;

            // start with root object's bounds
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            if (assetModel.transform.TryGetComponent<BoxCollider>(out var mainBoxCollider))
            {
                // as mentioned here https://forum.unity.com/threads/what-are-bounds.480975/
                // new Bounds() will include 0,0,0 which you may not want to Encapsulate
                // because the vertices of the mesh may be way off the model's origin
                // so instead start with the first renderer bounds and Encapsulate from there
                bounds = mainBoxCollider.bounds;
            }

            var descendants = assetModel.GetComponentsInChildren<Transform>();
            foreach (Transform desc in descendants)
            {
                if (desc.TryGetComponent<BoxCollider>(out var childBoxCollider))
                {
                    // use this trick to see if initialized to renderer bounds yet
                    // https://answers.unity.com/questions/724635/how-does-boundsencapsulate-work.html
                    if (bounds.extents == Vector3.zero)
                        bounds = childBoxCollider.bounds;
                    bounds.Encapsulate(childBoxCollider.bounds);
                }
            }

            // restore transforms
            assetModel.transform.localPosition = pos;
            assetModel.transform.localRotation = rot;
            assetModel.transform.localScale = scale;

            return bounds;
        }

        //TODO:旋转随机
        private void ResetToSafeRandomPosition(GameObject g)
        {
            //更改物体位置之后要手动调用Physics.Simulate，否则可能无法检测到碰撞！
            Physics.autoSimulation=false;
            Physics.Simulate(Time.fixedDeltaTime);
            Physics.autoSimulation = true;
            
            //100次以内尝试
            int remainAttempts = 100;
            bool safePositionFound = false;
            var potentialPosition = Vector3.zero;
            //获取碰撞盒外接正方体
            Bounds bounds = GetEncapsulateBoxColliderBounds(g);
            while (!safePositionFound && remainAttempts>0)
            {
                potentialPosition = new Vector3(Random.Range(-curX/2f, curX/2f),bounds.extents.y,Random.Range(-curZ/2f, curZ/2f));
                potentialPosition = transform.position + potentialPosition;
                remainAttempts--;
                LayerMask mask = LayerMask.GetMask("Default","Trigger");    //添加遮罩：只检测Default层
                Collider[] colliders = Physics.OverlapBox(potentialPosition, bounds.extents,Quaternion.identity,mask);

                safePositionFound = colliders.Length == 0;
            }
            if (safePositionFound)
            {
                potentialPosition = new Vector3(potentialPosition.x, transform.position.y+g.transform.position.y - bounds.min.y+0.1f, potentialPosition.z);
                g.transform.position = potentialPosition;
            }
            else
            {
                Debug.LogError("Unable to find a safe position to reset work point: "+g.name);
            }
        }

        public Item InstantiateItem(string itemType, GameObject parentObj)
        {
            if (ItemSet.Contains(itemType))
            {
                GameObject obj = Instantiate(itemPrefab, parentObj.transform);
                Item item = obj.GetComponent<Item>();
                item.setItemType(itemType);
                return item;
            }
            else
            {
                return null;
            }
        }

        public void ProductFinished()
        {
            _simpleMultiAgentGroup.AddGroupReward(1f);
            groundController.FlipGreen();
            deliveredCounts++;
        }
    }
}
