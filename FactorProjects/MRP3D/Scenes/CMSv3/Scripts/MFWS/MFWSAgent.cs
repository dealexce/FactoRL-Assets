using System;
using System.Collections.Generic;
using Multi;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class MFWSAgent : Agent
    {
        public MFWSController mfwsController;


        private void Awake()
        {
            mfwsController = GetComponent<MFWSController>();
        }

        //根据这个MFWS可接受的process和input buffer中现有的材料遮罩动作：只选择可以执行的process
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            int processCount = mfwsController._planeController.ProcessList.Count;
            //Disable all process actions except hold action (pid=0)
            var available = mfwsController.getCurrentAvailableProcessId();
            for(int i=1;i<processCount;i++)
            {
                actionMask.SetActionEnabled(0,i,false);
            }
            //Then enable available process actions
            foreach (var i in available)
            {
                actionMask.SetActionEnabled(0, i, true);
            }
        }
        
        //动作空间：所有process的集合+1待机（0=什么也不做）
        public override void OnActionReceived(ActionBuffers actions)
        {
            var action = actions.DiscreteActions[0];
            mfwsController.DecideAndStartProcessItem(action);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sensor"></param>
        public override void CollectObservations(VectorSensor sensor)
        {
            int processCount = mfwsController._planeController.ProcessList.Count;
            float maxDiameter = mfwsController._planeController.MAXDiameter;
            foreach (var mc in mfwsController._planeController.MFWSControllers)
            {
                //相对位置
                sensor.AddObservation(Utils.PolarRelativePosition(transform,mc.transform,maxDiameter));
                MFWSStatus s = mc.GetStatus();
                sensor.AddObservation(s.InputLoadRatio);
                sensor.AddObservation(s.OutputLoadRatio);
                sensor.AddObservation(s.InputItemQuantityArray);
                sensor.AddObservation(s.OutputItemQuantityArray);
                sensor.AddOneHotObservation(s.CurrentProcessIndex,processCount);
            }

            int targetCount = mfwsController._planeController.TargetCombinationList.Count;
            int itemTypeCount = mfwsController._planeController.ItemTypeList.Count;
            foreach (var ac in mfwsController._planeController.AGVControllers)
            {
                sensor.AddObservation(Utils.PolarRelativePosition(transform,ac.transform,maxDiameter));
                AGVStatus s = ac.GetStatus();
                sensor.AddObservation(s.Rigidbody.velocity);
                sensor.AddObservation(s.Rigidbody.angularVelocity);
                sensor.AddOneHotObservation(s.TargetIndex,targetCount);
                sensor.AddOneHotObservation(s.HoldingItemIndex,itemTypeCount);
            }
        }

        public void DecideProcess()
        {
            RequestDecision();
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var act = actionsOut.DiscreteActions;
            List<int> available = mfwsController.getCurrentAvailableProcessId();
            if (available.Count == 0)
            {
                act[0] = 0;
            }
            act[0] = available[Random.Range(0, available.Count)];
        }
    }
}
