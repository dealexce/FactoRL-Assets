using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        
        //Prefabs
        public GameObject importPrefab;
        public GameObject exportPrefab;
        public GameObject MFWMPrefab;
        public GameObject itemPrefab;
        public GameObject AGVPrefab;

        //Group
        public float timePenalty = 3f;
        private SimpleMultiAgentGroup _simpleMultiAgentGroup = new SimpleMultiAgentGroup();

        /// <summary>
        /// XML中定义的物料类型, k=0 ~ NullItem
        /// </summary>
        public List<string> ItemTypeList { get; private set; } = new List<string>(){PConsts.NullItem};
        public Dictionary<string,int> ItemTypeIndexDict { get; private set; }

        /// <summary>
        /// XML中定义的Processes, k->pid, v->Process, pid=0 ~ NullProcess
        /// </summary>
        public List<Process> ProcessList { get; private set; } = new List<Process>(){PConsts.NullProcess};
        public Dictionary<Process,int> ProcessIndexDict { get; private set; }

        /// <summary>
        /// 所有有效的[targetGameObject, targetItem]组合, k=0 ~ NullTarget
        /// </summary>
        public List<Target> TargetCombinationList { get; private set; } = new List<Target>(){PConsts.NullTarget};
        public Dictionary<Target,int> TargetCombinationIndexDict { get; private set; }

        /// <summary>
        /// 存放GameObject的ItemHolder组件
        /// </summary>
        public Dictionary<GameObject, ItemHolder> GameObjectItemHolderDict { get; private set; } = new Dictionary<GameObject, ItemHolder>();
        public List<MFWSController> MFWSControllers { get; private set; } = new List<MFWSController>();
        public List<AGVController> AGVControllers { get; private set; } = new List<AGVController>();

        #region Ground parameters
        /// <summary>
        /// XML配置的场地随机尺寸的长宽随机范围
        /// </summary>
        private float minX, maxX, minZ, maxZ;
        /// <summary>
        /// 是否在每次场地重置时随机场地尺寸
        /// </summary>
        public bool randomGroundSize = false;
        /// <summary>
        /// 当前场地尺寸
        /// </summary>
        public float curX=30f, curZ=20f;
        #endregion

        #region Normalization values
        /// <summary>
        /// 当前场地尺寸下的外接圆直径，场地中两物体距离最大不会超过该值，用于观测值归一化
        /// </summary>
        public float MAXDiameter { get; private set; }
        public float MAXDuration { get; private set; }
        public float MAXCapacity { get; private set; }
        #endregion

        static PlaneController()
        {
            //加载XML配置文件
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(configPath);
            envNode = xmlDocument.SelectSingleNode("env");
            Debug.Log("XML LOADED");
        }
        
        //生产对象列表
        private Dictionary<GameObject, MonoBehaviour> EpisodeResetObjects = new Dictionary<GameObject, MonoBehaviour>(); 
        private void Start()
        {

            #region Ground Config
            groundController = ground.GetComponent<Ground>();
            //配置ground随机尺寸范围
            XmlElement groundElement = (XmlElement) envNode.SelectSingleNode("ground");
            minX = float.Parse(groundElement.GetAttribute("minX")); 
            maxX = float.Parse(groundElement.GetAttribute("maxX")); 
            minZ = float.Parse(groundElement.GetAttribute("minZ")); 
            maxZ = float.Parse(groundElement.GetAttribute("maxZ"));
            //随机本episode的ground尺寸
            if (randomGroundSize)
            {
                curX = Random.Range(minX, maxX);
                curZ = Random.Range(minZ, maxZ);
            }
            groundController.changeSize(curX,curZ);
            #endregion

            #region Normalization used values
            //计算场地内最长对角线距离，用于归一化
            MAXDiameter = (float)Math.Sqrt(Math.Pow(maxX, 2) + Math.Pow(maxZ, 2));
            XmlElement normElement = (XmlElement) envNode.SelectSingleNode("norm");
            MAXCapacity = Int32.Parse(normElement.GetAttribute("maxCapacity"));
            MAXDuration = Int32.Parse(normElement.GetAttribute("maxDuration"));
            #endregion

            #region ItemTypeList config
            //根据XML配置生成ItemType集合，用于场景中的物体生成
            foreach (XmlNode node in envNode.SelectSingleNode("itemtypes").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                string type = node.InnerText;
                ItemTypeList.Add(type);
            }
            #endregion

            #region ProcessList config
            //根据XML配置的processes用三元组数组记录所有的process
            foreach (XmlNode node in envNode.SelectSingleNode("workflow").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                Process p = new Process(
                    Int32.Parse(e.GetAttribute("id")),
                    e.GetAttribute("input"),
                    e.GetAttribute("output"),
                    Int32.Parse(e.GetAttribute("duration")));
                ProcessList.Add(p);
            }
            #endregion

            #region Import/Outport Config
            //配置Import原料口
            foreach (XmlNode node in envNode.SelectSingleNode("raws").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                GameObject g = Instantiate(importPrefab, this.transform);
                ImportController ic = g.GetComponent<ImportController>();
                ic._planeController = this;
                ic.rawType = node.InnerText;
                EpisodeResetObjects.Add(g,ic);
                GameObjectItemHolderDict.Add(g, ic);
                TargetCombinationList.Add(new Target(g,TargetAction.Get,ic.rawType));
            }
            //配置Export出货口
            foreach (XmlNode node in envNode.SelectSingleNode("products").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                GameObject g = Instantiate(exportPrefab, this.transform);
                ExportController ec = g.GetComponent<ExportController>();
                ec._planeController = this;
                ec.productType = node.InnerText;
                ec.supportInputs.Add(ec.productType);
                EpisodeResetObjects.Add(g,ec);
                GameObjectItemHolderDict.Add(g, ec);
                TargetCombinationList.Add(new Target(g,TargetAction.Give,ec.productType));
            }
            #endregion

            #region MFWS Config
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
                TargetCombinationList.Add(new Target(
                    controller.inputPlate,
                    TargetAction.Give,
                    PConsts.AnyItem));
                foreach (XmlNode processNode in e.SelectSingleNode("processes").ChildNodes)
                {
                    XmlElement processElement = (XmlElement) processNode;
                    int pid = Int32.Parse(processElement.GetAttribute("id"));
                    controller.supportProcessId.Add(pid);
                    if (!controller.supportInputs.Contains(ProcessList[pid].inputType))
                    {
                        controller.supportInputs.Add(ProcessList[pid].inputType);
                        TargetCombinationList.Add(new Target(
                            controller.outputPlate,
                            TargetAction.Get,
                            ProcessList[pid].outputType));  //代表从这个WS的outputPlate拿outputType的物体
                    }
                }
                controller.isMultiFunctional = controller.supportProcessId.Count > 1;
                EpisodeResetObjects.Add(g,controller);
                GameObjectItemHolderDict.Add(controller.inputPlate,controller);
                GameObjectItemHolderDict.Add(controller.outputPlate, controller);
                MFWSControllers.Add(controller);
                //TargetableGameObjectItemHolderDict.Add(g, controller);
            }
            #endregion

            #region AGV config
            //根据XML配置的agvs生成场景中的AGV
            foreach (XmlNode node in envNode.SelectSingleNode("agvs").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                GameObject AGV = Instantiate(AGVPrefab, transform);
                AGVController agvController = AGV.GetComponent<AGVController>();
                agvController._planeController = this;
                AGVDispatcherAgent dpAgent = agvController.agvDispatcherAgent;
                _simpleMultiAgentGroup.RegisterAgent(dpAgent);
                EpisodeResetObjects.Add(AGV,agvController);
                AGVControllers.Add(agvController);
            }
            #endregion

            #region Reverse Index Dictionary Initialize

            ItemTypeIndexDict = Utils.ToIndexDict(ItemTypeList);
            ProcessIndexDict = Utils.ToIndexDict(ProcessList);
            TargetCombinationIndexDict = Utils.ToIndexDict(TargetCombinationList);

            #endregion
            
            PrintAgentSpaceInfo();
            ResetGround(true);

            foreach (var a in AGVControllers)
            {
                a.activateAward = true;
            }
        }

        //TODO:计算并打印当前XML配置下各个Agent的观测和动作空间大小
        public void PrintAgentSpaceInfo()
        {
            Debug.Log("AGV Dispatcher Act Size: "+(TargetCombinationList.Count+1));
            Debug.Log("AGV Dispatcher Obs Size: "+(GameObjectItemHolderDict.Keys.Count*2+MFWSControllers.Count*2+AGVControllers.Count*TargetCombinationList.Count));
            Debug.Log("MFWS Act Size: "+(ProcessList.Count+1));
            Debug.Log("MFWS Obs Size: "+(ProcessList.Count+1));
        }

        private void OnDrawGizmos()
        {
            // foreach (var obj in EpisodeResetObjects.Keys)
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
            foreach (var kv in EpisodeResetObjects)
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
            if (ItemTypeList.Contains(itemType))
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
