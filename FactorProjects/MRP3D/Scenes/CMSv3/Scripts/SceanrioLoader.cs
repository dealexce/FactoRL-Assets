using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class SceanrioLoader
    {
        private static string ModelConfigPath = "Assets/FactorProjects/MRP3D/Scenes/CMSv3/config/Scenario01-init.xml";
        private static Scenario _scenario=null;
        private static Dictionary<string, ItemState> ItemStateDict = new Dictionary<string, ItemState>();
        private static Dictionary<string, Process> ProcessDict = new Dictionary<string, Process>();
        private static Dictionary<string, Workstation> WorkstationDict = new Dictionary<string, Workstation>();
        public static void Load(string path)
        {
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
