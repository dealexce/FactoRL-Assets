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
    public class EntityAgent<T> : Agent, ILinkedToPlane
    {
        public PlaneController PlaneController { get; set; }
        public int typeNum = -1;
        public int offset = -1;
        [SerializeField]
        protected VectorSensorComponent goalSensor;

        [SerializeField]
        public BehaviorParameters behaviorParameters;

        [NonSerialized]
        public List<T> ActionSpace;

        public virtual List<T> InitActionSpace()
        {
            throw new NotImplementedException();
        }
        public override void CollectObservations(VectorSensor sensor)
        {
            // Use goal sensor as role identifier
            goalSensor.GetSensor().AddOneHotObservation(typeNum,PlaneController.AgentTypeCount);
            CollectObservationsProtected(sensor);
            var specObsSize = behaviorParameters.BrainParameters.VectorObservationSize;
            var addedObsSize = GetObservations().Count;
            if (addedObsSize > specObsSize)
            {
                Debug.LogError($"Spec observation size {specObsSize} is smaller than added observation's size {addedObsSize}");
            }
        }

        protected virtual void CollectObservationsProtected(VectorSensor sensor)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Mask actions that are out of current agent's action space size
        /// </summary>
        /// <param name="actionMask"></param>
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            var maxActionSize = behaviorParameters.BrainParameters.ActionSpec.BranchSizes[0];
            if (PlaneController.globalSetting.UseUnionActionSpace)
            {
                for (int i = 0; i < offset; i++)
                {
                    actionMask.SetActionEnabled(0, i, false);
                }

                for (int i = offset+ActionSpace.Count; i < maxActionSize; i++)
                {
                    actionMask.SetActionEnabled(0, i, false);
                }
            }
            else
            {
                for (int i = ActionSpace.Count; i < maxActionSize; i++)
                {
                    actionMask.SetActionEnabled(0, i, false);
                }
            }
            
            WriteDiscreteActionMaskProtected()?.ForEach(i=>actionMask.SetActionEnabled(0,InnerActionToModelAction(i),false));
        }

        protected virtual List<int> WriteDiscreteActionMaskProtected()
        {
            throw new NotImplementedException();
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            var act = actions.DiscreteActions;
            OnActionReceivedProtected(ActionSpace[ModelActionToInnerAction(act[0])]);
        }

        protected virtual void OnActionReceivedProtected(T action)
        {
            throw new NotImplementedException();
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var act = actionsOut.DiscreteActions;
            act[0] = InnerActionToModelAction(HeuristicProtected());
        }

        protected virtual int HeuristicProtected()
        {
            throw new NotImplementedException();
        }

        private int InnerActionToModelAction(int innerAction)
        {
            if (PlaneController.globalSetting.UseUnionActionSpace)
            {
                if (innerAction >= 0 && innerAction < ActionSpace.Count)
                {
                    return innerAction + offset;
                }
                else
                {
                    Debug.LogError(
                        "Inner action index out of bound of action space when converting inner action to model action");
                    return 0;
                }
            }
            return innerAction;
        }
        
        private int ModelActionToInnerAction(int modelAction)
        {
            if (PlaneController.globalSetting.UseUnionActionSpace)
            {
                var innerAction = modelAction - offset;
                if (innerAction >= 0 && innerAction < ActionSpace.Count)
                {
                    return innerAction;
                }
                else
                {
                    Debug.LogError(
                        "Inner action index out of bound of action space when converting model action to inner action");
                    return 0;
                }
            }
            
            return modelAction;
        }
    }
}
