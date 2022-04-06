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
    public class AgvDispatcherAgent : EntityAgent<Target>, ILinkedToPlane
    {
        public PlaneController PlaneController { get; set; }
        [SerializeField]
        private AgvController agvController;

        public List<Target> InitActionSpace()
        {
            var agvDispatcherActionSpace = new List<Target>();
            var entityGameObjectsDict = PlaneController.EntityGameObjectsDict;
            // Target==null refers to no target
            agvDispatcherActionSpace.Add(null);
            foreach (var wsObj in entityGameObjectsDict[typeof(Workstation)])
            {
                // possible [give x input item state] actions to workstation
                var controller = wsObj.GetComponent<WorkstationController>();
                foreach (var itemStateId in controller.InputBufferItemsDict.Keys)
                {
                    agvDispatcherActionSpace.Add(new Target(
                        controller.inputPlateGameObject,
                        TargetAction.Give,
                        itemStateId));
                }
                // possible [get x output item state] actions to workstation
                foreach (var itemStateId in controller.OutputBufferItemsDict.Keys)
                {
                    agvDispatcherActionSpace.Add(new Target(
                        controller.outputPlateGameObject,
                        TargetAction.Get,
                        itemStateId));
                }
            }
            
            // possible give product actions to export station
            foreach (var esObj in entityGameObjectsDict[typeof(ExportStation)])
            {
                foreach (var itemState in ScenarioLoader.ProductItemStates)
                {
                    agvDispatcherActionSpace.Add(new Target(
                        esObj,
                        TargetAction.Give,
                        itemState.id));
                }
            }
            // possible get raw actions to import station
            foreach (var isObj in entityGameObjectsDict[typeof(ImportStation)])
            {
                var controller = isObj.GetComponent<ImportController>();
                foreach (var iId in controller.RawItemsDict.Keys)
                {
                    agvDispatcherActionSpace.Add(new Target(
                        isObj,
                        TargetAction.Get,
                        iId));
                }
            }
            return agvDispatcherActionSpace;
        }
        public override void OnActionReceived(ActionBuffers actions)
        {
            var action = actions.DiscreteActions[0];
            agvController.AssignNewTarget(ActionSpace[action]);
        }

        public void RequestTargetDecision()
        {
            RequestDecision();
        }

        public bool useMask = false;
        protected override void WriteDiscreteActionMaskProtected(IDiscreteActionMask actionMask)
        {
            if(!useMask)
                return;
            for (int i = 0; i < ActionSpace.Count; i++)
            {
                var target = ActionSpace[i];
                if(target==null)
                    continue;
                var other = PlaneController.GameObjectExchangeableDict[target.GameObject];
                switch (target.TargetAction)
                {
                    case TargetAction.Get:
                    {
                        var item = other.GetItem(target.ItemStateId);
                        // Disable action if other is not givable or this is not receivable
                        if(other.CheckGivable(agvController,item)!=ExchangeMessage.Ok
                           ||agvController.CheckReceivable(other,item)!=ExchangeMessage.Ok)
                            actionMask.SetActionEnabled(0,i,false);
                        break;
                    }
                    case TargetAction.Give:
                    {
                        var item = agvController.GetItem(target.ItemStateId);
                        // Disable action if other is not receivable or this is not givable
                        if(other.CheckReceivable(agvController,item)!=ExchangeMessage.Ok
                           ||agvController.CheckGivable(other,item)!=ExchangeMessage.Ok)
                            actionMask.SetActionEnabled(0,i,false);
                        break;
                    }
                }
            }
        }
        
        protected override void CollectObservationsProtected(VectorSensor sensor)
        {
            //collect relative position and status of all workstations
            foreach (var (wsObj,wsController) in PlaneController.WorkstationControllerDict)
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
                foreach(var process in s.ActionSpace)
                {
                    sensor.AddObservation(s.CurrentProcess == process ? 1.0f : 0.0f);
                }
            }
            
            // collect relative position and status of other AGVs
            foreach (var (agvObj,agvCtrl) in PlaneController.AgvControllerDict)
            {
                if (agvCtrl == this.agvController)
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
                
                var s = agvCtrl.GetStatus();
                // Collect current currentTarget information
                foreach (var target in ActionSpace)
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
                sensor.AddObservation((ddl-Time.fixedTime)/NormValues.OrderTimeMaxValue);
                foreach (var itemState in ScenarioLoader.ProductItemStates)
                {
                    sensor.AddObservation(itemState.id == order.ProductId ? 1.0f : 0.0f);
                }
                orderCount++;
                if(orderCount>=NormValues.OrderObservationLength)
                    break;
            }
            // Padding order observation
            int padSize = Math.Clamp((NormValues.OrderObservationLength - orderCount), 0, NormValues.OrderObservationLength) *
                          (1 + ScenarioLoader.ProductItemStates.Count);
            sensor.AddObservation(new float[padSize]);
        }
        

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var act = actionsOut.DiscreteActions;
            act[0] = GetRandomValidTargetIndex();
        }
        //give a random valid currentTarget
        public int GetRandomValidTargetIndex()
        {
            var validTargets = new List<int>();
            for (int i = 0; i < ActionSpace.Count; i++)
            {
                var target = ActionSpace[i];
                if(target==null)
                    continue;
                var other = PlaneController.GameObjectExchangeableDict[target.GameObject];
                switch (target.TargetAction)
                {
                    case TargetAction.Get:
                    {
                        var item = other.GetItem(target.ItemStateId);
                        // Disable action if other is not givable or this is not receivable
                        if (other.CheckGivable(agvController, item) == ExchangeMessage.Ok
                            && agvController.CheckReceivable(other, item) == ExchangeMessage.Ok)
                        {
                            validTargets.Add(i);
                        }
                        break;
                    }
                    case TargetAction.Give:
                    {
                        var item = agvController.GetItem(target.ItemStateId);
                        // Disable action if other is not receivable or this is not givable
                        if (other.CheckReceivable(agvController, item) == ExchangeMessage.Ok
                            && agvController.CheckGivable(other, item) == ExchangeMessage.Ok)
                        {
                            validTargets.Add(i);
                        }
                        break;
                    }
                }
            }
            return validTargets.Count > 0 ? validTargets[Random.Range(0, validTargets.Count)] : 0;
        }
        private int GetFirstValidTargetIndex()
        {
            for (int i = 0; i < ActionSpace.Count; i++)
            {
                var target = ActionSpace[i];
                if(target==null)
                    continue;
                var other = PlaneController.GameObjectExchangeableDict[target.GameObject];
                switch (target.TargetAction)
                {
                    case TargetAction.Get:
                    {
                        var item = other.GetItem(target.ItemStateId);
                        // Disable action if other is not givable or this is not receivable
                        if (other.CheckGivable(agvController, item) == ExchangeMessage.Ok
                            && agvController.CheckReceivable(other, item) == ExchangeMessage.Ok)
                        {
                            return i;
                        }
                        break;
                    }
                    case TargetAction.Give:
                    {
                        var item = agvController.GetItem(target.ItemStateId);
                        // Disable action if other is not receivable or this is not givable
                        if (other.CheckReceivable(agvController, item) == ExchangeMessage.Ok
                            && agvController.CheckGivable(other, item) == ExchangeMessage.Ok)
                        {
                            return i;
                        }
                        break;
                    }
                }
            }
            return 0;
        }
    }
}
