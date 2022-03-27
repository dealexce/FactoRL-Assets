using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Multi;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class WorkstationBase : MonoBehaviour, IExchangable, IResetable, ILinkedToPlane
    {
        public PlaneController planeController { get; set; }
        public GameObject inputPlateGameObject;
        public GameObject outputPlateGameObject;
        
        public Workstation workstation;
        public GameObject nameText;
        public GameObject processText;
        
        [InspectorUtil.DisplayOnly]
        public List<ItemState> receivableInputItemStates;  // need initialize
        [InspectorUtil.DisplayOnly]
        public List<Item> inputBufferItems = new List<Item>();
        [InspectorUtil.DisplayOnly]
        public List<Item> outputBufferItems = new List<Item>();

        #region IExchangable Implement
        public Item GetItem(string id)
        {
            foreach (var item in outputBufferItems)
            {
                if (id.Equals(item.itemState.id))
                {
                    return item;
                }
            }
            return null;
        }
        public ExchangeMessage CheckGivable(IExchangable receiver, Item item)
        {
            if (item != null && outputBufferItems.Contains(item))
            {
                return ExchangeMessage.Ok;
            }
            return ExchangeMessage.NullItem;
        }

        public ExchangeMessage CheckReceivable(IExchangable giver, Item item)
        {
            if (item!=null
                &&inputBufferItems.Count<workstation.inputCapacity
                &&receivableInputItemStates.Contains(item.itemState))
            {
                return ExchangeMessage.Ok;
            }
            return ExchangeMessage.WrongType;
        }
        
        public bool Store(Item item)
        {
            inputBufferItems.Add(item);
            PlaceItems();
            return true;
        }

        public bool Remove(Item item)
        {
            if (!outputBufferItems.Contains(item))
            {
                return false;
            }
            outputBufferItems.Remove(item);
            PlaceItems();
            return true;
        }

        #endregion

        #region IResetable Implement

        public void EpisodeReset()
        {
            StopCoroutine(nameof(ProcessItemToOutput));
            Done();
            DestroyAndClearList(inputBufferItems);
            DestroyAndClearList(_currentProcessInputs);
            DestroyAndClearList(outputBufferItems);
        }

        #endregion

        #region Workstation Process Implement
        
        public int CurrentProcessIndex { get; private set; } = -1;
        private List<Item> _currentProcessInputs = new List<Item>();
        public void StartProcess(int processIndex)
        {
            CurrentProcessIndex = processIndex;
            if (processIndex==-1)
            {
                StartCoroutine(nameof(Hold));
                return;
            }
            if (processIndex < 0 || processIndex >= workstation.supportProcessesRef.Length)
            {
                Debug.LogError("received invalid process id");
            }
            Process p = SceanrioLoader.getProcess(workstation.supportProcessesRef[processIndex].idref);
            // Find required input items in input buffer
            Dictionary<string, Item> pickDict = new Dictionary<string, Item>();
            
            bool itemsReady = false;
            int requiredItemCount = p.inputItemsRef.Length;
            int pickedItemCount = 0;
            foreach (var i in p.inputItemsRef)
            {
                pickDict.Add(i.idref,null);
            }
            foreach (var item in inputBufferItems)
            {
                if(pickedItemCount>=requiredItemCount)
                    break;
                Item tempItem = null;
                if (pickDict.TryGetValue(item.itemState.id, out tempItem))
                {
                    if (tempItem == null)
                    {
                        pickDict[item.itemState.id] = item;
                        pickedItemCount++;
                    }
                }
            }
            if (pickedItemCount!=requiredItemCount)
            {
                Debug.LogError(workstation.name+": An invalid process has been chosen, " +
                               "some required input items are not found in input buffer");
                return;
            }
            StartCoroutine(nameof(ProcessItemToOutput),(pickDict,p));
        }

        public float holdActionDuration = 1f;
        IEnumerator Hold()
        {
            yield return new WaitForSeconds(holdActionDuration);
        }

        IEnumerator ProcessItemToOutput((Dictionary<string,Item>,Process) ip)
        {
            var (pickInputDict, process) = ip;
            // Move input items to _currentProcessInputs
            foreach (var item in pickInputDict.Values)
            {
                inputBufferItems.Remove(item);
                _currentProcessInputs.Add(item);
            }
            PlaceItems();
            // Simulate process time
            yield return new WaitForSeconds(process.duration);
            // Remove and destroy items in _currentProcessInputs
            DestroyAndClearList(_currentProcessInputs);
            // Generate output items
            foreach (var itemStateRef in process.outputItemsRef)
            {
                var item = planeController.InstantiateItem(itemStateRef.idref,outputPlateGameObject);
                outputBufferItems.Add(item);
            }
            PlaceItems();
            Done();
        }
        
        private void Done()
        {
            CurrentProcessIndex = -1;
        }

        #endregion

        #region Visualization

        public float itemInterval = 1f;
        /// <summary>
        /// TODO: Place items in input buffer, output buffer and _currentProcessInputs
        /// </summary>
        private void PlaceItems()
        {
            for (int i = 0; i < inputBufferItems.Count; i++)
            {
                inputBufferItems[i].transform.position = inputPlateGameObject.transform.position
                                                         + Vector3.up * itemInterval * i;
            }
            for (int i = 0; i < _currentProcessInputs.Count; i++)
            {
                _currentProcessInputs[i].transform.position = transform.position
                                                              + Vector3.up * itemInterval * i;
            }
            for (int i = 0; i < outputBufferItems.Count; i++)
            {
                outputBufferItems[i].transform.position = outputPlateGameObject.transform.position
                                                          + Vector3.up * itemInterval * i;
            }
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

        private void InitReceivableInputItemStates()
        {
            receivableInputItemStates = new List<ItemState>();
            foreach (var pRef in workstation.supportProcessesRef)
            {
                foreach (var iRef in SceanrioLoader.getProcess(pRef.idref).inputItemsRef)
                {
                    receivableInputItemStates.Add(SceanrioLoader.getItemState(iRef.idref));
                }
            }
        }

        private void DestroyAndClearList(List<Item> items)
        {
            foreach (var item in items)
            {
                Destroy(item.gameObject);
            }
            items.Clear();
        }
        public void Start()
        {
            InitReceivableInputItemStates();
            RefreshText();
        }
    }
}

