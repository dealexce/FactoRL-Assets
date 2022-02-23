using System;
using System.Collections;
using System.Collections.Generic;
using Multi;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class MFWSController : ItemHolder, Resetable, LinkedToPlane, IHasStatus<MFWSStatus>
    {
        //记录这个机器可以执行的process：k:input类型 v:process
        public List<int> supportProcessId = new List<int>();
        public int inputBufferCapacity = 20;
        public int outputBufferCapacity = 20;
        public PlaneController _planeController { get; set; }    //由PlaneController赋值
        public MFWSAgent mfwsAgent;
        public bool isMultiFunctional;

        //processing variables and objects
        private Process currentProcess = default;
        private Item processingItem = null;
        private float onProcessTime = 0f;
        private bool outputReady = true;
        
        //input/output plate GameObjects
        public GameObject inputPlate;
        public GameObject outputPlate;

        private List<Item> InputItemBuffer = new List<Item>();
        private List<Item> OutputItemBuffer = new List<Item>();

        #region Status

        public MFWSStatus GetStatus()
        {
            return new MFWSStatus(
                getInputCapacityRatio(),
                getItemQuantityArray(InputItemBuffer),
                getOutputCapacityRatio(),
                getItemQuantityArray(OutputItemBuffer),
                _planeController.ProcessIndexDict[currentProcess]);
        }
        public float getInputCapacityRatio()
        {
            return 1f-(float)InputItemBuffer.Count/inputBufferCapacity;
        }
        
        public float getOutputCapacityRatio()
        {
            return (float)OutputItemBuffer.Count/outputBufferCapacity;
        }
        private float[] getItemQuantityArray(List<Item> buffer)
        {
            int typeNum = _planeController.ItemTypeList.Count;
            float[] res = new float[typeNum];
            foreach (var item in InputItemBuffer)
            {
                int itemIndex = _planeController.ItemTypeIndexDict[item.itemType];
                if (itemIndex < typeNum)
                    res[itemIndex] += 1f / _planeController.MAXCapacity;
                else
                    Debug.LogError("INVALID ITEM INDEX");
            }
            return res;
        }

        #endregion


        #region Monobehavior Methods

        private void Awake()
        {
            InputGameObject = inputPlate;
            OutputGameObject = outputPlate;
            mfwsAgent = GetComponent<MFWSAgent>();
            _planeController = GetComponentInParent<PlaneController>();
        }

        private void Start()
        {
            
        }

        private void Update()
        {
            if (InputItemBuffer.Count>0&&outputReady&&processingItem == null)
            {
                DecideAndStartProcessItem();
            }
        }

        #endregion

        #region ItemHolder Implement
        public override Item GetItem(string itemType)
        {
            foreach (var item in OutputItemBuffer)
            {
                if (itemType.Equals(item.itemType))
                {
                    return item;
                }
            }
            return null;
        }
        public override ExchangeMessage CheckGivable(ItemHolder receiver, Item item)
        {
            if (item != null && OutputItemBuffer.Contains(item))
            {
                return ExchangeMessage.OK;
            }
            return ExchangeMessage.NullItem;
        }

        public override ExchangeMessage CheckReceivable(ItemHolder giver, Item item)
        {
            if (item!=null&&InputItemBuffer.Count<inputBufferCapacity&&supportInputs.Contains(item.itemType))
            {
                return ExchangeMessage.OK;
            }
            return ExchangeMessage.WrongType;
        }

        protected override bool Store(Item item)
        {
            if (item!=null&&InputItemBuffer.Count < inputBufferCapacity && supportInputs.Contains(item.itemType))
            {
                InputItemBuffer.Add(item);
                item.transform.SetParent(transform,true);
                item.transform.position = inputPlate.transform.position+Vector3.up*InputItemBuffer.Count;
                return true;
            }
            return false;
        }

        protected override bool Remove(Item item)
        {
            if (item != null && OutputItemBuffer.Contains(item))
            {
                OutputItemBuffer.Remove(item);
                if (OutputItemBuffer.Count < outputBufferCapacity)
                {
                    outputReady = true;
                }
                return true;
            }
            return false;
        }

        #endregion

        //TODO:性能优化
        public List<int> getCurrentAvailableProcessId()
        {
            List<int> res = new List<int>();
            foreach (var id in supportProcessId)
            {
                foreach (var item in InputItemBuffer)
                {
                    if (item.itemType.Equals(_planeController.ProcessList[id].inputType))
                    {
                        res.Add(id);
                        break;
                    }
                }
            }
            return res;
        }

        public void EpisodeReset()
        {
            CancelInvoke();
            if (processingItem != null)
            {
                Destroy(processingItem.gameObject);
            }
            processingItem = null;
            currentProcess = default;
            foreach (var item in InputItemBuffer)
            {
                GameObject.Destroy(item.gameObject);
            }
            InputItemBuffer.Clear();
            foreach (var item in OutputItemBuffer)
            {
                GameObject.Destroy(item.gameObject);
            }
            OutputItemBuffer.Clear();
            outputReady = true;
        }

        private void DecideAndStartProcessItem()
        {
            int todoPid = mfwsAgent.DecideProcess();
            if (todoPid == -1)
            {
                return;
            }
            currentProcess = _planeController.ProcessList[todoPid];
            string inputType = currentProcess.inputType;
            Item processItem = null;
            foreach (var item in InputItemBuffer)
            {
                if (item.itemType.Equals(inputType))
                {
                    processItem = item;
                    break;
                }
            }
            if (processItem == null)
            {
                Debug.LogError("MFWS Agent have chosen an invalid process");
                return;
            }
            InputItemBuffer.Remove(processItem);
            this.processingItem = processItem;
            processItem.transform.position = transform.position + Vector3.up;
            Invoke("ProcessingItemToOutput",currentProcess.duration);
        }

        private void ProcessingItemToOutput()
        {
            if (processingItem != null)
            {
                //Add processingItem into OutputItemBuffer and move its position to outputPlate
                processingItem.setItemType(currentProcess.outputType);
                OutputItemBuffer.Add(processingItem);
                processingItem.transform.position = outputPlate.transform.position + Vector3.up * OutputItemBuffer.Count;
            }
            if (OutputItemBuffer.Count >= outputBufferCapacity)
            {
                outputReady = false;
            }
            currentProcess = default;
            processingItem = null;
        }


        //
        // public void ResetStation()
        // {
        //     if (processingItem != null)
        //     {
        //         Destroy(processingItem.GameObject);
        //     }
        //     processingItem = null;
        //     onProcessTime = 0f;
        //     foreach (var item in InputItemBuffer)
        //     {
        //         GameObject.Destroy(item.GameObject);
        //     }
        //     InputItemBuffer.Clear();
        //     foreach (var item in OutputItemBuffer)
        //     {
        //         GameObject.Destroy(item.GameObject);
        //     }
        //     OutputItemBuffer.Clear();
        // }
        //
        // private void Start()
        // {
        // }
        //
        // /// <summary>
        // /// 加工过程：没有processingItem且InputItemBuffer不为空时，取一个item为processingItem并开始计时，
        // /// 当onProcessTime大于processTime且OutputItemBuffer未满时，执行GenerateOutputItem，
        // /// 删掉processingItem，重置onProcessTime，生成一个outputItem放在outputPlate上并存入OutputItemBuffer
        // /// </summary>
        // private void FixedUpdate()
        // {
        //     if (InputItemBuffer.Count > 0 && processingItem == null)
        //     {
        //         processingItem = InputItemBuffer[0];
        //         InputItemBuffer.RemoveAt(0);
        //     }
        //     if (processingItem != null)
        //     {
        //         onProcessTime += Time.deltaTime;
        //         if (onProcessTime > processTime && OutputItemBuffer.Count<outputBufferCapacity)
        //         {
        //             Destroy(processingItem.GameObject);
        //             processingItem = null;
        //             onProcessTime = 0f;
        //             GenerateOutputItem();
        //         }
        //     }
        //     int tempCount = 1;
        //     foreach (var item in InputItemBuffer)
        //     {
        //         item.GameObject.transform.position = inputPlate.transform.position + Vector3.up * .5f * tempCount++;
        //     }
        //     if (processingItem != null)
        //     {
        //         processingItem.GameObject.transform.position = transform.position + Vector3.up;
        //     }
        //     foreach (var item in OutputItemBuffer)
        //     {
        //         item.GameObject.transform.position = outputPlate.transform.position + Vector3.up * .5f * tempCount++;
        //     }
        // }
        //
        // /// <summary>
        // /// 生成一个outputObject，放在outputPlate上，并new一个Item存入outputBuffer
        // /// </summary>
        // private void GenerateOutputItem()
        // {
        //     GameObject outputObject = Instantiate(outputPrefab);
        //     
        //     Item item = new Item(outputType, outputObject);
        //     OutputItemBuffer.Add(item);
        // }
        //
        // public override Item GetItem()
        // {
        //     if (OutputItemBuffer.Count > 0)
        //     {
        //         return OutputItemBuffer[0];
        //     }
        //     return null;
        // }
        //
        // public override ExchangeMessage CheckReceivable(ItemHolder giver, Item item)
        // {
        //     if (item.ItemType != inputType)
        //     {
        //         return ExchangeMessage.WrongType;
        //     }
        //     if (InputItemBuffer.Count >= inputBufferCapacity)
        //     {
        //         return ExchangeMessage.Overload;
        //     }
        //     return ExchangeMessage.OK;
        // }
        //
        // public override ExchangeMessage CheckGivable(ItemHolder receiver, Item item)
        // {
        //     if (OutputItemBuffer.Contains(item))
        //     {
        //         return ExchangeMessage.OK;
        //     }
        //     return ExchangeMessage.ItemNotFound;
        // }
        //
        // protected override bool Store(Item item)
        // {
        //     try
        //     {
        //         InputItemBuffer.Add(item);
        //         return true;
        //     }
        //     catch (Exception e)
        //     {
        //         return false;
        //     }
        // }
        //
        // protected override bool Remove(Item item)
        // {
        //     return OutputItemBuffer.Remove(item);
        //     
        // }
        //
        // protected override void OnReceived(ExchangeMessage exchangeMessage)
        // {
        //     if (exchangeMessage==ExchangeMessage.WrongType)
        //     {
        //         _planeController.OnRewardEvent(Event.InputTypeError);
        //     }
        //     if (exchangeMessage == ExchangeMessage.OK)
        //     {
        //         _planeController.OnRewardEvent(Event.CorrectItemDelivered,(int)inputType+1.0f);
        //     }
        // }
    }
}

