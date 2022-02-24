using System;
using System.Collections.Generic;
using System.Text;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class AGVDispatcherAgent : Agent
    {
        private AGVController _agvController;
        private int cur = 1;
        public bool showDebug = false;

        private void Awake()
        {
            _agvController = GetComponentInParent<AGVController>();

        }

        private void Start()
        {

        }


        public override void OnActionReceived(ActionBuffers actions)
        {
            var action = actions.DiscreteActions[0];
            _agvController.AssignNewTarget(action);
        }


        public int RequestNewRandomTarget()
        {
            return Random.Range(0, _agvController.targetableGameObjects.Count);
        }

        public void RequestTargetDecision()
        {
            RequestDecision();
        }

        //TODO:这里有问题
        //Mask invalid actions based on AGV's holding item (TODO:and workstations' status?)
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            List<Target> comb = _agvController._planeController.TargetCombinationList;
            //手里没有物体，只能拿不能给，屏蔽所有给的动作（comb.ItemType==null）
            if (_agvController.holdingItem == null)
            {
                for (int i = 0; i < comb.Count; i++)
                {
                    if (comb[i].ItemType == null)
                    {
                        actionMask.SetActionEnabled(0, i+1, false);
                    }
                }
            }
            //手里有一个itemType的物体，不能再拿只能给，TODO:屏蔽掉所有拿的动作（comb.ItemType!=null)和收不了的target
            else
            {
                for (int i = 0; i < comb.Count; i++)
                {
                    if (comb[i].ItemType != null||
                        !_agvController.TargetableGameObjectItemHolderDict[comb[i].GameObject]
                            .supportInputs.Contains(_agvController.holdingItem.itemType))
                    {
                        actionMask.SetActionEnabled(0, i+1, false);
                    }
                }
            }
        }
        

        //collect relative position of all workstations
        //collect status of all workstations (input/output buffer capacity ratio)
        //collect target of all AGVs in one-hot
        //SIZE = TargetableGameObjectItemHolderDict.Keys.Count*2+MFWSControllers.Count*2+AGVControllers.Count*TargetCombinationList.Count
        // *collect received broadcast info from other agents
        public override void CollectObservations(VectorSensor sensor)
        {
            //Normalization values
            float maxDiameter = _agvController._planeController.MAXDiameter;
            //collect relative position of all workstations
            foreach (var targetObj in _agvController.targetableGameObjects)
            {
                Vector2 polarTargetPos = new Vector2();
                if (targetObj != null)
                {
                    polarTargetPos = Utils.PolarRelativePosition(transform,targetObj.transform,maxDiameter);
                }
                sensor.AddObservation(polarTargetPos);
            }
            //collect status of all workstations (input/output buffer capacity ratio)
            foreach (var c in _agvController._planeController.MFWSControllers)
            {
                sensor.AddObservation(c.GetInputCapacityRatio());
                sensor.AddObservation(c.GetOutputCapacityRatio());
            }
            //collect target of all AGVs in one-hot
            foreach (var agv in _agvController._planeController.AGVControllers)
            {
                AGVStatus agvStatus = agv.GetStatus();
                // sensor.AddOneHotObservation(agv.targetIndex,
                //     _agvController
                //         ._planeController
                //         .TargetCombinationList
                //         .Count);
            }
        }
        
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var o = actionsOut.DiscreteActions;
            // o[0] = Random.Range(1, _agvController._planeController.TargetCombinationList.Count+1);
            o[0] = cur++;
            if (cur > _agvController._planeController.TargetCombinationList.Count)
            {
                cur = 1;
            }
        }
    }
}
