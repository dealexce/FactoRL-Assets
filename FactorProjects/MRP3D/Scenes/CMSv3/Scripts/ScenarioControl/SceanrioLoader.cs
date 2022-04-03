using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class SceanrioLoader
    {
        private static Scenario _scenario=null;

        public static List<ItemState> RawItemStates { get; } = new List<ItemState>();
        public static List<ItemState> ProductItemStates { get; } = new List<ItemState>();
        public static Dictionary<string, ItemState> ItemStateDict { get; } = new Dictionary<string, ItemState>();
        private static Dictionary<string, Process> ProcessDict = new Dictionary<string, Process>();
        private static Dictionary<string, Workstation> WorkstationDict = new Dictionary<string, Workstation>();
        public static void Load(string path)
        {
            if (_scenario != null)
            {
                Debug.LogWarning("Already loaded a scenario, overwriting current scenario");
            }
            XmlSerializer serializer = new XmlSerializer(typeof(Scenario));
            using (StreamReader reader = new StreamReader(path))
            {
                _scenario = serializer.Deserialize(reader) as Scenario;
            }
            if (_scenario == null)
            {
                throw new Exception("Failed to load scenario XML");
                return;
            }
            foreach (var i in _scenario.model.itemStates)
            {
                ItemStateDict.Add(i.id,i);
                if (i.type == SpecialItemStateType.Raw)
                {
                    RawItemStates.Add(i);
                }
                if (i.type == SpecialItemStateType.Product)
                {
                    ProductItemStates.Add(i);
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
