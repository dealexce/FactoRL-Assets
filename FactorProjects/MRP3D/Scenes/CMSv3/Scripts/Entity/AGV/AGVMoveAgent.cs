using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class AGVMoveAgent : Agent
    {
        [SerializeField]
        private AgvController agvController;

        public bool trainingMode = true;
        public bool showObsDebugInfo = false;
        public bool activateReward = true;
        public float arriveReward = 1f;
        public float collisionReward = -.01f;
        


        public void ArriveTarget()
        {
            RewardIfActivated(arriveReward);
        }

        public void CollideTrain()
        {
            RewardIfActivated(collisionReward);
        }

        private void RewardIfActivated(float r)
        {
            if (activateReward)
            {
                AddReward(r);
            }
        }
        

        /// <summary>
        /// This override method handles the action decisions made by neural network
        /// or gameplay actions and use the actions to operate in the game
        /// action.DiscreteActions[i] is 1 or -1
        /// Index   Meaning (1, -1)
        /// 0       move forthright (forward, backward)
        /// 1       rotate (right, left)
        /// </summary>
        /// <param name="actions"></param>
        public override void OnActionReceived(ActionBuffers actions)
        {
            // Don't do anything if target is null
            if(agvController.CurrentTarget==null)
                return;
            ActionSegment<int> act = actions.DiscreteActions;
            float move = act[0] switch
            {
                1 => 1f,
                2 => -1f,
                _ => 0f
            };
            float rot = act[1] switch
            {
                1 => 1f,
                2 => -1f,
                _ => 0f
            };
            agvController.Move(move, rot);
        }

        /// <summary>
        /// Total Observation:
        /// 2 (polar velocity x,z)
        /// 2 (currentTarget relative position x,z)
        /// = 4 continuous
        /// </summary>
        /// <param name="sensor"></param>
        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(agvController.PolarVelocity);
            sensor.AddObservation(agvController.PolarTargetPos);
            if (showObsDebugInfo)
            {
                Debug.Log(
                    "R:" + GetCumulativeReward()+ 
                    "|polarV:"+agvController.PolarVelocity+
                    "|polarP:"+agvController.PolarTargetPos.ToString("f6"));
            }
        }


        public override void Heuristic(in ActionBuffers actionsOut)
        {
            // if (trainingMode)
            //     return;
            var discreteActions = actionsOut.DiscreteActions;
            discreteActions[0] = Input.GetAxis("Vertical") switch
            {
                > 0 => 1,
                < 0 => 2,
                _ => discreteActions[0]
            };
            discreteActions[1] = Input.GetAxis("Horizontal") switch
            {
                > 0 => 1,
                < 0 => 2,
                _ => discreteActions[1]
            };
            discreteActions[0] = Random.Range(0, 3);
            discreteActions[1] = Random.Range(0, 3);
        }
        

    }
}
