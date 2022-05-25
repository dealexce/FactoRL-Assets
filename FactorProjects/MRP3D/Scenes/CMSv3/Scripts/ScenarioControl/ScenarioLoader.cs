using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            InitProductOperationDict();
            InitProductItemPriorityDict();
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

        public static List<Process> GetDfsOperation(string id)
        {
            return productDfsOperationDict[id];
        }

        private static Dictionary<string, List<Process>> productDfsOperationDict;

        private static void InitProductOperationDict()
        {
            // Create outputProcessDict
            foreach (var p in ProcessDict.Values)
            {
                foreach (var iRef in p.outputItemsRef)
                {
                    outputProcessDict.Add(iRef.idref, p);
                }
            }

            productDfsOperationDict = new Dictionary<string, List<Process>>();
            foreach (var pi in ProductItemStates)
            {
                productDfsOperationDict.Add(pi.id, GenerateDfsOperations(pi.id));
            }
        }

        private static Dictionary<string, Process> outputProcessDict = new();
        /// <summary>
        /// Generate linked operations and linked transport to schedule for the product of given id
        /// </summary>
        /// <param name="id">product item id</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static List<Process> GenerateDfsOperations(string id)
        {
            var operationList = new List<Process>();
            GenerateDfsOperationTransportRecursive(id, operationList);
            return operationList;
        }

        private static void GenerateDfsOperationTransportRecursive(string outputId, in List<Process> operationList)
        {
            // Find the process whose output contains outputId
            var p = outputProcessDict[outputId];
            foreach (var item in p.inputItemsRef.Select(iRef=>ItemStateDict[iRef.idref]))
            {
                // If this input is raw, there is no pre operation for this input, so add a transport from raw to undetermined
                if (item.type == SpecialItemStateType.Raw)
                {
                    continue;
                }
                // Else go recursively to this input
                GenerateDfsOperationTransportRecursive(item.id, operationList);   
            }
            operationList.Add(p);

        }

        public static Dictionary<string, Dictionary<string, int>> ProductItemPriorityDict = new();
        private static void InitProductItemPriorityDict()
        {
            foreach (var (productId,processes) in productDfsOperationDict)
            {
                ProductItemPriorityDict.Add(productId,new Dictionary<string, int>());
                // item in later process have higher priority
                for (int i = processes.Count - 1; i >= 0; i--)
                {
                    var p = processes[i];
                    ProductItemPriorityDict[productId].AddOrUpdate(
                        p.outputItemsRef.Single().idref,
                        i,
                        Math.Max);
                    foreach (var iRef in p.inputItemsRef)
                    {
                        ProductItemPriorityDict[productId].AddOrUpdate(
                            iRef.idref,
                            0,
                            Math.Max);
                    }
                }
            }
        }
        

        // public static Dictionary<string, List<Target>> ProductRelateTargetsDict;
        // public static void InitProductRelateTargetsDict(List<Target> targets)
        // {
        //     if(ProductRelateTargetsDict!=null)
        //         return;
        //     ProductRelateTargetsDict = new Dictionary<string, List<Target>>();
        //     foreach (var itemState in ProductItemStates)
        //     {
        //         ProductRelateTargetsDict.Add(itemState.id,new List<Target>());
        //     }
        //
        //     var lookup = targets.GroupBy(
        //         t => t.TargetAction,
        //         t => t,
        //         (_, ts) => ts.ToLookup(t => t.ItemStateId));
        //     foreach (var (productId,processes) in productDfsOperationDict)
        //     {
        //         for (int i = processes.Count - 1; i >= 0; i--)
        //         {
        //             ProductRelateTargetsDict[productId].Add(lookup.First(d=>d));
        //         }
        //         var match = processes.Any(p => 
        //             p.inputItemsRef.Any(iRef => iRef.idref == target.ItemStateId) || 
        //             p.outputItemsRef.Any(iRef => iRef.idref == target.ItemStateId));
        //         if (match)
        //         {
        //             
        //         }
        //     }
        //     foreach (var target in targets)
        //     {
        //         
        //     }
        // }
    }

}
