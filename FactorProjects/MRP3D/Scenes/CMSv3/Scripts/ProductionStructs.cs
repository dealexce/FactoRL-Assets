using System.Collections.Generic;
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
        public string ProductId { get; }
        public float deadLine { get; }

        public Order(string productId, float deadLine)
        {
            this.ProductId = productId;
            this.deadLine = deadLine;
        }
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

        public AgvStatus(Rigidbody rigidbody, OrderedDictionary<string,List<Item>> holdingItems, Target target)
        {
            Rigidbody = rigidbody;
            HoldingItems = holdingItems;
            Target = target;
        }
    }

    public record WorkstationStatus
    {
        public Process CurrentProcess;
        public OrderedDictionary<string,List<Item>> InputBufferItems;
        public OrderedDictionary<string,List<Item>> OutputBufferItems;

        public WorkstationStatus(Process currentProcess, OrderedDictionary<string, List<Item>> inputBufferItems, OrderedDictionary<string, List<Item>> outputBufferItems)
        {
            CurrentProcess = currentProcess;
            InputBufferItems = inputBufferItems;
            OutputBufferItems = outputBufferItems;
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

}
