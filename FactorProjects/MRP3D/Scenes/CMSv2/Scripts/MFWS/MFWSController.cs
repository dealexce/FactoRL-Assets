using System;
using System.Collections;
using System.Collections.Generic;
using Multi;
using UnityEngine;
using UnityEngine.Assertions;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class MFWSController : ItemHolder, Resetable, LinkedToPlane, IHasStatus<MFWSStatus>
    {
        //记录这个机器可以执行的process：k:input类型 v:process
        public List<int> supportProcessIndex = new List<int>();
        public List<string> inputSet = new List<string>();
        public List<string> outputSet = new List<string>();
        public int inputBufferCapacity = 20;
        public int outputBufferCapacity = 20;

        //每次给出hold动作时的待机时间
        public float holdActionTime = 1f;
        
        public PlaneController _planeController { get; set; }    //由PlaneController赋值
        public MFWSAgent mfwsAgent;
        public bool isMultiFunctional;

        //processing variables and objects
        private int currentProcessIndex;
        private float remainHoldTime;
        
        //input/output plate GameObjects
        public GameObject inputPlate;
        public GameObject outputPlate;

        private List<Item> InputItemBuffer = new List<Item>();

        private List<Item> OutputItemBuffer = new List<Item>();

        #region Status

        public MFWSStatus GetStatus()
        {
            return new MFWSStatus(
                GetInputCapacityRatio(),
                getItemQuantityArray(InputItemBuffer),
                GetOutputCapacityRatio(),
                getItemQuantityArray(OutputItemBuffer),
                currentProcessIndex);
        }
        
        public float GetInputCapacityRatio()
        {
            return 1f-(float)InputItemBuffer.Count/inputBufferCapacity;
        }
        
        public float GetOutputCapacityRatio()
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
        
        public MFWSSimpleStatus GetSimpleStatus()
        {
            return new MFWSSimpleStatus(getSelfItemQuantityArray(BufferWay.In),
                getSelfItemQuantityArray(BufferWay.Out),
                Utils.ToOneHotObservation(supportProcessIndex.IndexOf(currentProcessIndex),supportProcessIndex.Count)
                );
        }
        
        private enum BufferWay
        {
            In,Out
        }
        private float[] getSelfItemQuantityArray(BufferWay bw)
        {
            List<string> set = inputSet;
            List<Item> buffer = InputItemBuffer;
            int capacity = inputBufferCapacity;
            if(bw == BufferWay.Out){
                set = outputSet;
                buffer = OutputItemBuffer;
                capacity = outputBufferCapacity;
            }
            int typeNum = set.Count;
            float[] res = new float[typeNum];
            foreach (var item in buffer)
            {
                int itemIndex = set.IndexOf(item.itemType);
                if (itemIndex < typeNum)
                    res[itemIndex] += 1f / capacity;
                else
                    Debug.LogError("FOUND INVALID ITEM IN BUFFER");
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

        private void FixedUpdate()
        {
            if (remainHoldTime<=0f 
                && currentProcessIndex==0 
                && InputItemBuffer.Count>0 
                && OutputItemBuffer.Count<outputBufferCapacity)
            {
                mfwsAgent.DecideProcess();
            }

            if (remainHoldTime > 0f)
            {
                remainHoldTime -= Time.fixedDeltaTime;
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
                return true;
            }
            return false;
        }

        #endregion

        //TODO:性能优化
        public List<int> getCurrentAvailableProcessId()
        {
            List<int> res = new List<int>();
            foreach (var id in supportProcessIndex)
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
            StopCoroutine(nameof(ProcessItemToOutput));
            Done();
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
        }

        public void DecideAndStartProcessItem(int todoPid)
        {
            currentProcessIndex = todoPid;
            Process p = _planeController.ProcessList[todoPid];
            if (p == PConsts.NullProcess)
            {
                Hold();
                return;
            }
            string inputType = p.inputType;
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
                Debug.LogError("MFWS Agent have chosen an invalid process: no required item found");
                return;
            }
            StartCoroutine(nameof(ProcessItemToOutput),(processItem,p));
        }

        private void Hold()
        {
            remainHoldTime += holdActionTime;
        }

        IEnumerator ProcessItemToOutput((Item,Process) ip)
        {
            var (item, process) = ip;
            Assert.IsNotNull(item);
            Assert.AreEqual(item.itemType,process.inputType);
            Assert.AreNotEqual(process,PConsts.NullProcess);
            //Move processing item to the middle
            item.transform.position = transform.position + Vector3.up;
            //simulate process time
            yield return new WaitForSeconds(process.duration);
            //set the type of input item to output item type
            item.setItemType(process.outputType);
            //Add processingItem into OutputItemBuffer and move its position to outputPlate
            InputItemBuffer.Remove(item);
            OutputItemBuffer.Add(item);
            //Move output item to the output plate
            item.transform.position = outputPlate.transform.position + Vector3.up * OutputItemBuffer.Count;
            Done();
            
        }

        private void Done()
        {
            currentProcessIndex = 0;
        }
    }
}

