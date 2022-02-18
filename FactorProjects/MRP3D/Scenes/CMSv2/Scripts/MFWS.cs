using System;
using System.Collections;
using System.Collections.Generic;
using Multi;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class MFWS : MonoBehaviour, Resetable
    {
        //记录这个机器可以执行的process：k:input类型 v:process
        public Dictionary<int,Tuple<string,string,int>> supportProcesses = new Dictionary<int,Tuple<string,string,int>>();
        public HashSet<string> supportInputs = new HashSet<string>();
        public int inputBufferCapacity = 20;
        public int outputBufferCapacity = 20;
        public PlaneController _planeController;

        //processing variables and objects
        private Item processingItem = null;
        private float onProcessTime = 0f;
        
        //input/output plate GameObjects
        public GameObject inputPlate;
        public GameObject outputPlate;

        private List<Item> InputItemBuffer = new List<Item>();
        private List<Item> OutputItemBuffer = new List<Item>();

        public void EpisodeReset()
        {
            throw new NotImplementedException();
        }

        // public float getInputCapacityRatio()
        // {
        //     return 1f-(float)InputItemBuffer.Count/inputBufferCapacity;
        // }
        //
        // public float getOutputCapacityRatio()
        // {
        //     return (float)OutputItemBuffer.Count/outputBufferCapacity;
        // }
        //
        // public void ResetStation()
        // {
        //     if (processingItem != null)
        //     {
        //         Destroy(processingItem.gameObject);
        //     }
        //     processingItem = null;
        //     onProcessTime = 0f;
        //     foreach (var item in InputItemBuffer)
        //     {
        //         GameObject.Destroy(item.gameObject);
        //     }
        //     InputItemBuffer.Clear();
        //     foreach (var item in OutputItemBuffer)
        //     {
        //         GameObject.Destroy(item.gameObject);
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
        //             Destroy(processingItem.gameObject);
        //             processingItem = null;
        //             onProcessTime = 0f;
        //             GenerateOutputItem();
        //         }
        //     }
        //     int tempCount = 1;
        //     foreach (var item in InputItemBuffer)
        //     {
        //         item.gameObject.transform.position = inputPlate.transform.position + Vector3.up * .5f * tempCount++;
        //     }
        //     if (processingItem != null)
        //     {
        //         processingItem.gameObject.transform.position = transform.position + Vector3.up;
        //     }
        //     foreach (var item in OutputItemBuffer)
        //     {
        //         item.gameObject.transform.position = outputPlate.transform.position + Vector3.up * .5f * tempCount++;
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
        //     if (item.itemType != inputType)
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

