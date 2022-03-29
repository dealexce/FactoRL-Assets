using System;
using System.Collections.Generic;
using Multi;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class AGVMoveAgent : Agent
    {
        private AgvController _agvController;

        public bool trainingMode = true;
        public bool showObsDebugInfo = false;
        public bool activateReward = true;
        public float arriveReward = 1f;
        public float collisionReward = -.01f;
        


        public void ArriveTarget()
        {
            RewardIfActivated(arriveReward);
        }

        public void collideTrain()
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



        void Awake()
        {
            _agvController = GetComponentInParent<AgvController>();
        }

        private void Start()
        {
            
        }

        private void Update()
        {

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
            ActionSegment<int> act = actions.DiscreteActions;
            float move = 0f;
            switch (act[0])
            {
                case 1:
                    move = 1f;
                    break;
                case 2:
                    move = -1f;
                    break;
            }
            float rot = 0f;
            switch (act[1])
            {
                case 1:
                    rot = 1f;
                    break;
                case 2:
                    rot = -1f;
                    break;
            }
            _agvController.Move(move, rot);
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
            sensor.AddObservation(_agvController.PolarVelocity);
            sensor.AddObservation(_agvController.PolarTargetPos);
            if (showObsDebugInfo)
            {
                Debug.Log(
                    "R:" + GetCumulativeReward()+ 
                    "|polarV:"+_agvController.PolarVelocity+
                    "|polarP:"+_agvController.PolarTargetPos.ToString("f6"));
            }
        }


        public override void Heuristic(in ActionBuffers actionsOut)
        {
            if (trainingMode)
                return;
            var discreteActions = actionsOut.DiscreteActions;
            float vert = Input.GetAxis("Vertical");
            if (vert > 0)
            {
                discreteActions[0] = 1;
            }else if (vert < 0)
            {
                discreteActions[0] = 2;
            }
            float hori = Input.GetAxis("Horizontal");
            if (hori > 0)
            {
                discreteActions[1] = 1;
            }else if (hori < 0)
            {
                discreteActions[1] = 2;
            }
        }
        

    }
}
