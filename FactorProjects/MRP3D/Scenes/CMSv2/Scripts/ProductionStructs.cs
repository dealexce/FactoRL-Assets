using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    /// <summary>
    /// gameObject是持有物体的对象，itemType是要从gameObject拿取的物体类型，如果是null则是把某物体给gameObject
    /// </summary>
    public struct Target
    {
        public GameObject gameObject;
        public string itemType;

        public Target(GameObject gameObject, string itemType)
        {
            this.gameObject = gameObject;
            this.itemType = itemType;
        }
    }

    public struct Process
    {
        public int pid;
        public string inputType;
        public string outputType;
        public float duration;

        public Process(int pid, string inputType, string outputType, float duration)
        {
            this.pid = pid;
            this.inputType = inputType;
            this.outputType = outputType;
            this.duration = duration;
        }
    }
}
