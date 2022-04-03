using System;
using System.Collections.Generic;
using Multi;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class WorkstationAgent : Agent, ILinkedToPlane
    {
        private WorkstationController _workstationController;
        private List<Process> _actionSpace;
        public PlaneController PlaneController { get; set; }

        private void Awake()
        {
            _workstationController = GetComponentInParent<WorkstationController>();
        }

        private void InitActionSpace()
        {
            // Process==null refers to no process
            _actionSpace.Add(null);
            foreach (var pRef in _workstationController.Workstation.supportProcessesRef)
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
            //throw new NotImplementedException();
            //collect relative position and status of all workstations
            foreach (var (wsObj,wsController) in PlaneController.WorkstationControllerDict)
            {
                if (wsController == _workstationController)
                {
                    continue;
                }
                Vector2 polarTargetPos = new Vector2();
                if (wsObj != null)
                {
                    polarTargetPos = Utils.NormalizedPolarRelativePosition(
                        transform,
                        wsObj.transform,
                        NormValues.DistanceMaxValue);
                }
                sensor.AddObservation(polarTargetPos);
                
                var s = wsController.GetStatus();
                foreach (var list in s.InputBufferItems.Values)
                {
                    sensor.AddObservation(Utils.NormalizeValue(list.Count,0,NormValues.ItemCountMaxValue));
                }
                foreach (var list in s.OutputBufferItems.Values)
                {
                    sensor.AddObservation(Utils.NormalizeValue(list.Count,0,NormValues.ItemCountMaxValue));
                }
                // If current process != null, pOneHot = index of current process in workstation.supportProcessesRef
                // else, pOneHot = workstation.supportProcessesRef.Count
                var supportProcessesRef = wsController.Workstation.supportProcessesRef;
                foreach(var pRef in supportProcessesRef)
                {
                    sensor.AddObservation(s.CurrentProcess.id == pRef.idref ? 1.0f : 0.0f);
                }
            }
            
            // collect relative position and status of other AGVs
            foreach (var (agvObj,agvController) in PlaneController.AgvControllerDict)
            {
                Vector2 polarTargetPos = new Vector2();
                if (agvObj != null)
                {
                    polarTargetPos = Utils.NormalizedPolarRelativePosition(
                        transform,
                        agvObj.transform,
                        NormValues.DistanceMaxValue);
                }
                sensor.AddObservation(polarTargetPos);
                
                var s = agvController.GetStatus();
                // Collect current currentTarget information
                foreach (var target in PlaneController.AgvDispatcherActionSpace)
                {
                    sensor.AddObservation(s.Target == target ? 1.0f : 0.0f);
                }
                // Collect holding item information
                foreach (var list in s.HoldingItems.Values)
                {
                    sensor.AddObservation(Utils.NormalizeValue(list.Count,0,NormValues.ItemCountMaxValue));
                }
            }

            // Collect product stock info
            foreach (var stock in PlaneController.productStockDict.Values)
            {
                sensor.AddObservation(Utils.NormalizeValue(stock,0,NormValues.StockCountMaxValue));
            }
            
            // Collect order info
            int orderCount = 0;
            foreach (var (ddl,order) in PlaneController.orderList)
            {
                sensor.AddObservation((ddl-Time.fixedTime)/NormValues.TimeLeftMaxValue);
                foreach (var itemState in SceanrioLoader.ProductItemStates)
                {
                    sensor.AddObservation(itemState.id == order.ProductId ? 1.0f : 0.0f);
                }
                orderCount++;
                if(orderCount>=NormValues.OrderObservationLength)
                    break;
            }
            // Padding order observation
            int padSize = Math.Clamp(
                              (NormValues.OrderObservationLength - orderCount), 
                              0, 
                              NormValues.OrderObservationLength)
                          * (1 + SceanrioLoader.ProductItemStates.Count);
            sensor.AddObservation(new float[padSize]);
        }

        public void DecideProcess()
        {
            RequestDecision();
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var act = actionsOut.DiscreteActions;
            act[0] = GetRandomExecutableProcessIndex();

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
