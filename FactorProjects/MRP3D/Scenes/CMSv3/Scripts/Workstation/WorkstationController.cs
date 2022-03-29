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
    public class WorkstationController : MonoBehaviour, IExchangeable, IResetable, ILinkedToPlane, IHasStatus<WorkstationStatus>
    {
        public PlaneController planeController { get; set; }
        public GameObject inputPlateGameObject;
        public GameObject processPlateGameObject;
        public GameObject outputPlateGameObject;
        
        public Workstation workstation;
        public GameObject nameText;
        public GameObject processText;

        public OrderedDictionary<string,List<Item>> InputBufferItems;
        public OrderedDictionary<string,List<Item>> ProcessingInputItems;
        public OrderedDictionary<string,List<Item>> OutputBufferItems;

        public WorkstationStatus GetStatus()
        {
            return new WorkstationStatus(
                CurrentProcess,
                InputBufferItems,
                OutputBufferItems);
        }

        #region IExchangeable Implement
        public Item GetItem(string id)
        {
            if (!OutputBufferItems.ContainsKey(id))
                return null;
            if (!(OutputBufferItems[id].Count>0))
                return null;
            return OutputBufferItems[id][0];
        }
        public ExchangeMessage CheckGivable(IExchangeable receiver, Item item)
        {
            if (item != null 
                && OutputBufferItems.ContainsKey(item.itemState.id)
                && OutputBufferItems[item.itemState.id].Contains(item))
            {
                return ExchangeMessage.Ok;
            }
            return ExchangeMessage.NullItem;
        }

        public ExchangeMessage CheckReceivable(IExchangeable giver, Item item)
        {
            if (item==null
                || !InputBufferItems.ContainsKey(item.itemState.id))
            {
                return ExchangeMessage.WrongType;
            }
            if(workstation.inputCapacitySpecified
               &&ItemOdUtils.ListsSumCount(InputBufferItems.Values)>=workstation.inputCapacity)
            {
                return ExchangeMessage.Overload;
            }
            return ExchangeMessage.Ok;
        }
        
        public bool Store(Item item)
        {
            if (!InputBufferItems.ContainsKey(item.itemState.id))
                return false;
            InputBufferItems[item.itemState.id].Add(item);
            PlaceItemList(InputBufferItems.Values);
            return true;
        }

        public bool Remove(Item item)
        {
            if (!OutputBufferItems.ContainsKey(item.itemState.id))
                return false;

            if (!OutputBufferItems[item.itemState.id].Remove(item))
                return false;
            PlaceItemList(OutputBufferItems.Values);
            return true;
        }

        #endregion

        #region IResetable Implement

        public void EpisodeReset()
        {
            StopCoroutine(nameof(ProcessItemToOutput));
            Done();
            ItemOdUtils.DestroyAndClearLists(InputBufferItems.Values,Destroy);
            ItemOdUtils.DestroyAndClearLists(ProcessingInputItems.Values,Destroy);
            ItemOdUtils.DestroyAndClearLists(OutputBufferItems.Values,Destroy);
        }

        #endregion

        #region Workstation Process Implement
        
        public Process CurrentProcess { get; private set; } = null;
        public void StartProcess(string processId)
        {
            ItemOdUtils.ClearLists(ProcessingInputItems.Values);
            Process process = SceanrioLoader.getProcess(processId);
            CurrentProcess = process;
            if (process==null)
            {
                StartCoroutine(nameof(Hold));
                return;
            }
            // Find required input items in input buffer
            foreach (var iRef in process.inputItemsRef)
            {
                if (!InputBufferItems.ContainsKey(iRef.idref))
                {
                    Debug.LogWarning(workstation.name+": Cannot start chosen process: " +
                                     "unsupported process");
                    return;
                }
                var itemList = InputBufferItems[iRef.idref];
                if (itemList.Count > 0)
                {
                    // Copy input item reference from input buffer to processing item list
                    var item = itemList[0];
                    item.transform.parent = processPlateGameObject.transform;
                    ProcessingInputItems[iRef.idref].Add(item);
                }
                else
                {
                    // Input buffer does not have required input item
                    ItemOdUtils.ClearLists(ProcessingInputItems.Values);
                    Debug.LogWarning(workstation.name+": Cannot start chosen process: " +
                                     "some required input items are not found in input buffer");
                    return;
                }
            }
            StartCoroutine(nameof(ProcessItemToOutput),(ProcessingInputItems,process));
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
            // Remove and destroy items in ProcessingInputItems
            foreach (var list in ProcessingInputItems.Values)
            {
                foreach (var item in list)
                {
                    InputBufferItems[item.itemState.id].Remove(item);
                }
            }
            ItemOdUtils.DestroyAndClearLists(ProcessingInputItems.Values,Destroy);
            // Generate output items
            foreach (var itemStateRef in p.outputItemsRef)
            {
                var item = planeController.InstantiateItem(itemStateRef.idref,outputPlateGameObject);
                OutputBufferItems[item.itemState.id].Add(item);
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
            PlaceItemList(InputBufferItems.Values);
            PlaceItemList(ProcessingInputItems.Values);
            PlaceItemList(OutputBufferItems.Values);
        }

        /// <summary>
        /// Parse and show workstation name and support process information on the game object.
        /// </summary>
        private void RefreshText()
        {
            nameText.GetComponent<TextMeshPro>().text = workstation.name;
            StringBuilder sb = new StringBuilder();
            foreach (var pref in workstation.supportProcessesRef)
            {
                Process p = SceanrioLoader.getProcess(pref.idref);
                foreach (var iref in p.inputItemsRef)
                {
                    sb.Append(SceanrioLoader.getItemState(iref.idref).name);
                    sb.Append('+');
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append("=>");
                foreach (var iref in p.outputItemsRef)
                {
                    sb.Append(SceanrioLoader.getItemState(iref.idref).name);
                    sb.Append('+');
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append('\n');
            }
            processText.GetComponent<TextMeshPro>().text = sb.ToString();
        }

        #endregion

        private void InitInputOutputItems()
        {
            InputBufferItems = new OrderedDictionary<string,List<Item>>();
            OutputBufferItems = new OrderedDictionary<string,List<Item>>();
            foreach (var pRef in workstation.supportProcessesRef)
            {
                var p = SceanrioLoader.getProcess(pRef.idref);
                foreach (var iRef in p.inputItemsRef)
                {
                    if(!InputBufferItems.ContainsKey(iRef.idref))
                        InputBufferItems.Add(iRef.idref,new List<Item>());
                }
                foreach (var iRef in p.outputItemsRef)
                {
                    if(!OutputBufferItems.ContainsKey(iRef.idref))
                        OutputBufferItems.Add(iRef.idref,new List<Item>());
                }
            }
        }

                
        public void Start()
        {
            InitInputOutputItems();
            RefreshText();
        }
    }
}

