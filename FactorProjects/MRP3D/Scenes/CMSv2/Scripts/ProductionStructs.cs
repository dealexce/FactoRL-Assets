using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
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

    #region Process
    public record Process
    {
        public int pid { get; private set; }
        public string inputType { get; private set; }
        public string outputType { get; private set; }
        public float duration { get; private set; }

        public Process(){}

        public Process(int pid, string inputType, string outputType, float duration)
        {
            this.pid = pid;
            this.inputType = inputType;
            this.outputType = outputType;
            this.duration = duration;
        }
    }
    #endregion

    #region Order

    public record Order
    {
        public string productItemType { get; }
        public float deadLine { get; }

        public Order(string productItemType, float deadLine)
        {
            this.productItemType = productItemType;
            this.deadLine = deadLine;
        }
    }

    #endregion
    

    #region Status

    public struct AGVStatus
    {
        public Rigidbody Rigidbody;
        public int HoldingItemIndex;
        public int TargetIndex;

        public AGVStatus(Rigidbody rigidbody, int holdingItemIndex, int targetIndex)
        {
            Rigidbody = rigidbody;
            HoldingItemIndex = holdingItemIndex;
            TargetIndex = targetIndex;
        }
    }

    public struct MFWSSimpleStatus
    {
        public float[] SelfInputItemQuantityArray;
        public float[] SelfOutputItemQuantityArray;
        public float[] SelfCurrentProcessOneHot;

        public MFWSSimpleStatus(float[] selfInputItemQuantityArray, float[] selfOutputItemQuantityArray, float[] selfCurrentProcessOneHot)
        {
            SelfInputItemQuantityArray = selfInputItemQuantityArray;
            SelfOutputItemQuantityArray = selfOutputItemQuantityArray;
            SelfCurrentProcessOneHot = selfCurrentProcessOneHot;
        }
    }
    public struct MFWSStatus
    {
        /// <summary>
        /// this should be number of items in input buffer/max capacity of input buffer
        /// </summary>
        public float InputLoadRatio;
        /// <summary>
        /// 输入口各类物料的数量/全局最大缓冲区容量
        /// </summary>
        public float[] InputItemQuantityArray;
        /// <summary>
        /// this should be number of items in output buffer/max capacity of output buffer
        /// </summary>
        public float OutputLoadRatio;
        /// <summary>
        /// 输出口各类物料的数量/全局最大缓冲区容量
        /// </summary>
        public float[] OutputItemQuantityArray;
        /// <summary>
        /// Index of current process in PlaneController.ProcessList
        /// </summary>
        public int CurrentProcessIndex;

        public MFWSStatus(float inputLoadRatio, float[] inputItemQuantityArray, float outputLoadRatio, float[] outputItemQuantityArray, int currentProcessIndex)
        {
            InputLoadRatio = inputLoadRatio;
            InputItemQuantityArray = inputItemQuantityArray;
            OutputLoadRatio = outputLoadRatio;
            OutputItemQuantityArray = outputItemQuantityArray;
            CurrentProcessIndex = currentProcessIndex;
        }
    }

    #endregion

}
