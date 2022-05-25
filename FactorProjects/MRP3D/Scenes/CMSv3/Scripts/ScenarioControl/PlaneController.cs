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
            AgentTypeCount = AgentTypeActionSpaceOd.Count;

            var actionSpaceSize = 0;
            string actionSpaceMethod;
            if (globalSetting.UseUnionActionSpace)
            {
                var unionActionSpaceSize = 0;
                foreach (var (_,actions) in AgentTypeActionSpaceOd.Values)
                {
                    actionSpaceSize += actions.Count;
                }

                actionSpaceMethod = "UNION";
            }
            else
            {
                foreach (var (_,actionSpace) in AgentTypeActionSpaceOd.Values)
                {
                    actionSpaceSize = Math.Max(actionSpace.Count, actionSpaceSize);
                }

                actionSpaceMethod = "MAX";
            }
            
            Debug.Log($"Using {actionSpaceMethod} action space, action space size should be: {actionSpaceSize}");
            Debug.Log($"Goal sensor size should be: {AgentTypeCount}");
            // Verify all agent's behaviour parameter settings of action space. Should all be MaxActionSpaceSize
            foreach (var registeredAgent in AgentGroup.GetRegisteredAgents())
            {
                var brainParas = registeredAgent.GetComponent<BehaviorParameters>().BrainParameters;
                var specSize = brainParas.ActionSpec.BranchSizes[0];
                if(specSize!=actionSpaceSize)
                    Debug.LogError(
                        $"{registeredAgent.GetType().Name}: Action space size does not match, " +
                        $"should be {actionSpaceSize}, but set to {specSize}");
            }
            // init productStockDict
            foreach (var itemState in _scenario.model.itemStates)
            {
                if (itemState.type == SpecialItemStateType.Product)
                {
                    productStockOd.Add(itemState.id,0);
                    deliveredProductCount.Add(itemState.id,0);
                }
            }
            InitGameObjectControllerDict();
            InitGameObjectExchangeableDict();
            lastOrderGenerateTime = Time.fixedTime+startGenerateOrderTime;
            //GenerateRandomOrders(initialOrderNum);
            RefreshTrainInfoText();
            RefreshEpisodeInfoText(Time.fixedTime);
        }
        
        public float EpisodeDuration = 1000f;
        [SerializeField]
        [InspectorUtil.DisplayOnly]
        private float currentEpisodeStart = 0;
        [SerializeField]
        [InspectorUtil.DisplayOnly]
        private float lastOrderGenerateTime = 0f;

        private int refreshTextCounter = 0;
        public int textRefreshRate = 30;
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
            foreach (var (ddl,order) in orderSortedList)
            {
                if (now > ddl && !order.IsDue)
                {
                    OrderFailed(order);
                }

                if (dumpOverDueOrderTime>=0 && now > ddl + dumpOverDueOrderTime)
                {
                    toRemove.Add(ddl);
                }
            }
            foreach (var ddl in toRemove)
            {
                orderSortedList.Remove(ddl);
            }

            refreshTextCounter++;
            if (refreshTextCounter > textRefreshRate)
            {
                RefreshEpisodeInfoText(now);
                RefreshTrainInfoText();
                refreshTextCounter = 0;
            }
        }
        
        public TextMeshPro EpisodeInfoText;
        public TextMeshPro TrainInfoText;

        private void RefreshTrainInfoText()
        {
            if(TrainInfoText==null)
                return;
            TrainInfoText.text = $"Episode: {episodeCount}\n" +
                                 $"{agvDispatcherDecisionCount} Total AGV decision\n" +
                                 // $"AGV mask: {_agvMaskCount}\n" +
                                 // $"AGV act: {_agvActionCount}\n" +
                                 $"{workstationDecisionCount} Total WS decision\n" +
                                 $"{_workstationStrangeMask} Strange WS Mask\n";
            // + $"WS mask: {_workstationMaskCount}\n" +
            // $"WS act: {_workstationActionCount}\n";
        }
        public void ChangeText(string text)
        {
            EpisodeInfoText.text = text;
        }
        
        private static int episodeCount = 0;
        private int finishedOrder = 0;
        private int failedOrder = 0;
        private float episodeCumulativeReward = 0f;
        [NonSerialized]
        public static int agvDispatcherDecisionCount = 0;
        //[NonSerialized]
        //public static int _agvActionCount=0,_agvMaskCount=0;
        [NonSerialized]
        public static int workstationDecisionCount = 0;
        //[NonSerialized]
        //public static int _workstationMaskCount = 0, _workstationActionCount = 0;
        [NonSerialized]
        public static int _workstationStrangeMask = 0;
        
        public TextMeshPro countText;
        public void RefreshEpisodeInfoText(float now)
        {
            if (countText != null)
            {
                countText.text = $"<color=white>method: {globalSetting.agvDecisionMethod}</color>\n" +
                                 $"<color=green>complete: {finishedOrder}</color>\n" +
                                 $"<color=red>timeout: {failedOrder}</color>";
            }
            if(EpisodeInfoText==null)
                return;
            StringBuilder sb = new StringBuilder();
            sb.Append($"<color=#00ffffff>Episode time: {now - currentEpisodeStart:0.0}/{EpisodeDuration:0.0}(s)</color>\n");
            sb.Append($"<color=green>Episode finished: {finishedOrder}</color>\n<color=red>Episode Failed: {failedOrder}</color>\n");
            sb.Append($"Cumulative reward: {episodeCumulativeReward:0.00}\n");
            sb.Append("\n<b>Delivered</b>------\n");
            foreach (var (id,num) in deliveredProductCount)
            {
                sb.Append($"{ScenarioLoader.getItemState(id).name}*{num}\n");
            }
            sb.Append("\n<b>Stock</b>----------\n");
            foreach (var (id,num) in productStockOd)
            {
                sb.Append($"{ScenarioLoader.getItemState(id).name}*{num}\n");
            }
            sb.Append("\n<b>Order</b>----------\n");
            foreach (var order in orderSortedList.Values)
            {
                sb.Append(
                    $"[{ScenarioLoader.getItemState(order.ProductId).name}] " +
                    $"<color={((now + 10f) > order.DeadLine ? "red" : "green")}>" +
                    $"{order.DeadLine - now:0.00} sec left" +
                    $"</color>\n");
            }
            ChangeText(sb.ToString());
        }


        protected virtual void ResetPlane()
        {
            episodeCount++;
            AgentGroup.GroupEpisodeInterrupted();
            foreach (var c in WorkstationControllerOd.Values)
            {
                c.EpisodeReset();
            }
            foreach (var c in AgvControllerOd.Values)
            {
                c.EpisodeReset();
            }
            foreach (var k in productStockOd.Keys.ToList())
            {
                productStockOd[k] = 0;
                deliveredProductCount[k] = 0;
            }
            orderSortedList.Clear();
            finishedOrder = 0;
            failedOrder = 0;
            episodeCumulativeReward = 0f;
            lastOrderGenerateTime = Time.fixedTime + startGenerateOrderTime;
            RefreshPromptText("Plane has been reset","white");
        }

        public OrderedDictionary<GameObject, WorkstationController> WorkstationControllerOd;
        public OrderedDictionary<GameObject, AgvController> AgvControllerOd;
        public OrderedDictionary<GameObject, ImportController> ImportControllerDict;
        public OrderedDictionary<GameObject, ExportController> ExportControllerDict;
        private void InitGameObjectControllerDict()
        {
            WorkstationControllerOd = new OrderedDictionary<GameObject, WorkstationController>();
            AgvControllerOd = new OrderedDictionary<GameObject, AgvController>();
            ImportControllerDict = new OrderedDictionary<GameObject, ImportController>();
            ExportControllerDict = new OrderedDictionary<GameObject, ExportController>();
            foreach (var wsObj in EntityGameObjectsDict[typeof(Workstation)])
            {
                WorkstationControllerOd.Add(wsObj,wsObj.GetComponent<WorkstationController>());
            }
            foreach (var agvObj in EntityGameObjectsDict[typeof(Agv)])
            {
                AgvControllerOd.Add(agvObj,agvObj.GetComponent<AgvController>());
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
        public OrderedDictionary<GameObject, IExchangeable> GameObjectExchangeableDict;
        public void InitGameObjectExchangeableDict()
        {
            GameObjectExchangeableDict = new OrderedDictionary<GameObject, IExchangeable>();
            foreach (var c in WorkstationControllerOd.Values)
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
        private OrderedDictionary<string, (int,List<object>)> AgentTypeActionSpaceOd = new OrderedDictionary<string, (int,List<object>)>();
        public void RegisterAgent<T>(EntityAgent<T> agent, string typeName, Func<List<T>> initFunc)
        {
            if (_instantiatedComplete)
                throw new Exception("Instantiation has been complete. Cannot register agent any more.");
            int typeNum;
            if (AgentTypeActionSpaceOd.ContainsKey(typeName))
            {
                // Load stored action space
                var (num,objActs) = AgentTypeActionSpaceOd[typeName];
                typeNum = num;
                agent.ActionSpace = objActs.Cast<T>().ToList();
            }
            else
            {
                // Init and add new action space
                typeNum = AgentTypeActionSpaceOd.Count;
                var a = initFunc();
                agent.ActionSpace = a;
                AgentTypeActionSpaceOd.Add(typeName,(typeNum,a.Cast<object>().ToList()));
            }

            int offset = 0;
            foreach (var (tName, (tNum,actions)) in AgentTypeActionSpaceOd)
            {
                if (tName != typeName)
                {
                    offset += actions.Count;
                }
                else
                {
                    break;
                }
            }
            agent.typeNum = typeNum;
            agent.offset = offset;
            AgentGroup.RegisterAgent(agent);
        }
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

        /// <summary>
        /// All orders at present, sorted by deadline
        /// </summary>
        public SortedList<float,Order> orderSortedList = new SortedList<float,Order>();
        public float minDeadline = 60f;
        public float maxDeadline = 120f;
        public int initialOrderNum = 0;
        public float startGenerateOrderTime = 40f;
        public float orderGenerateInterval = 60f;
        public float dumpOverDueOrderTime = 60f;

        private void GenerateOneRandomOrder()
        {
            var now = Time.fixedTime;
            Order o = new Order(
                ScenarioLoader.ProductItemStates[Random.Range(0, ScenarioLoader.ProductItemStates.Count)].id, 
                now,
                GetNonConflictRandomDeadline());
            if (productStockOd[o.ProductId] > 0)
            {
                productStockOd[o.ProductId]--;
                OrderFinished(o,true);
            }
            else
            {
                orderSortedList.Add(o.DeadLine,o);
            }
        }

        private void GenerateRandomOrders(int num)
        {
            for (int i = 0; i < num; i++)
            {
                GenerateOneRandomOrder();
            }
        }

        private float GetNonConflictRandomDeadline()
        {
            var ddl = GetRandomDeadline();
            while (orderSortedList.ContainsKey(ddl))
            {
                ddl = GetRandomDeadline();
            }

            return ddl;
        }
        private float GetRandomDeadline()
        {
            return Time.fixedTime + Random.Range(minDeadline, maxDeadline);
        }
        public OrderedDictionary<string, int> productStockOd = new OrderedDictionary<string, int>();
        public void DeliverProduct(Item item)
        {
            productStockOd[item.itemState.id]++;
            deliveredProductCount[item.itemState.id]++;
            Destroy(item.gameObject);
            CheckStockFinishOrder();
        }

        /// <summary>
        /// Check in product stock whether there are orders can be finished
        /// If so, finish these orders and remove unit from product stock
        /// </summary>
        public void CheckStockFinishOrder()
        {
            List<float> toRemove = new List<float>();
            foreach (var (ddl,o) in orderSortedList)
            {
                if (productStockOd[o.ProductId] > 0)
                {
                    productStockOd[o.ProductId]--;
                    toRemove.Add(ddl);
                }
            }
            foreach (var ddl in toRemove)
            {
                OrderFinished(orderSortedList[ddl]);
                orderSortedList.Remove(ddl);
            }
        }

        public TextMeshPro PromptText;

        private void RefreshPromptText(string text, string color)
        {
            if(PromptText==null)
                return;
            PromptText.text = $"<color={color}>"+text+"</color>";
        }

        public float OrderFailReward = -.1f;
        public float StockFinishNewOrderReward = .02f;
        public float OrderFinishReward = .1f;
        public float FinishOrderTimeCostRewardFactor = -.1f;
        private OrderedDictionary<string, int> deliveredProductCount = new OrderedDictionary<string, int>();
        protected virtual void OrderFinished(Order o,bool isNewOrder=false)
        {
            finishedOrder++;
            // if (isNewOrder)
            // {
            //     AgentGroup.AddGroupReward(StockFinishNewOrderReward);
            //     Debug.LogFormat("Finished {0} order by stock. Reward: {1:0.00}",
            //         ScenarioLoader.getItemState(o.ProductId).name, StockFinishNewOrderReward);
            //     ground.FlipColor(Ground.GroundSwitchColor.Yellow);
            //     return;
            // }
            var timeLeft = o.DeadLine - Time.fixedTime;
            var timeCost = Time.fixedTime - o.GenerateTime;
            var r = OrderFinishReward*(1-timeCost/NormValues.OrderTimeMaxValue);
            AgentGroup.AddGroupReward(r);
            episodeCumulativeReward += r;
            RefreshPromptText(
                $"Finished {ScenarioLoader.getItemState(o.ProductId).name} order " +
                              $"use {timeCost:0.00} seconds, {timeLeft:0.00} seconds before deadline. Reward: {r:0.00}",
                "green");
            ground.FlipColor(Ground.GroundSwitchColor.Green);
        }

        protected virtual void OrderFailed(Order o)
        {
            o.IsDue = true;
            failedOrder++;
            var r = OrderFailReward;
            AgentGroup.AddGroupReward(r);
            episodeCumulativeReward += r;
            RefreshPromptText(
                $"Failed {ScenarioLoader.getItemState(o.ProductId).name} order. Reward: {OrderFailReward:0.00}",
                "red");
            ground.FlipColor(Ground.GroundSwitchColor.Red);
        }

        #endregion
        private Dictionary<string, List<WorkstationController>> processWorkstationsDict = new ();
        private List<WorkstationController> GetProcessCandidateWorkstations(string pId)
        {
            if (!processWorkstationsDict.ContainsKey(pId))
            {
                var candidateWorkstations = WorkstationControllerOd.Values.Where(controller => controller.Workstation.supportProcessesRef.Any(pRef => pRef.idref == pId)).ToList();
                // Find all workstations support this operation
                if (candidateWorkstations.Count == 0)
                {
                    throw new Exception(
                        "Find an operation with process that is not supported by any workstation");
                }
                processWorkstationsDict.Add(pId,candidateWorkstations);
            }

            return processWorkstationsDict[pId];
        }

        private Dictionary<string, float> estimateProcessCost = new();

        public float GetEstimateProcessCost(string productId)
        {
            if (!estimateProcessCost.ContainsKey(productId))
            {
                float etc = 0.0f;
                var processes = ScenarioLoader.GetDfsOperation(productId);
                var importPosition = ImportControllerDict.Keys.Single().transform.position;
                var exportPosition = ExportControllerDict.Keys.Single().transform.position;
                foreach (var process in processes)
                {
                    var workstations = GetProcessCandidateWorkstations(process.id);
                    // Sum up raw material deliver average distance
                    foreach (var iRef in process.inputItemsRef)
                    {
                        if (ScenarioLoader.getItemState(iRef.idref).type == SpecialItemStateType.Raw)
                        {
                            etc+=workstations.Sum(ws =>
                                Math.Abs((importPosition - ws.transform.position).magnitude) / workstations.Count);
                        }
                        else
                        {
                            // TODO: Should be length to previous workstation
                            etc+=workstations.Sum(ws =>
                                Math.Abs((transform.position - ws.transform.position).magnitude) / workstations.Count);
                        }
                    }

                    foreach (var itemState in process.outputItemsRef.Select(iRef=>ScenarioLoader.getItemState(iRef.idref)))
                    {
                        if (itemState.type == SpecialItemStateType.Product)
                        {
                            etc+=workstations.Sum(ws =>
                                Math.Abs((exportPosition - ws.transform.position).magnitude) / workstations.Count);
                        }
                    }

                    etc += process.duration;
                }

                estimateProcessCost.Add(productId, etc);
            }

            return estimateProcessCost[productId];
        }

    }
}
