using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class AgvDispatcherAgent : EntityAgent<Target>
    {
        [SerializeField]
        private AgvController agvController;

        public override List<Target> InitActionSpace()
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

            //ScenarioLoader.InitProductRelateTargetsDict(agvDispatcherActionSpace);
            return agvDispatcherActionSpace;
        }
        
        // public override void OnActionReceived(ActionBuffers actions)
        // {
        //     var action = actions.DiscreteActions[0];
        //     agvController.AssignNewTarget(ActionSpace[action]);
        // }
        protected override void OnActionReceivedProtected(Target target)
        {
            agvController.AssignNewTarget(target);
        }

        private Target GetRelatedPriorestValidTarget(string productId)
        {
            int I(Dictionary<string, int> dictionary, Target t)
            {
                if(dictionary.ContainsKey(t.ItemStateId))
                    return dictionary[t.ItemStateId];
                return -1;
            }

            var targets = GetNotNullValidTargets();
            if (targets.Count == 0) return null;
            var pd = ScenarioLoader.ProductItemPriorityDict[productId];
            var candidate = targets.Aggregate((t1,t2)=>I(pd, t1)>I(pd, t2)?t1:t2);
            if (I(pd, candidate) > 0)
                return candidate;
            else
            {
                return targets.FindAll(t => I(pd, t) == 0).GetRandomItem();
            }
        }
        public void RequestTargetDecision()
        {
            PlaneController.agvDispatcherDecisionCount++;
            if (PlaneController.orderSortedList.Count == 0&&
                PlaneController.globalSetting.agvDecisionMethod!=GlobalSetting.DecisionMethod.HETE)
            {
                AssignTargetRandom();
                return;
            }
            switch (PlaneController.globalSetting.agvDecisionMethod)
            {
                case GlobalSetting.DecisionMethod.HETE:
                    RequestDecision();
                    break;
                case GlobalSetting.DecisionMethod.RVA:
                    AssignTargetRandom();
                    break;
                case GlobalSetting.DecisionMethod.EDD:
                    // var useful = GetNotNullValidTargets().FindAll(t =>
                    //     !PlaneController.ImportControllerDict.ContainsKey(t.GameObject));
                    // agvController.AssignNewTarget(useful.Count > 0 ? useful[0] : GetFirstValidTarget());
                    AssignTargetAccordOrder(PlaneController.orderSortedList.Values[0]);
                    break;
                case GlobalSetting.DecisionMethod.FCFS:
                    AssignTargetAccordOrder(PlaneController.orderSortedList.Values
                        .Aggregate((o1,o2)=>o1.GenerateTime<o2.GenerateTime?o1:o2));
                    break;
                case GlobalSetting.DecisionMethod.LCFS:
                    AssignTargetAccordOrder(PlaneController.orderSortedList.Values
                        .Aggregate((o1,o2)=>o1.GenerateTime>o2.GenerateTime?o1:o2));
                    break;
                case GlobalSetting.DecisionMethod.SET:
                    // agvController.AssignNewTarget(
                    //     GetRandomizedNotNullValidTargets().Aggregate((t1, t2) => 
                    //         EstimateDistance(t1) < EstimateDistance(t2) ? t1 : t2)
                    // );
                    AssignTargetAccordOrder(PlaneController.orderSortedList.Values
                        .Aggregate((o1,o2)=>
                            PlaneController.GetEstimateProcessCost(o1.ProductId)<
                            PlaneController.GetEstimateProcessCost(o2.ProductId)?o1:o2));
                    break;
                case GlobalSetting.DecisionMethod.LET:
                    // agvController.AssignNewTarget(
                    //     GetRandomizedNotNullValidTargets().Aggregate((t1, t2) => 
                    //             EstimateDistance(t1) > EstimateDistance(t2) ? t1 : t2));
                    AssignTargetAccordOrder(PlaneController.orderSortedList.Values
                        .Aggregate((o1,o2)=>
                            PlaneController.GetEstimateProcessCost(o1.ProductId)>
                            PlaneController.GetEstimateProcessCost(o2.ProductId)?o1:o2));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void AssignTargetAccordOrder(Order o)
        {
            var priorTarget = GetRelatedPriorestValidTarget(o.ProductId);
            if(priorTarget==null)
                AssignTargetRandom();
            else
                agvController.AssignNewTarget(priorTarget);
        }

        private void AssignTargetRandom()
        {
            agvController.AssignNewTarget(ActionSpace[GetRandomValidTargetIndex()]);
        }

        public bool useMask = false;
        protected override List<int> WriteDiscreteActionMaskProtected()
        {
            var mask = new List<int>();
            if (!useMask)
                return mask;
            for (int i = 0; i < ActionSpace.Count; i++)
            {
                var target = ActionSpace[i];
                if (target == null)
                {
                    // Mask null target action
                    mask.Add(i);
                    continue;
                }
                var other = PlaneController.GameObjectExchangeableDict[target.GameObject];
                switch (target.TargetAction)
                {
                    case TargetAction.Get:
                    {
                        var item = other.GetItem(target.ItemStateId);
                        // Disable action if other is not givable or this is not receivable
                        if(other.CheckGivable(agvController,item)!=ExchangeMessage.Ok
                           ||agvController.CheckReceivable(other,item)!=ExchangeMessage.Ok)
                            mask.Add(i);
                        break;
                    }
                    case TargetAction.Give:
                    {
                        var item = agvController.GetItem(target.ItemStateId);
                        // Disable action if other is not receivable or this is not givable
                        if(other.CheckReceivable(agvController,item)!=ExchangeMessage.Ok
                           ||agvController.CheckGivable(other,item)!=ExchangeMessage.Ok)
                            mask.Add(i);
                        break;
                    }
                }
            }

            return mask;
        }
        
        protected override void CollectObservationsProtected(VectorSensor sensor)
        {
            //collect relative position and status of all workstations
            foreach (var (wsObj,wsController) in PlaneController.WorkstationControllerOd)
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
            foreach (var (agvObj,agvCtrl) in PlaneController.AgvControllerOd)
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
            foreach (var stock in PlaneController.productStockOd.Values)
            {
                sensor.AddObservation(Utils.NormalizeValue(stock,0,NormValues.StockCountMaxValue));
            }
            
            // Collect order info
            int orderCount = 0;
            foreach (var (_,order) in PlaneController.orderSortedList)
            {
                sensor.AddObservation(1.0f-Math.Clamp(order.DeadLine-Time.fixedTime,0f,NormValues.OrderTimeMaxValue)/NormValues.OrderTimeMaxValue);
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
        

        protected override int HeuristicProtected()
        {
            return GetRandomValidTargetIndex();
        }
        //give a random valid currentTarget
        private int GetRandomValidTargetIndex()
        {
            var validTargets = GetNotNullValidTargetIndexes();
            return validTargets.Count > 0 ? validTargets[Random.Range(0, validTargets.Count)] : 0;
        }
        private Target GetFirstValidTarget()
        {
            var validTargets = GetNotNullValidTargetIndexes();
            var index= validTargets.Count > 0 ? validTargets[0] : 0;
            return ActionSpace[index];
        }

        private List<int> GetNotNullValidTargetIndexes()
        {
            List<int> valids = new ();
            for (int i = 0; i < ActionSpace.Count; i++)
            {
                var target = ActionSpace[i];
                if(target==null)
                    continue;
                if (CheckTargetValid(target)) valids.Add(i);
            }

            return valids;
        }
        private List<Target> GetNotNullValidTargets()
        {
            return GetNotNullValidTargetIndexes().Select(i => ActionSpace[i]).ToList();
        }

        private List<Target> GetRandomizedNotNullValidTargets()
        {
            var targets = GetNotNullValidTargets();
            targets.Shuffle();
            return targets;
        }

        private bool CheckTargetValid([NotNull] Target target)
        {
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
                        return true;
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
                        return true;
                    }
                    break;
                }
            }

            return false;
        }
        
        private float EstimateDistance(Target target)
        {
            // Calculate the direct distance from AGV's last transport put position to new transport's pick position
            return (transform.position - target.GameObject.transform.position)
                .magnitude;
        }
        
    }
}
