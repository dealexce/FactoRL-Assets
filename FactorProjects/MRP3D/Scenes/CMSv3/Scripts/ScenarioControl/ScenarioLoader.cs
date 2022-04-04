using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class ScenarioLoader
    {
        private static Scenario _scenario=null;
        public static List<ItemState> RawItemStates { get; } = new List<ItemState>();
        public static List<ItemState> ProductItemStates { get; } = new List<ItemState>();
        public static Dictionary<string, ItemState> ItemStateDict { get; } = new Dictionary<string, ItemState>();
        private static Dictionary<string, Process> ProcessDict = new Dictionary<string, Process>();
        public static Dictionary<string, Workstation> WorkstationDict { get; } = new Dictionary<string, Workstation>();
        public static void Load(string path)
        {
            if (_scenario != null)
            {
                //Debug.Log("Already loaded a scenario, using existing scenario");
                return;
            }
            XmlSerializer serializer = new XmlSerializer(typeof(Scenario));
            using (StreamReader reader = new StreamReader(path))
            {
                _scenario = serializer.Deserialize(reader) as Scenario;
            }
            if (_scenario == null)
            {
                throw new Exception("Failed to load scenario XML");
            }
            foreach (var i in _scenario.model.itemStates)
            {
                ItemStateDict.Add(i.id,i);
                switch (i.type)
                {
                    case SpecialItemStateType.Raw:
                        RawItemStates.Add(i);
                        break;
                    case SpecialItemStateType.Product:
                        ProductItemStates.Add(i);
                        break;
                    case SpecialItemStateType.Mid:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            foreach (var p in _scenario.model.processes)
            {
                ProcessDict.Add(p.id,p);
            }
            foreach (var w in _scenario.model.workstations)
            {
                WorkstationDict.Add(w.id,w);
            }
        }

        public static Scenario getScenario()
        {
            return _scenario;
        }
        public static ItemState getItemState(string id)
        {
            return ItemStateDict[id];
        }
        public static Process getProcess(string id)
        {
            return ProcessDict[id];
        }
        public static Workstation getWorkstation(string id)
        {
            return WorkstationDict[id];
        }
    }
}
