using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OD;
using TMPro;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class PlaneController : ScenarioGenerator
    {
        public new void Start()
        {
            base.Start();
            _instantiatedComplete = true;
            AgentTypeCount = Math.Max(AgentTypeCount, AgentTypeActionSpaceDict.Count);
            foreach (var (id,actionSpace) in AgentTypeActionSpaceDict.Values)
            {
                MaxActionSpaceSize = Math.Max(actionSpace.Count, MaxActionSpaceSize);
            }
            Debug.LogFormat("Max action space size is: {0}",MaxActionSpaceSize);
            Debug.LogFormat("Goal sensor size should be: {0}",AgentTypeCount);
            // Verify all agent's behaviour parameter settings of action space. Should all be MaxActionSpaceSize
            foreach (var registeredAgent in AgentGroup.GetRegisteredAgents())
            {
                var specSize = registeredAgent.GetComponent<BehaviorParameters>().BrainParameters.ActionSpec.BranchSizes[0];
                if(specSize!=MaxActionSpaceSize)
                    Debug.LogErrorFormat(
                        "{0}: Action space size does not match, should be {1}, but set to {2}",
                        registeredAgent.GetType().Name,
                        MaxActionSpaceSize,specSize);
            }
            // init productStockDict
            foreach (var itemState in _scenario.model.itemStates)
            {
                if (itemState.type == SpecialItemStateType.Product)
                {
                    productStockDict.Add(itemState.id,0);
                }
            }
            InitGameObjectControllerDict();
            InitGameObjectExchangeableDict();
            GenerateRandomOrders(initialOrderNum);
        }

        public float EpisodeDuration = 300f;
        [SerializeField]
        [InspectorUtil.DisplayOnly]
        private float currentEpisodeStart = 0;
        [SerializeField]
        [InspectorUtil.DisplayOnly]
        private float lastOrderGenerateTime = 0f;
        private void FixedUpdate()
        {
            // Check Whether episode is end and should reset plane
            var now = Time.fixedTime;
            if (now-currentEpisodeStart > EpisodeDuration)
            {
                ResetPlane();
                currentEpisodeStart = now;
            }
            // Check whether should generate an order
            if (now-lastOrderGenerateTime > orderGenerateInterval)
            {
                GenerateOneRandomOrder();
                lastOrderGenerateTime = now;
            }
            // Check in orders whether timeout. If so, call OrderFailed() and remove it from orderList
            var toRemove = new List<float>();
            foreach (var (ddl,order) in orderList)
            {
                if (now > ddl)
                {
                    toRemove.Add(ddl);
                    OrderFailed(order);
                }
                else
                {
                    break;
                }
            }
            foreach (var ddl in toRemove)
            {
                orderList.Remove(ddl);
            }
            RefreshOrderText(now);
        }
        
        public TextMeshPro textMeshPro;
        public void ChangeText(string text)
        {
            textMeshPro.text = text;
        }

        public void RefreshOrderText(float now)
        {
            if(textMeshPro==null)
                return;
            StringBuilder sb = new StringBuilder();
            sb.Append($"<color=blue>Episode time: {now - currentEpisodeStart:0.0}/{EpisodeDuration:0.0}(s)</color>\n");
            sb.Append("\nStock----------\n");
            foreach (var (id,num) in productStockDict)
            {
                
                sb.Append($"{ScenarioLoader.getItemState(id).name}*{num}\n");
            }
            sb.Append("\nOrder----------\n");
            foreach (var (ddl,order) in orderList)
            {
                sb.Append(
                    $"[{ScenarioLoader.getItemState(order.ProductId).name}] <color={((now + 10f) > ddl ? "red" : "green")}>{ddl - now:0.00} sec left</color>\n");
            }
            ChangeText(sb.ToString());
        }

        private void ResetPlane()
        {
            AgentGroup.EndGroupEpisode();
            foreach (var c in WorkstationControllerDict.Values)
            {
                c.EpisodeReset();
            }
            foreach (var c in AgvControllerDict.Values)
            {
                c.EpisodeReset();
            }
            foreach (var k in productStockDict.Keys.ToList())
            {
                productStockDict[k] = 0;
            }
            orderList.Clear();
        }

        public Dictionary<GameObject, WorkstationController> WorkstationControllerDict;
        public Dictionary<GameObject, AgvController> AgvControllerDict;
        public Dictionary<GameObject, ImportController> ImportControllerDict;
        public Dictionary<GameObject, ExportController> ExportControllerDict;
        private void InitGameObjectControllerDict()
        {
            WorkstationControllerDict = new Dictionary<GameObject, WorkstationController>();
            AgvControllerDict = new Dictionary<GameObject, AgvController>();
            ImportControllerDict = new Dictionary<GameObject, ImportController>();
            ExportControllerDict = new Dictionary<GameObject, ExportController>();
            foreach (var wsObj in EntityGameObjectsDict[typeof(Workstation)])
            {
                WorkstationControllerDict.Add(wsObj,wsObj.GetComponent<WorkstationController>());
            }
            foreach (var agvObj in EntityGameObjectsDict[typeof(Agv)])
            {
                AgvControllerDict.Add(agvObj,agvObj.GetComponent<AgvController>());
            }
            foreach (var o in EntityGameObjectsDict[typeof(ImportStation)])
            {
                ImportControllerDict.Add(o,o.GetComponent<ImportController>());
            }
            foreach (var o in EntityGameObjectsDict[typeof(ExportStation)])
            {
                ExportControllerDict.Add(o,o.GetComponent<ExportController>());
            }
        }
        /// <summary>
        /// Dictionary from GameObject to IExchangeable instances. This will NOT be updated after Start()!
        /// </summary>
        public Dictionary<GameObject, IExchangeable> GameObjectExchangeableDict;
        public void InitGameObjectExchangeableDict()
        {
            GameObjectExchangeableDict = new Dictionary<GameObject, IExchangeable>();
            foreach (var c in WorkstationControllerDict.Values)
            {
                GameObjectExchangeableDict.Add(c.inputPlateGameObject,c);
                GameObjectExchangeableDict.Add(c.outputPlateGameObject,c);
            }
            foreach (var c in ImportControllerDict.Values)
            {
                GameObjectExchangeableDict.Add(c.gameObject,c);
            }
            foreach (var c in ExportControllerDict.Values)
            {
                GameObjectExchangeableDict.Add(c.gameObject,c);
            }
            // ItemOdUtils.IterateLists(
            //     EntityGameObjectsDict.Values,
            //     itemAction: o =>
            //     {
            //         var exchangeable = (IExchangeable) o.GetComponent(typeof(IExchangeable));
            //         if(exchangeable!=null)
            //             GameObjectExchangeableDict.Add(o,exchangeable);
            //     });
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
            if (g.GetComponent(typeof(IResettable)) is IResettable resettable)
                resettable.InitPosition = g.transform.position;
            return g;
        }

        private bool _instantiatedComplete = false;
        private SimpleMultiAgentGroup AgentGroup = new SimpleMultiAgentGroup();
        private Dictionary<string, (int,List<object>)> AgentTypeActionSpaceDict = new Dictionary<string, (int,List<object>)>();
        public int RegisterAgent<T>(EntityAgent<T> agent, string typeId, Func<List<T>> initFunc)
        {
            if (_instantiatedComplete)
                throw new Exception("Instantiation has been complete. Cannot register agent any more.");
            int typeNum;
            if (AgentTypeActionSpaceDict.ContainsKey(typeId))
            {
                var a = AgentTypeActionSpaceDict[typeId];
                typeNum = a.Item1;
                
                agent.ActionSpace = a.Item2.Cast<T>().ToList();
            }
            else
            {
                typeNum = AgentTypeActionSpaceDict.Count;
                var a = initFunc();
                agent.ActionSpace = a;
                AgentTypeActionSpaceDict.Add(typeId,(typeNum,a.Cast<object>().ToList()));
            }
            AgentGroup.RegisterAgent(agent);
            return typeNum;
        }

        public static int MaxActionSpaceSize { get; private set; } = 0;
        public static int AgentTypeCount { get; private set; } = 0;



        public GameObject itemPrefab;
        public Item InstantiateItem(string id, GameObject parentObj)
        {
            ItemState itemState = ScenarioLoader.getItemState(id);
            if (itemState!=null)
            {
                GameObject obj = Instantiate(itemPrefab, parentObj.transform);
                Item item = obj.GetComponent<Item>();
                item.SetItemState(itemState);
                return item;
            }
            Debug.LogError("Tried to instantiate an item not defined in description model: "+id);
            return null;
        }

        #region Order

        public SortedList<float,Order> orderList = new SortedList<float,Order>();
        public float minDeadline = 60f;
        public float maxDeadline = 120f;
        public int initialOrderNum = 0;
        public float orderGenerateInterval = 60f;

        private void GenerateOneRandomOrder()
        {
            Order o = new Order(
                ScenarioLoader.ProductItemStates[Random.Range(0, ScenarioLoader.ProductItemStates.Count)].id, 
                GetRandomNonConflictDeadline());
            if (productStockDict[o.ProductId] > 0)
            {
                productStockDict[o.ProductId]--;
                OrderFinished(o,true);
            }
            else
            {
                orderList.Add(o.deadLine,o);
            }
        }

        private void GenerateRandomOrders(int num)
        {
            for (int i = 0; i < num; i++)
            {
                GenerateOneRandomOrder();
            }
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
            CheckStockFinishOrder();
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
                    productStockDict[o.ProductId]--;
                    toRemove.Add(o);
                }
            }
            foreach (var o in toRemove)
            {
                orderList.Remove(o.deadLine);
                OrderFinished(o);
            }
        }

        public float OrderFailReward = -.1f;
        public float StockFinishNewOrderReward = .02f;
        public float OrderFinishReward = .1f;
        public float FinishAheadRewardFactor = .01f;
        private void OrderFinished(Order o,bool isNewOrder=false)
        {
            if (isNewOrder)
            {
                AgentGroup.AddGroupReward(StockFinishNewOrderReward);
                Debug.LogFormat("Finished {0} order by stock. Reward: {1:0.00}",
                    ScenarioLoader.getItemState(o.ProductId).name, StockFinishNewOrderReward);
                ground.FlipColor(Ground.GroundSwitchColor.Yellow);
                return;
            }
            var timeLeft = o.deadLine - Time.fixedTime;
            var r = OrderFinishReward + FinishAheadRewardFactor * timeLeft;
            AgentGroup.AddGroupReward(r);
            Debug.LogFormat("Finished {0} order {1:0.00} seconds before deadline. Reward: {2:0.00}",
                ScenarioLoader.getItemState(o.ProductId).name, timeLeft, r);
            ground.FlipColor(Ground.GroundSwitchColor.Green);
        }

        private void OrderFailed(Order o)
        {
            AgentGroup.AddGroupReward(OrderFailReward);
            Debug.LogFormat("Failed {0} order. Reward: {1:0.00}",ScenarioLoader.getItemState(o.ProductId).name,OrderFailReward);
            ground.FlipColor(Ground.GroundSwitchColor.Red);
        }

        #endregion
        
        
    }
}
