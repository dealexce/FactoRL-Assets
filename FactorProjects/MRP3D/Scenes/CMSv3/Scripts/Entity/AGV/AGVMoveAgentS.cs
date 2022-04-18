using System;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class AGVMoveAgentS : EntityAgent<AGVMoveAgentS.MoveAction>
    {
        public enum MoveAction
        {
            Hold,Left,LeftForward,Forward,RightForward,Right,RightBackward,Backward,LeftBackward
        }
        [SerializeField]
        private AgvController agvController;

        private bool activateReward = true;
        public float arriveReward = .01f;
        public float collisionReward = -.01f;

        protected override List<int> WriteDiscreteActionMaskProtected()
        {
            var mask = new List<int>();
            if (agvController.CurrentTarget == null)
            {
                for (int i = 1; i < ActionSpace.Count; i++)
                {
                    mask.Add(i);
                }
            }
            else
            {
                mask.Add(0);
            }

            return mask;
        }

        public override List<MoveAction> InitActionSpace()
        {
            return ((MoveAction[]) Enum.GetValues(typeof(MoveAction))).ToList();
        }


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

        protected override void OnActionReceivedProtected(MoveAction action)
        {
            switch (action)
            {
                case MoveAction.Hold:
                    return;
                case MoveAction.Left:
                    agvController.Move(0f,-1f);
                    break;
                case MoveAction.LeftForward:
                    agvController.Move(1f, -1f);
                    break;
                case MoveAction.Forward:
                    agvController.Move(1f,0f);
                    break;
                case MoveAction.RightForward:
                    agvController.Move(1f,1f);
                    break;
                case MoveAction.Right:
                    agvController.Move(0f,1f);
                    break;
                case MoveAction.RightBackward:
                    agvController.Move(-1f,1f);
                    break;
                case MoveAction.Backward:
                    agvController.Move(-1f,0f);
                    break;
                case MoveAction.LeftBackward:
                    agvController.Move(-1f,-1f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public RayPerceptionSensorComponent3D rayFront;
        public RayPerceptionSensorComponent3D rayBack;
        private RayPerceptionSensor frps,brps;
        private float[] f_obs,b_obs;
        private Queue<float[]> f_sobs, b_sobs;
        private int fraynum, ftagcount;
        private int braynum, btagcount;

        private void Awake()
        {
            rayFront.CreateSensors();
            rayBack.CreateSensors();
            frps=rayFront.RaySensor;
            brps=rayBack.RaySensor;
            
            f_obs = new float[rayFront.GetRayPerceptionInput().OutputSize()];
            b_obs = new float[rayBack.GetRayPerceptionInput().OutputSize()];
            fraynum = rayFront.GetRayPerceptionInput().Angles.Count;
            ftagcount = rayFront.DetectableTags.Count;
            braynum = rayBack.GetRayPerceptionInput().Angles.Count;
            btagcount = rayBack.DetectableTags.Count;
        }

        public bool debugShow = false;

        /// <summary>
        /// Total Observation:
        /// 2 (polar velocity x,z)
        /// 2 (currentTarget relative position x,z)
        /// = 4 continuous
        /// </summary>
        /// <param name="sensor"></param>
        protected override void CollectObservationsProtected(VectorSensor sensor)
        {
            frps.Update();
            brps.Update();
            for (var rayIndex = 0; rayIndex < fraynum; rayIndex++)
            {
                frps.RayPerceptionOutput.RayOutputs?[rayIndex].ToFloatArray(ftagcount, rayIndex, f_obs);
            }
            for (var rayIndex = 0; rayIndex < braynum; rayIndex++)
            {
                brps.RayPerceptionOutput.RayOutputs?[rayIndex].ToFloatArray(btagcount, rayIndex, b_obs);
            }
            sensor.AddObservation(f_obs);
            sensor.AddObservation(b_obs);
            if (debugShow)
            {
                Debug.LogError(string.Join(',',f_obs));
            }

            sensor.AddObservation(agvController.PolarVelocity);
            sensor.AddObservation(agvController.PolarTargetPos);
            
        }
        
        protected override int HeuristicProtected()
        {
            return 0;
        }
        

    }
}
