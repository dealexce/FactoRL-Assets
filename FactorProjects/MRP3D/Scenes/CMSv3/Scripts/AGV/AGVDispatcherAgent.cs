using System;
using System.Collections.Generic;
using System.Text;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class AGVDispatcherAgent : Agent
    {
        private AgvController _agvController;
        private PlaneController _planeController;

        private List<Target> _actionSpace;

        private void Awake()
        {
            _agvController = GetComponentInParent<AgvController>();
        }

        private void Start()
        {
            _planeController = _agvController.planeController;
            _actionSpace = _planeController.AgvDispatcherActionSpace;
        }

        
        public override void OnActionReceived(ActionBuffers actions)
        {
            var action = actions.DiscreteActions[0];
            _agvController.AssignNewTarget(_actionSpace[action]);
        }

        public void RequestTargetDecision()
        {
            RequestDecision();
        }

        public bool useMask = false;
        // TODO:Mask invalid actions based on AGV's holding item
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            if (useMask)
                throw new NotImplementedException();
            // if(!useMask)
            //     return;
            // for (int i = 0; i < _actionSpace.Count; i++)
            // {
            //     var currentTarget = _actionSpace[i];
            //     if (currentTarget == TargetAction.Get)
            //     {
            //         if(currentTarget.)
            //     }
            // }



            // int mc = 0;
            // List<Target> comb = _agvController.PlaneController.TargetCombinationList;
            // //手里没有物体，只能拿不能给，屏蔽所有给的动作（comb.ItemType==null）
            // if (_agvController.holdingItem == null)
            // {
            //     for (int i = 1; i < comb.Count; i++)
            //     {
            //         if (comb[i].TargetAction != TargetAction.Get
            //             || _agvController.TargetableGameObjectItemHolderDict[comb[i].GameObject].GetItem(comb[i].ItemStateId)==null)
            //         {
            //             actionMask.SetActionEnabled(0, i, false);
            //             mc++;
            //         }
            //     }
            // }
            // //手里有一个itemType的物体，不能再拿只能给，TODO:屏蔽掉所有拿的动作和收不了的target
            // else
            // {
            //     for (int i = 1; i < comb.Count; i++)
            //     {
            //         if (comb[i].TargetAction != TargetAction.Give||
            //             !_agvController.TargetableGameObjectItemHolderDict[comb[i].GameObject]
            //                 .supportInputs.Contains(_agvController.holdingItem.itemType))
            //         {
            //             actionMask.SetActionEnabled(0, i, false);
            //             mc++;
            //         }
            //     }
            // }
            // if (mc < comb.Count - 1)
            // {
            //     actionMask.SetActionEnabled(0, 0, false);
            // }
        }
        
        /// <summary>
        /// How many orders can be observed
        /// </summary>
        public int orderObservationLength = 5;
        public int normItemCountMaxValue = 5;
        public float normTimeLeftMaxValue = 120f;

        //collect relative position of all workstations
        //collect status of all workstations (input/output buffer capacity ratio)
        //collect relative position of all AGVs
        //collect currentTarget of all AGVs in one-hot
        //SIZE = TargetableGameObjectItemHolderDict.Keys.Count*2+MFWSControllers.Count*2+AGVControllers.Count*TargetCombinationList.Count
        // *collect received broadcast info from other agents
        public override void CollectObservations(VectorSensor sensor)
        {
            //collect relative position and status of all workstations
            foreach (var (wsObj,wsController) in _planeController.WorkstationControllerDict)
            {
                Vector2 polarTargetPos = new Vector2();
                if (wsObj != null)
                {
                    polarTargetPos = Utils.NormalizedPolarRelativePosition(
                        transform,
                        wsObj.transform,
                        _agvController.planeController.normDistanceMaxValue);
                }
                sensor.AddObservation(polarTargetPos);
                
                var s = wsController.GetStatus();
                foreach (var list in s.InputBufferItems.Values)
                {
                    sensor.AddObservation(Utils.NormalizeValue(list.Count,0,normItemCountMaxValue));
                }
                foreach (var list in s.OutputBufferItems.Values)
                {
                    sensor.AddObservation(Utils.NormalizeValue(list.Count,0,normItemCountMaxValue));
                }
                // If current process != null, pOneHot = index of current process in workstation.supportProcessesRef
                // else, pOneHot = workstation.supportProcessesRef.Count
                var supportProcessesRef = wsController.workstation.supportProcessesRef;
                foreach(var pRef in supportProcessesRef)
                {
                    sensor.AddObservation(s.CurrentProcess.id == pRef.idref ? 1.0f : 0.0f);
                }
            }
            
            // collect relative position and status of other AGVs
            foreach (var (agvObj,agvController) in _planeController.AgvControllerDict)
            {
                if (agvController == _agvController)
                {
                    continue;
                }
                Vector2 polarTargetPos = new Vector2();
                if (agvObj != null)
                {
                    polarTargetPos = Utils.NormalizedPolarRelativePosition(
                        transform,
                        agvObj.transform,
                        _agvController.planeController.normDistanceMaxValue);
                }
                sensor.AddObservation(polarTargetPos);
                
                var s = agvController.GetStatus();
                // Collect current currentTarget information
                foreach (var target in _actionSpace)
                {
                    sensor.AddObservation(s.Target == target ? 1.0f : 0.0f);
                }
                // Collect holding item information
                foreach (var list in s.HoldingItems.Values)
                {
                    sensor.AddObservation(Utils.NormalizeValue(list.Count,0,normItemCountMaxValue));
                }
            }

            // Collect product stock info
            foreach (var stock in _planeController.productStockDict.Values)
            {
                sensor.AddObservation(Utils.NormalizeValue(stock,0,_planeController.normStockCountMaxValue));
            }
            
            // Collect order info
            int orderCount = 0;
            foreach (var (ddl,order) in _planeController.orderList)
            {
                sensor.AddObservation((ddl-Time.fixedTime)/normTimeLeftMaxValue);
                foreach (var itemState in SceanrioLoader.ProductItemStates)
                {
                    sensor.AddObservation(itemState.id == order.ProductId ? 1.0f : 0.0f);
                }
                orderCount++;
                if(orderCount>=orderObservationLength)
                    break;
            }
            // Padding
            int padSize = Math.Clamp((orderObservationLength - orderCount), 0, orderObservationLength) *
                          (1 + SceanrioLoader.ProductItemStates.Count);
            sensor.AddObservation(new float[padSize]);
        }
        
        //give a random valid currentTarget
        public int GenerateRandomActionIndex()
        {
            return Random.Range(0, _actionSpace.Count);
        }
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var act = actionsOut.DiscreteActions;
            act[0] = GenerateRandomActionIndex();
        }
    }
}
