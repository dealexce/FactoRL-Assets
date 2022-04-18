using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class WorkstationAgent : EntityAgent<Process>
    {
        [SerializeField]
        private WorkstationController workstationController;


        public override List<Process> InitActionSpace()
        {
            var actionSpace = new List<Process>();
            // Process==null refers to no process
            // actionSpace.Add(null);
            foreach (var pRef in workstationController.Workstation.supportProcessesRef)
            {
                actionSpace.Add(ScenarioLoader.getProcess(pRef.idref));
            }
            return actionSpace;
        }

        public bool useMask = false;
        /// <summary>
        /// If useMask==true, set action mask to false where the action process that cannot be executed for now
        /// </summary>
        protected override List<int> WriteDiscreteActionMaskProtected()
        {
            if(!useMask)
                return null;
            if (GetNotNullExecutableProcessIndexes().Count == 0)
            {
                PlaneController._workstationStrangeMask++;
            }
            var mask = new List<int>();
            for (int i = 0; i < ActionSpace.Count; i++)
            {
                if (ActionSpace[i] == null)
                {
                    // mask.Add(i);
                }
                else if(workstationController.CheckProcessIsExecutable(ActionSpace[i])!=WorkstationController.ProcessExecutableStatus.Ok)
                {
                    mask.Add(i);
                }
            }

            return mask;
        }
        
        // public override void OnActionReceived(ActionBuffers actions)
        // {
        //     var action = actions.DiscreteActions[0];
        //     workstationController.StartProcess(ActionSpace[action]);
        // }
        protected override void OnActionReceivedProtected(Process process)
        {
            workstationController.StartProcess(process);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sensor"></param>
        protected override void CollectObservationsProtected(VectorSensor sensor)
        {
            //throw new NotImplementedException();
            //collect relative position and status of all workstations
            foreach (var (wsObj,wsController) in PlaneController.WorkstationControllerOd)
            {
                if (wsController == workstationController)
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
                foreach(var process in s.ActionSpace)
                {
                    sensor.AddObservation(s.CurrentProcess == process ? 1.0f : 0.0f);
                }
            }
            
            // collect relative position and status of other AGVs
            foreach (var (agvObj,agvController) in PlaneController.AgvControllerOd)
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
                foreach (var target in s.ActionSpace)
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
            foreach (var stock in PlaneController.productStockOd.Values)
            {
                sensor.AddObservation(Utils.NormalizeValue(stock,0,NormValues.StockCountMaxValue));
            }
            
            // Collect order info
            int orderCount = 0;
            foreach (var (_,order) in PlaneController.orderSortedList)
            {
                sensor.AddObservation(1.0f-(order.DeadLine-Time.fixedTime)/NormValues.OrderTimeMaxValue);
                foreach (var itemState in ScenarioLoader.ProductItemStates)
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
                          * (1 + ScenarioLoader.ProductItemStates.Count);
            sensor.AddObservation(new float[padSize]);
        }

        public void DecideProcess()
        {
            // If workstation only supports one process (null, p1),
            // start the process as long as it can, no need for RL decision
            var executables = GetNotNullExecutableProcessIndexes();
            if (executables.Count == 0)
            {
                workstationController.StartProcess(null);
            }else if (executables.Count == 1)
            {
                workstationController.StartProcess(ActionSpace[executables[0]]);
            }
            else
            {
                RequestDecision();
                PlaneController.workstationDecisionCount++;
            }
            
        }

        protected override int HeuristicProtected()
        {
            return GetRandomExecutableProcessIndex();
        }

        /// <summary>
        /// Return index of first executable process.
        /// If none process in action space is executable, return 0
        /// </summary>
        /// <returns></returns>
        private int GetFirstExecutableProcessIndex()
        {
            for (int i = 0; i < ActionSpace.Count; i++)
            {
                if(ActionSpace[i]==null)
                    continue;
                if (workstationController.CheckProcessIsExecutable(ActionSpace[i])!=WorkstationController.ProcessExecutableStatus.Ok)
                {
                    continue;
                }
                return i;
            }
            return 0;
        }

        private List<int> GetNotNullExecutableProcessIndexes()
        {
            List<int> executables = new List<int>();
            for (int i = 0; i < ActionSpace.Count; i++)
            {
                if(ActionSpace[i]==null)
                    continue;
                if (workstationController.CheckProcessIsExecutable(ActionSpace[i])!=WorkstationController.ProcessExecutableStatus.Ok)
                {
                    continue;
                }
                executables.Add(i);
            }
            return executables;
        }
        private int GetRandomExecutableProcessIndex()
        {
            var executables = GetNotNullExecutableProcessIndexes();
            return executables.Count > 0 ? executables[Random.Range(0, executables.Count)] : 0;
        }
    }
}
