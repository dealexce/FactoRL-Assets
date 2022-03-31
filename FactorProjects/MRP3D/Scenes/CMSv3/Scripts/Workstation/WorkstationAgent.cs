using System;
using System.Collections.Generic;
using Multi;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class WorkstationAgent : Agent
    {
        private WorkstationController _workstationController;
        private List<Process> _actionSpace;

        private void Awake()
        {
            _workstationController = GetComponentInParent<WorkstationController>();
        }

        private void InitActionSpace()
        {
            // Process==null refers to no process
            _actionSpace.Add(null);
            foreach (var pRef in _workstationController.workstation.supportProcessesRef)
            {
                _actionSpace.Add(SceanrioLoader.getProcess(pRef.idref));
            }
        }

        public bool useMask = false;
        /// <summary>
        /// If useMask==true, set action mask to false where the action process that cannot be executed for now
        /// </summary>
        /// <param name="actionMask"></param>
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            for (int i = 0; i < _actionSpace.Count; i++)
            {
                if(!_workstationController.CheckProcessIsExecutable(_actionSpace[i]))
                    actionMask.SetActionEnabled(0,i,false);
            }
        }
        
        public override void OnActionReceived(ActionBuffers actions)
        {
            var action = actions.DiscreteActions[0];
            _workstationController.StartProcess(_actionSpace[action]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sensor"></param>
        public override void CollectObservations(VectorSensor sensor)
        {
            throw new NotImplementedException();
            // int processCount = _workstationController._planeController.ProcessList.Count;
            // float maxDiameter = _workstationController._planeController.MAXDiameter;
            // foreach (var mc in _workstationController._planeController.MFWSControllers)
            // {
            //     //相对位置
            //     sensor.AddObservation(Utils.PolarRelativePosition(transform,mc.transform,maxDiameter));
            //     MFWSStatus s = mc.GetStatus();
            //     sensor.AddObservation(s.InputLoadRatio);
            //     sensor.AddObservation(s.OutputLoadRatio);
            //     sensor.AddObservation(s.InputItemQuantityArray);
            //     sensor.AddObservation(s.OutputItemQuantityArray);
            //     sensor.AddOneHotObservation(s.CurrentProcessIndex,processCount);
            // }
            //
            // int targetCount = _workstationController._planeController.TargetCombinationList.Count;
            // int itemTypeCount = _workstationController._planeController.ItemTypeList.Count;
            // foreach (var ac in _workstationController._planeController.AGVControllers)
            // {
            //     sensor.AddObservation(Utils.PolarRelativePosition(transform,ac.transform,maxDiameter));
            //     AGVStatus s = ac.GetStatus();
            //     sensor.AddObservation(s.Rigidbody.velocity);
            //     sensor.AddObservation(s.Rigidbody.angularVelocity);
            //     sensor.AddOneHotObservation(s.TargetIndex,targetCount);
            //     sensor.AddOneHotObservation(s.HoldingItemIndex,itemTypeCount);
            // }
        }

        public void DecideProcess()
        {
            RequestDecision();
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var act = actionsOut.DiscreteActions;
            
        }

        /// <summary>
        /// Return index of first executable process.
        /// If none process in action space is executable, return 0
        /// </summary>
        /// <returns></returns>
        private int GetFirstExecutableProcessIndex()
        {
            for (int i = 0; i < _actionSpace.Count; i++)
            {
                if(_actionSpace[i]==null)
                    continue;
                if (!_workstationController.CheckProcessIsExecutable(_actionSpace[i]))
                {
                    continue;
                }
                return i;
            }
            return 0;
        }

        private int GetRandomExecutableProcessIndex()
        {
            List<int> executables = new List<int>();
            for (int i = 0; i < _actionSpace.Count; i++)
            {
                if(_actionSpace[i]==null)
                    continue;
                if (!_workstationController.CheckProcessIsExecutable(_actionSpace[i]))
                {
                    continue;
                }
                executables.Add(i);
            }
            return executables.Count > 0 ? executables[Random.Range(0, executables.Count)] : 0;
        }
    }
}
