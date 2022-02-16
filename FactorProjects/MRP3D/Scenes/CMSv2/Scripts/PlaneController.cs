using System;
using System.Collections.Generic;
using System.Xml;
using Multi;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class PlaneController : MonoBehaviour
    {
        public string configPath;
        public string prefabPath;

        public GameObject importPrefab;
        public GameObject exportPrefab;
        public GameObject MFWMPrefab;
        public Dictionary<string, GameObject> ItemPrefabSet { get; private set; }
        public Dictionary<int, Tuple<string, string, int>> ProcessSet { get; private set; }
        private void Start()
        {
            //加载XML配置文件
            configPath = Application.dataPath + configPath;
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(configPath);
            XmlNode envNode = xmlDocument.SelectSingleNode("env");
            
            //根据XML配置生成Item:ItemPrefab表，用于场景中的物体生成
            ItemPrefabSet = new Dictionary<string, GameObject>();
            foreach (XmlNode node in envNode.SelectSingleNode("itemtypes").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                string type = node.InnerText;
                GameObject p = (GameObject) Resources.Load(prefabPath + "\\" + type + ".prefab");
                ItemPrefabSet.Add(type, p);
            }
            
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
            
            //TODO:根据XML配置的raws和products生成原料口和交付口
            foreach (XmlNode node in envNode.SelectSingleNode("raws").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                //TODO:位置需要随机生成
                GameObject g = Instantiate(importPrefab, this.transform);
                ImportController ic = g.GetComponent<ImportController>();
                ic.rawType = node.InnerText;
                ic._planeController = this;
            }
            foreach (XmlNode node in envNode.SelectSingleNode("products").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                //TODO:位置需要随机生成
                GameObject g = Instantiate(exportPrefab, this.transform);
                ExportController ec = g.GetComponent<ExportController>();
                ec.productType = node.InnerText;
                ec._planeController = this;
            }
            
            //TODO:根据XML配置的workstations生成场景中的Workstation
            foreach (XmlNode node in envNode.SelectSingleNode("workstations").ChildNodes)
            {
                XmlElement e = (XmlElement) node;
                //TODO:位置需要随机生成
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
                }
            }
        }
    }
}
