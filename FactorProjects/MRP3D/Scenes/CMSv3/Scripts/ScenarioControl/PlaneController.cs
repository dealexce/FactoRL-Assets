﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class PlaneController : ScenarioGenerator
    {

        public new void Start()
        {
            base.Start();
            // init productStockDict
            foreach (var itemState in _scenario.model.itemStates)
            {
                if (itemState.type == SpecialItemStateType.Product)
                {
                    productStockDict.Add(itemState.id,0);
                }
            }
            InitRl();
            InitGameObjectExchangeableDict();
        }

        /// <summary>
        /// Override ScenarioGenerator InstantiateEntityOnGround(), call base method,
        /// and additionally link components that implements ILinkedToPlane
        /// </summary>
        /// <param name="type"></param>
        /// <param name="prefab"></param>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        protected override GameObject InstantiateEntityOnGround(Type type, GameObject prefab, float x, float z)
        {
            var g = base.InstantiateEntityOnGround(type, prefab, x, z);
            // link to this as PlaneController if implements ILinkedToPlane
            foreach (var c in g.GetComponentsInChildren(typeof(ILinkedToPlane)))
            {
                if(c is ILinkedToPlane l)
                    l.PlaneController = this;
            }
            return g;
        }

        /// <summary>
        /// Dictionary from GameObject to IExchangeable instances. This will NOT be updated after Start()!
        /// </summary>
        public Dictionary<GameObject, IExchangeable> GameObjectExchangeableDict;
        public void InitGameObjectExchangeableDict()
        {
            GameObjectExchangeableDict = new Dictionary<GameObject, IExchangeable>();
            ItemOdUtils.IterateLists(
                EntityGameObjectsDict.Values,
                itemAction: o =>
                {
                    var exchangeable = (IExchangeable) o.GetComponent(typeof(IExchangeable));
                    if(exchangeable!=null)
                        GameObjectExchangeableDict.Add(o,exchangeable);
                });
        }
        

        #region RL



        public Dictionary<GameObject, WorkstationController> WorkstationControllerDict;
        public Dictionary<GameObject, AgvController> AgvControllerDict;
        private void InitRl()
        {
            WorkstationControllerDict = new Dictionary<GameObject, WorkstationController>();
            AgvControllerDict = new Dictionary<GameObject, AgvController>();
            foreach (var wsObj in EntityGameObjectsDict[typeof(Workstation)])
            {
                WorkstationControllerDict.Add(wsObj,wsObj.GetComponent<WorkstationController>());
            }
            foreach (var agvObj in EntityGameObjectsDict[typeof(Agv)])
            {
                AgvControllerDict.Add(agvObj,agvObj.GetComponent<AgvController>());
            }
            
            InitActionSpaces();
        }
        private void InitActionSpaces()
        {
            InitAgvDispatcherActionSpace();
        }

        // At present, all AGVs have identical action spaces,
        // therefore initiate it at PlaneController once for all.
        public List<Target> AgvDispatcherActionSpace { get; private set; }
        private void InitAgvDispatcherActionSpace()
        {
            AgvDispatcherActionSpace = new List<Target>();
            // Target==null refers to no target
            AgvDispatcherActionSpace.Add(null);
            foreach (var wsObj in EntityGameObjectsDict[typeof(Workstation)])
            {
                // possible [give x input item state] actions to workstation
                var controller = wsObj.GetComponent<WorkstationController>();
                foreach (var itemStateId in controller.InputBufferItemsDict.Keys)
                {
                    AgvDispatcherActionSpace.Add(new Target(
                        controller.inputPlateGameObject,
                        TargetAction.Give,
                        itemStateId));
                }
                // possible [get x output item state] actions to workstation
                foreach (var itemStateId in controller.OutputBufferItemsDict.Keys)
                {
                    AgvDispatcherActionSpace.Add(new Target(
                        controller.outputPlateGameObject,
                        TargetAction.Get,
                        itemStateId));
                }
            }
            
            // possible give product actions to export station
            foreach (var esObj in EntityGameObjectsDict[typeof(ExportStation)])
            {
                foreach (var iId in productStockDict.Keys)
                {
                    AgvDispatcherActionSpace.Add(new Target(
                        esObj,
                        TargetAction.Give,
                        iId));
                }
            }
            // possible get raw actions to import station
            foreach (var isObj in EntityGameObjectsDict[typeof(ImportStation)])
            {
                var controller = isObj.GetComponent<ImportController>();
                foreach (var iId in controller.RawItemsDict.Keys)
                {
                    AgvDispatcherActionSpace.Add(new Target(
                        isObj,
                        TargetAction.Get,
                        iId));
                }
            }
        }
        
        // Different type of workstations have different action spaces,
        // therefore it is initiated in Start() at each WorkstationAgent.
        //private void InitWorkstationActionSpace(){}
        

        #endregion

        
        
        
        public GameObject itemPrefab;
        public Item InstantiateItem(string id, GameObject parentObj)
        {
            ItemState itemState = SceanrioLoader.getItemState(id);
            if (itemState!=null)
            {
                GameObject obj = Instantiate(itemPrefab, parentObj.transform);
                Item item = obj.GetComponent<Item>();
                item.SetItemState(itemState);
                return item;
            }
            else
            {
                Debug.LogError("Tried to instantiate an item not defined in description model: "+id);
                return null;
            }
        }

        #region Order

        public SortedList<float,Order> orderList = new SortedList<float,Order>();
        public float minDeadline { get; } = 60f;
        public float maxDeadline { get; } = 120f;

        private void GenerateOneRandomOrder()
        {
            Order o = new Order(
                SceanrioLoader.ProductItemStates[Random.Range(0, SceanrioLoader.ProductItemStates.Count)].id, 
                GetRandomNonConflictDeadline());
            
            orderList.Add(o.deadLine,o);
            CheckStockFinishOrder();
        }
        private float GetRandomNonConflictDeadline()
        {
            float ddl = Time.fixedTime + Random.Range(minDeadline, maxDeadline);
            while (orderList.ContainsKey(ddl))
            {
                ddl = Time.fixedTime + Random.Range(minDeadline, maxDeadline);
            }
            return ddl;
        }
        public Dictionary<string, int> productStockDict = new Dictionary<string, int>();
        public void DeliverProduct(Item item)
        {
            productStockDict[item.itemState.id]++;
            Destroy(item.gameObject);
        }

        /// <summary>
        /// Check in product stock whether there are orders can be finished
        /// If so, finish these orders and remove unit from product stock
        /// </summary>
        public void CheckStockFinishOrder()
        {
            List<Order> toRemove = new List<Order>();
            foreach (var o in orderList.Values)
            {
                if (productStockDict[o.ProductId] > 0)
                {
                    toRemove.Add(o);
                }
            }
            foreach (var o in toRemove)
            {
                productStockDict[o.ProductId]--;
                orderList.Remove(o.deadLine);
                OrderFinished(o);
            }
        }

        private void OrderFinished(Order o)
        {
            // _AGVMultiAgentGroup.AddGroupReward(.1f);
            // groundController.FlipColor(Ground.GroundSwitchColor.Green);
            // finishedOrders++;
        }

        #endregion
        
        
    }
}