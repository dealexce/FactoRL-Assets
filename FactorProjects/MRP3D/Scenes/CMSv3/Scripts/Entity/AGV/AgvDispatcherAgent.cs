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
    public class AgvDispatcherAgent : Agent, ILinkedToPlane
    {
        public PlaneController PlaneController { get; set; }
        private AgvController _agvController;
        private PlaneController _planeController;

        private List<Target> _actionSpace;

        private void Awake()
        {
            _agvController = GetComponentInParent<AgvController>();
            _planeController = GetComponentInParent<PlaneController>();
        }

        private void Start()
        {
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
            if(!useMask)
                return;
            for (int i = 0; i < _actionSpace.Count; i++)
            {
                var target = _actionSpace[i];
                if(target==null)
                    continue;
                var other = _planeController.GameObjectExchangeableDict[target.GameObject];
                if (target.TargetAction == TargetAction.Get)
                {
                    var item = other.GetItem(target.ItemStateId);
                    // Disable action if other is not givable or this is not receivable
                    if(other.CheckGivable(_agvController,item)!=ExchangeMessage.Ok
                       ||_agvController.CheckReceivable(other,item)!=ExchangeMessage.Ok)
                        actionMask.SetActionEnabled(0,i,false);
                }
                if (target.TargetAction == TargetAction.Give)
                {
                    var item = _agvController.GetItem(target.ItemStateId);
                    // Disable action if other is not receivable or this is not givable
                    if(other.CheckReceivable(_agvController,item)!=ExchangeMessage.Ok
                       ||_agvController.CheckGivable(other,item)!=ExchangeMessage.Ok)
                        actionMask.SetActionEnabled(0,i,false);
                }
            }
        }
        


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
                        NormValues.DistanceMaxValue);
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
                    sensor.AddObservation(Utils.NormalizeValue(list.Count,0,NormValues.ItemCountMaxValue));
                }
            }

            // Collect product stock info
            foreach (var stock in _planeController.productStockDict.Values)
            {
                sensor.AddObservation(Utils.NormalizeValue(stock,0,NormValues.StockCountMaxValue));
            }
            
            // Collect order info
            int orderCount = 0;
            foreach (var (ddl,order) in _planeController.orderList)
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
            int padSize = Math.Clamp((NormValues.OrderObservationLength - orderCount), 0, NormValues.OrderObservationLength) *
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
