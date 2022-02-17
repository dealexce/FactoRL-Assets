using System;
using System.Collections.Generic;
using System.Xml;
using Multi;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class PlaneController : MonoBehaviour
    {
        public string configPath;
        public string prefabPath;
        
        //关联对象
        public GameObject ground;
        public Ground groundController;
        public GameObject importPrefab;
        public GameObject exportPrefab;
        public GameObject MFWMPrefab;

        public GameObject testModel;
        public Bounds testBound;
        
        public Dictionary<string, GameObject> ItemPrefabSet { get; private set; }
        public Dictionary<int, Tuple<string, string, int>> ProcessSet { get; private set; }
        
        //Ground paras
        private float minX, maxX, minZ, maxZ;
        public float curX=30f, curZ=20f;
        public bool randomGroundSize = false;
        
        
        //生产对象列表
        private Dictionary<GameObject, MonoBehaviour> EpisodeObjects = new Dictionary<GameObject, MonoBehaviour>(); 
        private void Start()
        {
            groundController = ground.GetComponent<Ground>();
            //加载XML配置文件
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(configPath);
            XmlNode envNode = xmlDocument.SelectSingleNode("env");
            
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
            // //根据XML配置生成Item:ItemPrefab表，用于场景中的物体生成
            // ItemPrefabSet = new Dictionary<string, GameObject>();
            // foreach (XmlNode node in envNode.SelectSingleNode("itemtypes").ChildNodes)
            // {
            //     XmlElement e = (XmlElement) node;
            //     string type = node.InnerText;
            //     GameObject p = (GameObject) Resources.Load(prefabPath + "\\" + type + ".prefab");
            //     ItemPrefabSet.Add(type, p);
            // }
            //
            //根据XML配置的processes用三元组数组记录所有的process
            ProcessSet = new Dictionary<int, Tuple<string, string, int>>();
            foreach (XmlNode node in envNode.SelectSingleNode("workflow").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                Tuple<string, string, int> p = new Tuple<string, string, int>(
                    e.GetAttribute("input"),
                    e.GetAttribute("output"),
                    Int32.Parse(e.GetAttribute("duration")));
                ProcessSet.Add(Int32.Parse(e.GetAttribute("id")), p);
            }
            //
            // //TODO:根据XML配置的raws和products生成原料口和交付口
            // foreach (XmlNode node in envNode.SelectSingleNode("raws").ChildNodes)
            // {
            //     XmlElement e = (XmlElement) node;
            //     //TODO:位置需要随机生成
            //     GameObject g = Instantiate(importPrefab, this.transform);
            //     AddColliderAroundChildren(g);
            //     ImportController ic = g.GetComponent<ImportController>();
            //     ic.rawType = node.InnerText;
            //     ic._planeController = this;
            // }
            // foreach (XmlNode node in envNode.SelectSingleNode("products").ChildNodes)
            // {
            //     XmlElement e = (XmlElement) node;
            //     //TODO:位置需要随机生成
            //     GameObject g = Instantiate(exportPrefab, this.transform);
            //     ExportController ec = g.GetComponent<ExportController>();
            //     ec.productType = node.InnerText;
            //     ec._planeController = this;
            // }

            //TODO:根据XML配置的workstations生成场景中的Workstation
            foreach (XmlNode node in envNode.SelectSingleNode("workstations").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                GameObject g = Instantiate(MFWMPrefab, this.transform);
                MFWS controller = g.GetComponent<MFWS>();
                controller._planeController = this;
                //设置缓冲区容量
                controller.inputBufferCapacity = Int32.Parse(e.GetAttribute("inputcapacity"));
                controller.outputBufferCapacity = Int32.Parse(e.GetAttribute("outputcapacity"));
                //设置可执行process集合
                Dictionary<int, Tuple<string, string, int>> processes = new Dictionary<int, Tuple<string, string, int>>();
                foreach (XmlNode processNode in e.SelectSingleNode("processes").ChildNodes)
                {
                    XmlElement processElement = (XmlElement) processNode;
                    int pid = Int32.Parse(processElement.GetAttribute("id"));
                    processes.Add(pid,ProcessSet[pid]);
                    controller.supportInputs.Add(ProcessSet[pid].Item1);
                }
                controller.supportProcesses = processes;
                EpisodeObjects.Add(g,controller);
            }
            
            ResetGround();
        }

        private void OnDrawGizmos()
        {
            foreach (var obj in EpisodeObjects.Keys)
            {
                Bounds bounds = GetEncapsulateBounds(obj);
                if(Physics.OverlapBox(bounds.center, bounds.extents).Length>3)
                    Gizmos.DrawCube(bounds.center,bounds.size);
                Debug.Log(obj.transform.position+"|"+bounds.extents);
            }
        }

        private void Update()
        {
            
        }

        public void ResetGround()
        {
            foreach (var kv in EpisodeObjects)
            {
                GenerateSafeResetRandomPosition(kv.Key);
                if (kv.Value.GetType().IsAssignableFrom(typeof(Resetable)))
                {
                    (kv.Value as Resetable).Reset();
                }
            }
        }

        private Bounds GetEncapsulateBounds(GameObject assetModel)
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

        private void GenerateSafeResetRandomPosition(GameObject g)
        {

            int remainAttempts = 100;
            bool safePositionFound = false;
            var potentialPosition = Vector3.zero;
            Bounds bounds = GetEncapsulateBounds(g);
            while (!safePositionFound && remainAttempts>0)
            {
                //TODO:计算y值为多少时物体刚好贴地
                potentialPosition = new Vector3(Random.Range(-curX/2f, curX/2f),.15f, Random.Range(-curZ/2f, curZ/2f));
                potentialPosition = transform.position + potentialPosition;
                remainAttempts--;
                Collider[] colliders = Physics.OverlapBox(potentialPosition, bounds.extents);

                safePositionFound = colliders.Length == 0;
            }
            if (safePositionFound)
            {
                //TODO:计算y值为多少时物体刚好贴地
                potentialPosition = new Vector3(potentialPosition.x, transform.position.y+transform.position.y - bounds.min.y, potentialPosition.z);
                g.transform.position = potentialPosition;
                //更改物体位置之后要手动调用Physics.Simulate，否则可能无法检测到碰撞！
                Physics.autoSimulation=false;
                Physics.Simulate(Time.fixedDeltaTime);
                Physics.autoSimulation = true;
            }
            else
            {
                Debug.LogError("Unable to find a safe position to reset work point");
            }
        }
    }
}
