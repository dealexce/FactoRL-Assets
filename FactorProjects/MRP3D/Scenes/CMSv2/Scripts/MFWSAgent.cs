using System.Collections.Generic;
using Multi;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class MFWSAgent : Agent
    {
        public HashSet<ItemType> acceptableInputTypes = new HashSet<ItemType>();

        //根据这个MFWS可接受的process和input buffer中现有的材料遮罩动作：只选择可以执行的process
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            base.WriteDiscreteActionMask(actionMask);
        }
        
        //动作空间：所有process的集合+1待机（什么也不做）
        public override void OnActionReceived(ActionBuffers actions)
        {
            base.OnActionReceived(actions);
        }
        
    }
}
