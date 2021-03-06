using System;
using System.Collections.Generic;
using System.Linq;
using OD;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{

    #region Target

    /// <summary>
    /// GameObject是持有物体的对象，
    /// TargetAction是枚举，指定操作是Get/Give/Hold，
    /// ItemType是指定的物体类型
    /// </summary>
    public record Target
    {
        public GameObject GameObject{ get; private set; }
        public TargetAction TargetAction{ get; private set; }
        public string ItemStateId{ get; private set; }
        public Target(){}
        public Target(GameObject gameObject, TargetAction targetAction, string itemStateId)
        {
            this.GameObject = gameObject;
            this.TargetAction = targetAction;
            this.ItemStateId = itemStateId;
        }
    }

    public enum TargetAction
    {
        Get,Give,Hold
    }
    #endregion

    #region Order

    public record Order
    {
        public Order(string productId, float generateTime, float deadLine)
        {
            ProductId = productId;
            GenerateTime = generateTime;
            this.DeadLine = deadLine;
        }

        public string ProductId { get; }
        public float GenerateTime { get; }
        public float DeadLine { get; }
    }

    #endregion
    

    #region Status

    public record AgvStatus
    {
        /// <summary>
        /// for velocity information
        /// </summary>
        public Rigidbody Rigidbody;
        public OrderedDictionary<string,List<Item>> HoldingItems;
        public Target Target;
        public List<Target> ActionSpace;

        public AgvStatus(Rigidbody rigidbody, OrderedDictionary<string, List<Item>> holdingItems, Target target, List<Target> actionSpace)
        {
            Rigidbody = rigidbody;
            HoldingItems = holdingItems;
            Target = target;
            ActionSpace = actionSpace;
        }
    }

    public record WorkstationStatus
    {
        
        public Process CurrentProcess;
        public OrderedDictionary<string,List<Item>> InputBufferItems;
        public OrderedDictionary<string,List<Item>> OutputBufferItems;
        public List<Process> ActionSpace;
        
        public WorkstationStatus(Process currentProcess, OrderedDictionary<string, List<Item>> inputBufferItems, OrderedDictionary<string, List<Item>> outputBufferItems, List<Process> actionSpace)
        {
            CurrentProcess = currentProcess;
            InputBufferItems = inputBufferItems;
            OutputBufferItems = outputBufferItems;
            ActionSpace = actionSpace;
        }
    }
    
    // public record WorkstationStatus
    // {
    //     /// <summary>
    //     /// this should be number of items in input buffer/max capacity of input buffer
    //     /// </summary>
    //     public float InputLoadRatio;
    //     /// <summary>
    //     /// 输入口各类物料的数量/全局最大缓冲区容量
    //     /// </summary>
    //     public float[] InputItemQuantityArray;
    //     /// <summary>
    //     /// this should be number of items in output buffer/max capacity of output buffer
    //     /// </summary>
    //     public float OutputLoadRatio;
    //     /// <summary>
    //     /// 输出口各类物料的数量/全局最大缓冲区容量
    //     /// </summary>
    //     public float[] OutputItemQuantityArray;
    //     /// <summary>
    //     /// Index of current process in PlaneController.ProcessList
    //     /// </summary>
    //     public int CurrentProcess;
    //
    //     public WorkstationStatus(float inputLoadRatio, float[] inputItemQuantityArray, float outputLoadRatio, float[] outputItemQuantityArray, int currentProcessIndex)
    //     {
    //         InputLoadRatio = inputLoadRatio;
    //         InputItemQuantityArray = inputItemQuantityArray;
    //         OutputLoadRatio = outputLoadRatio;
    //         OutputItemQuantityArray = outputItemQuantityArray;
    //         CurrentProcess = currentProcessIndex;
    //     }
    // }

    #endregion
    public class LinkedOperation
    {
        public Process Process;
        public LinkedOperation Next;

        public LinkedOperation(Process process, LinkedOperation next=null)
        {
            this.Process = process;
            this.Next = next;
        }
    }

    public class LinkedTransport
    {
        public GameObject Pick;
        public GameObject Put;
        public string ItemId;
        public LinkedTransport Next;

        public LinkedTransport(GameObject pick, GameObject put, string itemId)
        {
            this.Pick = pick;
            this.Put = put;
            this.ItemId = itemId;
        }
    }

    public class Transport
    {
        public GameObject Pick;
        public GameObject Put;
        public string ItemId;

        public Transport(GameObject pick, GameObject put, string itemId)
        {
            this.Pick = pick;
            this.Put = put;
            this.ItemId = itemId;
        }
    }
}
