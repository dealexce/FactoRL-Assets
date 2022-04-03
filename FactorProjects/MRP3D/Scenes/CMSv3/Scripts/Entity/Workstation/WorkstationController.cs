using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Multi;
using OD;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class WorkstationController : WorkstationControllerBase,
        IExchangeable, IResetable, ILinkedToPlane, IHasStatus<WorkstationStatus>
    {
        public PlaneController PlaneController { get; set; }
        public GameObject inputPlateGameObject;
        public GameObject processPlateGameObject;
        public GameObject outputPlateGameObject;
        

        public OrderedDictionary<string,List<Item>> InputBufferItemsDict;
        public OrderedDictionary<string,List<Item>> ProcessingInputItemsDict;
        public OrderedDictionary<string,List<Item>> OutputBufferItemsDict;

        public WorkstationStatus GetStatus()
        {
            return new WorkstationStatus(
                CurrentProcess,
                InputBufferItemsDict,
                OutputBufferItemsDict);
        }

        #region IExchangeable Implement
        public Item GetItem(string id)
        {
            if (!OutputBufferItemsDict.ContainsKey(id))
                return null;
            if (!(OutputBufferItemsDict[id].Count>0))
                return null;
            return OutputBufferItemsDict[id][0];
        }
        public ExchangeMessage CheckGivable(IExchangeable receiver, Item item)
        {
            if (item != null 
                && OutputBufferItemsDict.ContainsKey(item.itemState.id)
                && OutputBufferItemsDict[item.itemState.id].Contains(item))
            {
                return ExchangeMessage.Ok;
            }
            return ExchangeMessage.NullItem;
        }

        public ExchangeMessage CheckReceivable(IExchangeable giver, Item item)
        {
            if (item==null
                || !InputBufferItemsDict.ContainsKey(item.itemState.id))
            {
                return ExchangeMessage.WrongType;
            }
            if(Workstation.inputCapacitySpecified
               &&ItemOdUtils.ListsSumCount(InputBufferItemsDict.Values)>=Workstation.inputCapacity)
            {
                return ExchangeMessage.Overload;
            }
            return ExchangeMessage.Ok;
        }
        
        public bool Store(Item item)
        {
            if (!InputBufferItemsDict.ContainsKey(item.itemState.id))
                return false;
            InputBufferItemsDict[item.itemState.id].Add(item);
            PlaceItemList(InputBufferItemsDict.Values);
            return true;
        }

        public bool Remove(Item item)
        {
            if (!OutputBufferItemsDict.ContainsKey(item.itemState.id))
                return false;

            if (!OutputBufferItemsDict[item.itemState.id].Remove(item))
                return false;
            PlaceItemList(OutputBufferItemsDict.Values);
            return true;
        }

        #endregion

        #region IResetable Implement

        public void EpisodeReset()
        {
            StopCoroutine(nameof(ProcessItemToOutput));
            Done();
            ItemOdUtils.DestroyAndClearLists(InputBufferItemsDict.Values,Destroy);
            ItemOdUtils.DestroyAndClearLists(ProcessingInputItemsDict.Values,Destroy);
            ItemOdUtils.DestroyAndClearLists(OutputBufferItemsDict.Values,Destroy);
        }

        #endregion

        #region Workstation Process Implement
        
        public Process CurrentProcess { get; private set; } = null;
        public void StartProcess(Process process)
        {
            ItemOdUtils.ClearLists(ProcessingInputItemsDict.Values);
            CurrentProcess = process;
            if (process==null)
            {
                StartCoroutine(nameof(Hold));
                return;
            }
            if (CheckProcessIsExecutable(process))
                StartCoroutine(nameof(ProcessItemToOutput),(ProcessingInputItemsDict,process));
        }

        /// <summary>
        /// Check whether a process can be executed at present.
        /// If yes, load required input items into ProcessingInputItemsDict.
        /// Else, clear the ProcessingInputItemsDict
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public bool CheckProcessIsExecutable(Process process)
        {
            ItemOdUtils.ClearLists(ProcessingInputItemsDict.Values);
            if (process == null)
                return true;
            // Find required input items in input buffer
            foreach (var iRef in process.inputItemsRef)
            {
                if (!InputBufferItemsDict.ContainsKey(iRef.idref))
                {
                    ItemOdUtils.ClearLists(ProcessingInputItemsDict.Values);
                    // Debug.LogWarning(workstation.name + ": Cannot start chosen process: " +
                    //                  "contains unsupported input(s)");
                    
                    return false;
                }
                var itemList = InputBufferItemsDict[iRef.idref];
                if (itemList.Count > 0)
                {
                    // Copy input item reference from input buffer to processing item list
                    var item = itemList[0];
                    item.transform.parent = processPlateGameObject.transform;
                    ProcessingInputItemsDict[iRef.idref].Add(item);
                }
                else
                {
                    // Input buffer does not have required input item
                    // ItemOdUtils.ClearLists(ProcessingInputItemsDict.Values);
                    // Debug.LogWarning(workstation.name + ": Cannot start chosen process: " +
                    //                  "some required input items are not found in input buffer");
                    return false;
                }
            }
            return true;
        }

        public float holdActionDuration = 1f;
        IEnumerator Hold()
        {
            yield return new WaitForSeconds(holdActionDuration);
            Done();
        }

        IEnumerator ProcessItemToOutput(Process p)
        {
            // Simulate process time
            yield return new WaitForSeconds(p.duration);
            // Remove and destroy items in ProcessingInputItemsDict
            foreach (var list in ProcessingInputItemsDict.Values)
            {
                foreach (var item in list)
                {
                    InputBufferItemsDict[item.itemState.id].Remove(item);
                }
            }
            ItemOdUtils.DestroyAndClearLists(ProcessingInputItemsDict.Values,Destroy);
            // Generate output items
            foreach (var itemStateRef in p.outputItemsRef)
            {
                var item = PlaneController.InstantiateItem(itemStateRef.idref,outputPlateGameObject);
                OutputBufferItemsDict[item.itemState.id].Add(item);
            }
            PlaceAllItems();
            Done();
        }
        
        private void Done()
        {
            CurrentProcess = null;
        }

        #endregion

        
        
        
        #region Visualization

        public float itemInterval = 1f;
        private void PlaceItemList(IEnumerable<List<Item>> lists)
        {
            int i = 1;
            foreach (var list in lists)
            {
                foreach (var item in list)
                {
                    item.gameObject.transform.localPosition = Vector3.up * itemInterval * i;
                    i++;
                }
            }
        }
        
        private void PlaceAllItems()
        {
            PlaceItemList(InputBufferItemsDict.Values);
            PlaceItemList(ProcessingInputItemsDict.Values);
            PlaceItemList(OutputBufferItemsDict.Values);
        }



        #endregion

        private void InitInputOutputItems()
        {
            InputBufferItemsDict = new OrderedDictionary<string,List<Item>>();
            OutputBufferItemsDict = new OrderedDictionary<string,List<Item>>();
            foreach (var pRef in Workstation.supportProcessesRef)
            {
                var p = SceanrioLoader.getProcess(pRef.idref);
                foreach (var iRef in p.inputItemsRef)
                {
                    if(!InputBufferItemsDict.ContainsKey(iRef.idref))
                        InputBufferItemsDict.Add(iRef.idref,new List<Item>());
                }
                foreach (var iRef in p.outputItemsRef)
                {
                    if(!OutputBufferItemsDict.ContainsKey(iRef.idref))
                        OutputBufferItemsDict.Add(iRef.idref,new List<Item>());
                }
            }
        }

                
        public override void Init(Workstation model)
        {
            base.Init(model);
            InitInputOutputItems();
        }

    }
}

