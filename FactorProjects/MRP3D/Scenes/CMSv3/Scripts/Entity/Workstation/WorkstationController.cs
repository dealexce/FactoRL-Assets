using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using OD;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class WorkstationController : WorkstationControllerBase,
        IExchangeable, IResettable, ILinkedToPlane, IHaveStatus<WorkstationStatus>
    {
        public PlaneController PlaneController { get; set; }
        public WorkstationAgent workstationAgent;
        public GameObject inputPlateGameObject;
        public GameObject processPlateGameObject;
        public GameObject outputPlateGameObject;
        
        public MeshRenderer processPlateMeshRenderer;
        public Material originalMaterial;
        public Material processingMaterial;

        public OrderedDictionary<string,List<Item>> InputBufferItemsDict;
        public OrderedDictionary<string,List<Item>> ProcessingInputItemsDict;
        public OrderedDictionary<string,List<Item>> OutputBufferItemsDict;

        public override void Init(Workstation model)
        {
            base.Init(model);
            InitInputOutputItems();
            ProcessingInputItemsDict = new OrderedDictionary<string, List<Item>>();
            foreach (var k in InputBufferItemsDict.Keys)
            {
                ProcessingInputItemsDict.Add(k,new List<Item>());
            }
            workstationAgent.InitActionSpace();
            PlaneController.RegisterAgent(workstationAgent, "WS"+Workstation.id,workstationAgent.InitActionSpace);
        }
        private void Start()
        {
            workstationAgent.DecideProcess();
        }

        public WorkstationStatus GetStatus()
        {
            return new WorkstationStatus(
                CurrentProcess,
                InputBufferItemsDict,
                OutputBufferItemsDict,
                workstationAgent.ActionSpace);
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

        public void OnReceived(ExchangeMessage exchangeMessage)
        {
            PlaceItemList(InputBufferItemsDict.Values);
        }

        #endregion

        #region IResettable Implement
        public Vector3 InitPosition { get; set; }
        public void EpisodeReset()
        {
            StopCoroutine(nameof(ProcessItemToOutput));
            StopCoroutine(nameof(Hold));
            Done();
            ItemOdUtils.DestroyAndClearLists(InputBufferItemsDict.Values,Destroy);
            ItemOdUtils.DestroyAndClearLists(ProcessingInputItemsDict.Values,Destroy);
            ItemOdUtils.DestroyAndClearLists(OutputBufferItemsDict.Values,Destroy);
            transform.position = InitPosition;
        }

        #endregion

        #region Workstation Process Implement
        
        public Process CurrentProcess { get; private set; } = null;
        [InspectorUtil.DisplayOnly]
        public string currentProcessName;
        public void StartProcess(Process process)
        {
            ItemOdUtils.ClearLists(ProcessingInputItemsDict.Values);
            if (process==null)
            {
                currentProcessName = "Hold";
                StartCoroutine(nameof(Hold));
                return;
            }
            currentProcessName = process.id;
            var status = CheckProcessIsExecutable(process);
            switch (status)
            {
                case ProcessExecutableStatus.Ok:
                    foreach (var iRef in process.inputItemsRef)
                    {
                        var item = InputBufferItemsDict[iRef.idref][0];
                        item.transform.parent = processPlateGameObject.transform;
                        ProcessingInputItemsDict[iRef.idref].Add(item);
                    }
                    StartCoroutine(nameof(ProcessItemToOutput),process);
                    break;
                case ProcessExecutableStatus.Unsupported:
                    Debug.LogWarning(Workstation.name + ": Cannot start chosen process: " +
                                     "chosen process is not supported");
                    break;
                case ProcessExecutableStatus.LackOfInput:
                    Debug.LogWarning(Workstation.name + ": Cannot start chosen process: " +
                                     "some required input items are not found in input buffer");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public enum ProcessExecutableStatus
        {
            Ok,Unsupported,LackOfInput
        }
        /// <summary>
        /// Check whether a process can be executed at present.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public ProcessExecutableStatus CheckProcessIsExecutable(Process process)
        {
            // Check whether the process is in support process list
            // if (!Workstation.supportProcessesRef.Select(p => p.idref).Contains(process.id))
            //     return ProcessExecutableStatus.Unsupported;
            if(!workstationAgent.ActionSpace.Contains(process))
                return ProcessExecutableStatus.Unsupported;
            if (process == null)
                return ProcessExecutableStatus.Ok;

            // Find required input items in input buffer
            foreach (var iRef in process.inputItemsRef)
            {
                Assert.IsTrue(InputBufferItemsDict.ContainsKey(iRef.idref));
                var itemList = InputBufferItemsDict[iRef.idref];
                if (itemList.Count <=0)
                {
                    // Input buffer does not have required input item
                    return ProcessExecutableStatus.LackOfInput;
                }
            }

            return ProcessExecutableStatus.Ok;
        }

        public float holdActionDuration = 1f;

        private IEnumerator Hold()
        {
            yield return new WaitForSeconds(holdActionDuration);
            Done();
        }

        private IEnumerator ProcessItemToOutput([NotNull] Process p)
        {
            CurrentProcess = p;
            processPlateMeshRenderer.material = processingMaterial;
            // Simulate process time
            yield return new WaitForSeconds(p.duration);
            processPlateMeshRenderer.material = originalMaterial;
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
            currentProcessName = "null";
            workstationAgent.DecideProcess();
        }

        #endregion
        
        #region Visualization

        public float itemInterval = .2f;
        private void PlaceItemList(IEnumerable<List<Item>> lists)
        {
            int i = 1;
            foreach (var list in lists)
            {
                foreach (var item in list)
                {
                    var transform1 = item.transform;
                    transform1.rotation = Quaternion.identity;
                    transform1.position = transform1.parent.position+Vector3.up * itemInterval * i;
                    //i++;
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
                var p = ScenarioLoader.getProcess(pRef.idref);
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

    }
}

