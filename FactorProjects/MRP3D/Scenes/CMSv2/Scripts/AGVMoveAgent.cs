using System;
using System.Collections.Generic;
using Multi;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class AGVMoveAgent : Agent
    {
        public AGVController agvController;

        public bool trainingMode = true;
        public float arriveReward = 1f;
        public float collisionReward = -.01f;
        


        public void arriveTargetTrain()
        {
            RewardIfActivated(arriveReward);
        }

        public void collideTrain()
        {
            RewardIfActivated(collisionReward);
        }

        private void RewardIfActivated(float r)
        {
            if (agvController.activateAward)
            {
                AddReward(r);
            }
        }



        void Awake()
        {
            agvController = GetComponentInParent<AGVController>();
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
            ActionSegment<float> act = actions.ContinuousActions;
            agvController.Move(act[0],act[1]);
        }

        /// <summary>
        /// Total Observation:
        /// 2 (polar velocity x,z)
        /// 2 (target relative position x,z)
        /// = 4 continuous
        /// </summary>
        /// <param name="sensor"></param>
        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(agvController.polarVelocity);
            sensor.AddObservation(agvController.polarTargetPos);
            if (!trainingMode)
            {
                Debug.Log("R:" + GetCumulativeReward()+ "|polarV:"+agvController.polarVelocity+"|polarP:"+agvController.polarTargetPos.ToString("f6"));
            }
        }
        
        //TODO:

        
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            if (trainingMode)
                return;
            var continuousAction = actionsOut.ContinuousActions;
            continuousAction[0] = Input.GetAxis("Vertical");
            continuousAction[1] = Input.GetAxis("Horizontal");
        }
        

    }
}
