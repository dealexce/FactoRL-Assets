using System;
using System.Collections.Generic;
using Multi;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class MFWSAgent : Agent
    {
        public HashSet<ItemType> acceptableInputTypes = new HashSet<ItemType>();
        public MFWSController mfwsController;

        private void Awake()
        {
            mfwsController = GetComponent<MFWSController>();
        }

        //根据这个MFWS可接受的process和input buffer中现有的材料遮罩动作：只选择可以执行的process
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            //By default, all discrete actions are allowed.
            //actionMask.SetActionEnabled();
        }
        
        //动作空间：所有process的集合+1待机（什么也不做）
        public override void OnActionReceived(ActionBuffers actions)
        {
            base.OnActionReceived(actions);
        }

        public int DecideProcess()
        {
            List<int> available = mfwsController.getCurrentAvailableProcessId();
            if (available.Count == 0)
            {
                return -1;
            }
            return available[Random.Range(0, available.Count)];
        }
    }
}
