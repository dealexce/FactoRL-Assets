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
            if (action == 0)
            {
                _agvController.target = new Target(null, null);
            }
            else
            {
                _agvController.target = _agvController.planeController.AvailableTargetCombination[action - 1];
            }
            _agvController.noTargetTime = 0f;
        }


        public Target RequestNewTarget()
        {
            int randomTargetIndex = Random.Range(0, _agvController.availableTargetsObj_forTest.Count);
            return new Target(_agvController.availableTargetsObj_forTest[randomTargetIndex],null);
        }

        public void RequestTargetDecision()
        {
            RequestDecision();
        }

        //TODO:这里有问题
        //Mask invalid actions based on AGV's holding item (TODO:and workstations' status?)
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            List<Target> comb = _agvController.planeController.AvailableTargetCombination;
            //手里没有物体，只能拿不能给，屏蔽所有给的动作（comb.itemType==null）
            if (_agvController.holdingItem == null)
            {
                for (int i = 0; i < comb.Count; i++)
                {
                    if (comb[i].itemType == null)
                    {
                        actionMask.SetActionEnabled(0, i+1, false);
                    }
                }
            }
            //手里有一个itemType的物体，不能再拿只能给，TODO:屏蔽掉所有拿的动作（comb.itemType!=null)和收不了的target
            else
            {
                for (int i = 0; i < comb.Count; i++)
                {
                    if (comb[i].itemType != null||
                        !_agvController.AvailableTargetsItemHolderDict[comb[i].gameObject]
                            .supportInputs.Contains(_agvController.holdingItem.itemType))
                    {
                        actionMask.SetActionEnabled(0, i+1, false);
                    }
                }
            }
        }
        

        //collect relative position of all workstations
        // *collect received broadcast info from other agents
        public override void CollectObservations(VectorSensor sensor)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var targetObj in _agvController.availableTargetsObj_forTest)
            {
                Vector2 polarTargetPos = new Vector2();
                Vector3 targetPos = Vector3.zero;
                if (targetObj != null)
                {
                    targetPos = (targetObj.transform.position - transform.position) / _agvController.planeController.maxDiameter;
                    Vector3 cross = Vector3.Cross(targetPos, transform.forward);
                    float angle = Vector3.Angle(targetPos, transform.forward) / 180f;
                    polarTargetPos = new Vector2(cross.y > 0 ? -angle : angle, targetPos.magnitude);
                }
                sensor.AddObservation(polarTargetPos);
                if (showDebug)
                {
                    sb.Append(polarTargetPos.ToString());
                }
            }
            foreach (var c in _agvController.planeController.MfwsControllers)
            {
                sensor.AddObservation(c.getInputCapacityRatio());
                sensor.AddObservation(c.getOutputCapacityRatio());
            }
            if (showDebug)
            {
                sb.Append("|R:");
                sb.Append(GetCumulativeReward());
                Debug.Log(sb.ToString());
            }
        }
        
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var o = actionsOut.DiscreteActions;
            // o[0] = Random.Range(1, _agvController.planeController.AvailableTargetCombination.Count+1);
            o[0] = cur++;
            if (cur > _agvController.planeController.AvailableTargetCombination.Count)
            {
                cur = 1;
            }
        }
    }
}
