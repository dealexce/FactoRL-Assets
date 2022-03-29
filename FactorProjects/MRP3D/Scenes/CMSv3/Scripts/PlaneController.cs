using System.Collections.Generic;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class PlaneController : ScenarioGenerator
    {

        new void Start()
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
        }

        public Dictionary<GameObject, IExchangeable> GameObjectExchangeableDict;
        public void InitGameObjectExchangeableDict()
        {
            GameObjectExchangeableDict = new Dictionary<GameObject, IExchangeable>();
            ItemOdUtils.IterateLists(
                EntityGameObjectsDict.Values,
                itemAction: o =>
                {
                    var exchangable = (IExchangeable) o.GetComponent(typeof(IExchangeable));
                    if(exchangable!=null)
                        GameObjectExchangeableDict.Add(o,exchangable);
                });
        }

        #region RL

        public float normDistanceMaxValue = 50f;
        public float normStockCountMaxValue = 10;

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

        public List<Target> AgvDispatcherActionSpace { get; private set; }
        private void InitAgvDispatcherActionSpace()
        {
            AgvDispatcherActionSpace = new List<Target>();
            AgvDispatcherActionSpace.Add(PConsts.NullTarget);
            foreach (var wsObj in EntityGameObjectsDict[typeof(Workstation)])
            {
                // possible [give x input item state] actions to workstation
                var controller = wsObj.GetComponent<WorkstationController>();
                foreach (var itemStateId in controller.InputBufferItems.Keys)
                {
                    AgvDispatcherActionSpace.Add(new Target(
                        controller.inputPlateGameObject,
                        TargetAction.Give,
                        itemStateId));
                }
                // possible [get x output item state] actions to workstation
                foreach (var itemStateId in controller.OutputBufferItems.Keys)
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
                foreach (var iId in controller.rawItemsDict.Keys)
                {
                    AgvDispatcherActionSpace.Add(new Target(
                        isObj,
                        TargetAction.Get,
                        iId));
                }
            }
        }
        

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
