using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using Multi;
using Unity.MLAgents;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class PlaneController : MonoBehaviour
    {
        
        public int maxEnvSteps = 50000;
        [SerializeField]
        [InspectorUtil.DisplayOnly]
        private int resetTimerStep = 0;
        
        public static string configPath = "Assets/FactorProjects/MRP3D/Scenes/CMSv2/config.xml";
        //XML Config Element
        public static XmlNode EnvNode { get; private set; }
        
        //evaluate
        public bool showEva = false;
        public static int TotalFinished = 0;
        public static int TotalFailed = 0;
        public static int EpisodeCount = 0;

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
        private SimpleMultiAgentGroup _AGVMultiAgentGroup = new SimpleMultiAgentGroup();
        private SimpleMultiAgentGroup _MFWSMultiAgentGroup = new SimpleMultiAgentGroup();

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
        
        public List<string> ProductItemTypeList { get; } = new List<string>();
        public List<ExportController> ExportControllerList = new List<ExportController>();
        public Dictionary<string, ExportController> ProductTypeExportControllerDict = new Dictionary<string, ExportController>();
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
        public float curX=50f, curZ=50f;
        #endregion

        #region Normalization values
        /// <summary>
        /// 当前场地尺寸下的外接圆直径，场地中两物体距离最大不会超过该值，用于观测值归一化
        /// </summary>
        public float MAXDiameter { get; private set; }
        public float MAXDuration { get; private set; }
        public float MAXCapacity { get; private set; }
        #endregion

        /// <summary>
        /// 是否生成新的positions并覆盖positionPath下的文件
        /// </summary>
        public static bool CoverNewPositions = false;
        public static List<Vector3> positions;
        public static string positionPath = "Assets/FactorProjects/MRP3D/Scenes/CMSv2/positions";
        static PlaneController()
        {
            //加载XML配置文件
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(configPath);
            EnvNode = xmlDocument.SelectSingleNode("env");
            Debug.Log("XML LOADED");
            //如果不要覆盖生成新的positions，并且存在position二进制文件，作为List<Vector3>加载
            if (!CoverNewPositions&&File.Exists(positionPath))
            {
                using (FileStream fs = new FileStream(positionPath, FileMode.Open))
                {
                    try
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        SurrogateSelector ss = new SurrogateSelector();
                        var streamingContext = new StreamingContext(StreamingContextStates.All);
                        ss.AddSurrogate(typeof(Vector3), streamingContext, new Vector3SerializationSurrogate());
                        bf.SurrogateSelector = ss;
                        positions = bf.Deserialize(fs) as List<Vector3>;
                    }
                    catch (Exception e)
                    {
                        positions = null;
                        Console.WriteLine(e);
                        throw;
                    }

                }
            }
        }
        
        
        //生产对象列表
        private List<GameObject> EpisodeResetObjectsList = new List<GameObject>();
        private Dictionary<GameObject, MonoBehaviour> EpisodeResetObjectsDict = new Dictionary<GameObject, MonoBehaviour>();
        public bool showMachine = true;
        public string machinePrefabPath = "Machines";
        private GameObject[] MachinePrefabs;
        private void Start()
        {

            #region Ground Config
            groundController = ground.GetComponent<Ground>();
            //配置ground随机尺寸范围
            XmlElement groundElement = (XmlElement) EnvNode.SelectSingleNode("ground");
            minX = float.Parse(groundElement.GetAttribute("minX")); 
            maxX = float.Parse(groundElement.GetAttribute("maxX")); 
            minZ = float.Parse(groundElement.GetAttribute("minZ")); 
            maxZ = float.Parse(groundElement.GetAttribute("maxZ"));
            //随机本episode的ground尺寸
            if (randomPositions&&randomGroundSize)
            {
                curX = Random.Range(minX, maxX);
                curZ = Random.Range(minZ, maxZ);
            }
            groundController.changeSize(curX,curZ);
            #endregion

            #region Normalization used values
            //计算场地内最长对角线距离，用于归一化
            MAXDiameter = (float)Math.Sqrt(Math.Pow(maxX, 2) + Math.Pow(maxZ, 2));
            XmlElement normElement = (XmlElement) EnvNode.SelectSingleNode("norm");
            MAXCapacity = Int32.Parse(normElement.GetAttribute("maxCapacity"));
            MAXDuration = Int32.Parse(normElement.GetAttribute("maxDuration"));
            #endregion

            #region ItemTypeList config
            //根据XML配置生成ItemType集合，用于场景中的物体生成
            foreach (XmlNode node in EnvNode.SelectSingleNode("itemtypes").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                string type = node.InnerText;
                ItemTypeList.Add(type);
            }
            #endregion

            #region ProcessList config
            //根据XML配置的processes用三元组数组记录所有的process
            foreach (XmlNode node in EnvNode.SelectSingleNode("workflow").ChildNodes)
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
            foreach (XmlNode node in EnvNode.SelectSingleNode("raws").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                GameObject g = Instantiate(importPrefab, this.transform);
                ImportController ic = g.GetComponent<ImportController>();
                ic._planeController = this;
                ic.rawType = node.InnerText;
                EpisodeResetObjectsList.Add(g);
                EpisodeResetObjectsDict.Add(g,ic);
                GameObjectItemHolderDict.Add(g, ic);
                TargetCombinationList.Add(new Target(g,TargetAction.Get,ic.rawType));
            }
            //配置Export出货口
            foreach (XmlNode node in EnvNode.SelectSingleNode("products").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                GameObject g = Instantiate(exportPrefab, this.transform);
                ExportController ec = g.GetComponent<ExportController>();
                ec._planeController = this;
                string productItemType = node.InnerText;
                ec.productType = productItemType;
                ProductItemTypeList.Add(productItemType);
                ExportControllerList.Add(ec);
                ProductTypeExportControllerDict.Add(productItemType,ec);
                ec.supportInputs.Add(ec.productType);
                EpisodeResetObjectsList.Add(g);
                EpisodeResetObjectsDict.Add(g,ec);
                GameObjectItemHolderDict.Add(g, ec);
                TargetCombinationList.Add(new Target(g,TargetAction.Give,ec.productType));
            }
            #endregion

            #region MFWS Config

            MachinePrefabs = Resources.LoadAll<GameObject>(machinePrefabPath);
            //根据XML配置的workstations生成场景中的Workstation
            foreach (XmlNode node in EnvNode.SelectSingleNode("workstations").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                GameObject g = Instantiate(MFWMPrefab, transform);
                int id = Int32.Parse(e.GetAttribute("id"))-1;
                if (showMachine)
                {
                    Instantiate(MachinePrefabs[Math.Clamp(id, 0, MachinePrefabs.Length)], g.transform);
                }
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
                //待机action
                controller.supportProcessIndex.Add(0);
                foreach (XmlNode processNode in e.SelectSingleNode("processes").ChildNodes)
                {
                    XmlElement processElement = (XmlElement) processNode;
                    int pid = Int32.Parse(processElement.GetAttribute("id"));
                    controller.supportProcessIndex.Add(pid);
                    if(!controller.inputSet.Contains(ProcessList[pid].inputType))
                        controller.inputSet.Add(ProcessList[pid].inputType);
                    if(!controller.outputSet.Contains(ProcessList[pid].outputType))
                        controller.outputSet.Add(ProcessList[pid].outputType);
                    if (!controller.supportInputs.Contains(ProcessList[pid].inputType))
                    {
                        controller.supportInputs.Add(ProcessList[pid].inputType);
                        TargetCombinationList.Add(new Target(
                            controller.inputPlate,
                            TargetAction.Give,
                            ProcessList[pid].inputType));
                        TargetCombinationList.Add(new Target(
                            controller.outputPlate,
                            TargetAction.Get,
                            ProcessList[pid].outputType));  //代表从这个WS的outputPlate拿outputType的物体
                    }
                }
                controller.isMultiFunctional = controller.supportProcessIndex.Count > 2;
                EpisodeResetObjectsList.Add(g);
                EpisodeResetObjectsDict.Add(g,controller);
                GameObjectItemHolderDict.Add(controller.inputPlate,controller);
                GameObjectItemHolderDict.Add(controller.outputPlate, controller);
                MFWSControllers.Add(controller);
                
                _MFWSMultiAgentGroup.RegisterAgent(controller.mfwsAgent);
            }
            #endregion

            #region AGV config
            //根据XML配置的agvs生成场景中的AGV
            foreach (XmlNode node in EnvNode.SelectSingleNode("agvs").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                GameObject AGV = Instantiate(AGVPrefab, transform);
                AGVController agvController = AGV.GetComponent<AGVController>();
                agvController._planeController = this;
                AGVDispatcherAgent dpAgent = agvController.agvDispatcherAgent;
                _AGVMultiAgentGroup.RegisterAgent(dpAgent);
                EpisodeResetObjectsList.Add(AGV);
                EpisodeResetObjectsDict.Add(AGV,agvController);
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
            Debug.Log("AGV Dispatcher Act Size: "+(TargetCombinationList.Count));
            Debug.Log("MFWS Act Size: "+(ProcessList.Count));
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
            //_AGVMultiAgentGroup.AddGroupReward(-timePenalty/(float)maxEnvSteps);
            if (resetTimerStep >= maxEnvSteps && maxEnvSteps > 0)
            {
                ResetGround(false);
                resetTimerStep = 0;
            }
            var removeList = new List<float>();
            foreach (var (k,v) in OrderList)
            {
                if (v.deadLine > Time.fixedTime)
                {
                    break;
                }
                removeList.Add(k);
                OrderFailed();
            }
            foreach (var rk in removeList)
            {
                OrderList.Remove(rk);
            }
            ReloadOrders();
            if(showEva)
                RefreshOrderText();
        }
        
                
        //Orders
        public int OrderWindowLength = 5;
        [SerializeField]
        [InspectorUtil.DisplayOnly]
        public int finishedOrders = 0, failedOrders = 0;
        public SortedList<float,Order> OrderList = new SortedList<float,Order>();

        /// <summary>
        /// 生成随机产品+ddl在随机30~60秒之后的订单填满OrderList
        /// </summary>
        private void ReloadOrders()
        {
            while (OrderList.Count < OrderWindowLength)
            {
                Order o = new Order(ProductItemTypeList[Random.Range(0, ProductItemTypeList.Count)], GetRandomNonConflictDeadline());
                while (ProductTypeExportControllerDict[o.productItemType].stock > 0)
                {
                    ProductTypeExportControllerDict[o.productItemType].RemoveOneStock();
                    OrderFinished();
                    o = new Order(ProductItemTypeList[Random.Range(0, ProductItemTypeList.Count)], GetRandomNonConflictDeadline());
                }
                OrderList.Add(o.deadLine,o);
            }
        }

        private void RefreshOrderText()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var (ddl,order) in OrderList)
            {
                sb.Append(order.productItemType + " IN " + (ddl - Time.fixedTime).ToString("F1")+'\n');
            }
            groundController.changeText(sb.ToString());
        }

        public float minDeadline { get; } = 60f;
        public float maxDeadline { get; } = 120f;
        private float GetRandomNonConflictDeadline()
        {
            float ddl = Time.fixedTime + Random.Range(minDeadline, maxDeadline);
            while (OrderList.ContainsKey(ddl))
            {
                ddl = Time.fixedTime + Random.Range(minDeadline, maxDeadline);
            }
            return ddl;
        }

        public bool TryFinishOrder(string productItemType)
        {
            foreach (var (k,v) in OrderList)
            {
                if (v.productItemType == productItemType)
                {
                    OrderList.Remove(k);
                    OrderFinished();
                    ReloadOrders();
                    return true;
                }
            }
            return false;
        }
        
        private void OrderFinished()
        {
            _AGVMultiAgentGroup.AddGroupReward(.1f);
            groundController.FlipColor(Ground.GroundSwitchColor.Green);
            finishedOrders++;
        }
        
        private void OrderFailed()
        {
            _AGVMultiAgentGroup.AddGroupReward(-.1f);
            groundController.FlipColor(Ground.GroundSwitchColor.Red);
            failedOrders++;
        }

        public bool randomPositions = false;
        public void ResetGround(bool init)
        {
            OrderList.Clear();
            ReloadOrders();
            if (!init)
            {
                //随机本episode的ground尺寸
                if (randomPositions&&randomGroundSize)
                {
                    curX = Random.Range(minX, maxX);
                    curZ = Random.Range(minZ, maxZ);
                    groundController.changeSize(curX,curZ);
                }
                _AGVMultiAgentGroup.GroupEpisodeInterrupted();
                _MFWSMultiAgentGroup.GroupEpisodeInterrupted();
                
                CalculateAndPrintAverageDPE();
                
            }
            finishedOrders = 0;
            failedOrders = 0;
            //如果随机位置，则直接随机重置所有对象的位置
            if (randomPositions)
            {
                foreach (var o in EpisodeResetObjectsList)
                {
                    ResetToSafeRandomPosition(o);
                    if (!init && EpisodeResetObjectsDict[o] is Resetable)
                    {
                        (EpisodeResetObjectsDict[o] as Resetable).EpisodeReset();
                    }
                }
                return;
            }
            //如果positions为空或者无效，生成一组新的所有物体的随机位置并序列化存到positionPath中
            bool initPositions = positions == null || positions.Count!=EpisodeResetObjectsList.Count;
            if (initPositions)
            {
                positions = new List<Vector3>();
            }
            if (initPositions)
            {
                foreach (var o in EpisodeResetObjectsList)
                {
                    Vector3 position = ResetToSafeRandomPosition(o);
                    positions.Add(position-transform.position);
                    if (!init && EpisodeResetObjectsDict[o] is Resetable)
                    {
                        (EpisodeResetObjectsDict[o] as Resetable).EpisodeReset();
                    }
                }
                using (FileStream fs = new FileStream(positionPath, FileMode.Create))
                {
                    var bf = new BinaryFormatter();
                    SurrogateSelector ss = new SurrogateSelector();
                    var streamingContext = new StreamingContext(StreamingContextStates.All);
                    ss.AddSurrogate(typeof(Vector3), streamingContext, new Vector3SerializationSurrogate());
                    bf.SurrogateSelector = ss;
                    bf.Serialize(fs,positions);
                }
            }
            else
            {
                for (int i = 0; i < EpisodeResetObjectsList.Count; i++)
                {
                    GameObject o = EpisodeResetObjectsList[i];
                    var c = EpisodeResetObjectsDict[o] as Resetable;
                    o.transform.position = positions[i]+transform.position;
                    if (!init && c!=null)
                    {
                        c.EpisodeReset();
                    }
                }
            }

        }

        private void CalculateAndPrintAverageDPE()
        {
            //计算场均DPE
            TotalFinished += finishedOrders;
            TotalFailed += failedOrders;
            EpisodeCount++;
            if(showEva)
                Debug.Log("EPISODE: "+EpisodeCount+
                      " | average Finished: "+(float)TotalFinished/EpisodeCount+
                      " | average Failed: "+(float)TotalFailed/EpisodeCount);
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
        private Vector3 ResetToSafeRandomPosition(GameObject g)
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
            return potentialPosition;
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


    }
}
