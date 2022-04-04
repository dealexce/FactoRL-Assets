using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    [RequireComponent(typeof(VectorSensorComponent))]
    public class EntityAgent<T> : Agent
    {
        public int typeNum = -1;
        [SerializeField]
        protected VectorSensorComponent goalSensor;

        [SerializeField]
        public BehaviorParameters behaviorParameters;

        [NonSerialized]
        public List<T> ActionSpace;

        public override void CollectObservations(VectorSensor sensor)
        {
            goalSensor.GetSensor().AddOneHotObservation(typeNum,PlaneController.AgentTypeCount);
            CollectObservationsProtected(sensor);
        }

        protected virtual void CollectObservationsProtected(VectorSensor sensor)
        {
        }

        /// <summary>
        /// Mask actions that are out of current agent's action space size
        /// </summary>
        /// <param name="actionMask"></param>
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            var maxActionSize = behaviorParameters.BrainParameters.ActionSpec.BranchSizes[0];
            for (int i = ActionSpace.Count; i < maxActionSize; i++)
            {
                actionMask.SetActionEnabled(0, i, false);
            }
            WriteDiscreteActionMaskProtected(actionMask);
        }

        protected virtual void WriteDiscreteActionMaskProtected(IDiscreteActionMask actionMask)
        {
        }
    }
}
