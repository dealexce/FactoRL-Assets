using System;
using System.Collections;
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
using UnityEngine.Serialization;
using Object = System.Object;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class PlaneController : ScenarioGenerator
    {
        public GlobalSetting globalSetting;
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
                if (now > ddl)
                {
                    toRemove.Add(ddl);
                    OrderFailed(order);
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
        
        private int episodeCount = 0;
        private int finishedOrder = 0;
        private int failedOrder = 0;
        private float episodeCumulativeReward = 0f;
        [NonSerialized]
        public int agvDispatcherDecisionCount = 0;
        //[NonSerialized]
        //public int _agvActionCount=0,_agvMaskCount=0;
        [NonSerialized]
        public int workstationDecisionCount = 0;
        //[NonSerialized]
        //public int _workstationMaskCount = 0, _workstationActionCount = 0;
        [NonSerialized]
        public int _workstationStrangeMask = 0;
        public void RefreshEpisodeInfoText(float now)
        {
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
        public void RegisterAgent<T>(EntityAgent<T> agent, string typeName)
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
                var a = agent.InitActionSpace();
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

            if (centralHeuristic) ScheduleOrder(o);
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
            if (isNewOrder)
            {
                AgentGroup.AddGroupReward(StockFinishNewOrderReward);
                Debug.LogFormat("Finished {0} order by stock. Reward: {1:0.00}",
                    ScenarioLoader.getItemState(o.ProductId).name, StockFinishNewOrderReward);
                ground.FlipColor(Ground.GroundSwitchColor.Yellow);
                return;
            }
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


        public bool centralHeuristic = false;
        
        private void ScheduleOrder(Order order)
        {
            // Record the selection of workstation instance for middle items in the stack
            var wStack = new Stack<(WorkstationController,string)>();
            // This should and should only work when using DFS to process tree
            var pList = ScenarioLoader.GetDfsOperation(order.ProductId);
            foreach (var pCurrent in pList)
            {
                // allocate a workstation for this process
                var m = AllocateProcessLl(pCurrent);
                // allocate an AGV for each input item
                foreach (var item in pCurrent.inputItemsRef.Select(iRef=>ScenarioLoader.getItemState(iRef.idref)))
                {
                    // If the input is raw, get from import station, otherwise get from stack
                    AllocateTransportLl(item.type == SpecialItemStateType.Raw
                        ? new Transport(SelectImportStation(), m.inputPlateGameObject, item.id)
                        : new Transport(wStack.Pop().Item1.outputPlateGameObject, m.inputPlateGameObject, item.id));
                }
                wStack.Push((m,pCurrent.outputItemsRef.Single().idref));
            }
            // The only element remain in the stack is the product item, allocate an AGV to transport it to export station
            var (ws, productId) = wStack.Pop();
            AllocateTransportLl(new Transport(ws.outputPlateGameObject, SelectExportStation(), productId));
        }

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

        private GameObject SelectImportStation()
        {
            return ImportControllerDict.Keys.First();
        }

        private GameObject SelectExportStation()
        {
            return ExportControllerDict.Keys.First();
        }
        
        /// <summary>
        /// Allocate process to workstation with minimum process load
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        private WorkstationController AllocateProcessLl(Process process)
        {
            var pId = process.id;
            // Allocate operation to the support workstation with minimum schedule
            var candidates = GetProcessCandidateWorkstations(pId);
            var selected = candidates.Aggregate((c1, c2) => c1.Schedule.Count < c2.Schedule.Count ? c1 : c2);
            selected.Schedule.Enqueue(process);
            selected.TryPushSchedule();
            return selected;
        }
        
        /// <summary>
        /// Allocate transport to AGV with minimum transport load
        /// </summary>
        /// <param name="transport"></param>
        /// <returns></returns>
        private AgvController AllocateTransportLl(Transport transport)
        {
            // Select the agv with minimum schedule load
            var selected =
                AgvControllerOd.Values.Aggregate((a1, a2) => a1.Schedule.Count < a2.Schedule.Count ? a1 : a2);
            selected.Schedule.Add(transport);
            selected.TryPushSchedule();
            return selected;
        }
        /// <summary>
        /// Select the AGV that the distance from its last transport put position to this new transport's pick position
        /// is minimum, and allocate the transport to selected AGV
        /// </summary>
        /// <param name="transport"></param>
        /// <returns></returns>
        // private AgvController AllocateTransportLtt(Transport transport)
        // {
        //     // Select the agv with minimum schedule load
        //     var selected =
        //         AgvControllerOd.Values.Aggregate((a1, a2) =>
        //             EstimatePickDistance(a1, transport) < EstimatePickDistance(a2, transport) ? a1 : a2);
        //     selected.Schedule.AddLast(transport);
        //     selected.TryPushSchedule();
        //     return selected;
        // }
        //
        // private static float EstimatePickDistance(AgvController agvController, Transport newTransport)
        // {
        //     // Calculate the direct distance from AGV's last transport put position to new transport's pick position
        //     return (agvController.Schedule.Last.Value.Put.transform.position - newTransport.Pick.transform.position)
        //         .magnitude;
        // }

        public bool CheckTransportAvailable(IExchangeable transporter, Transport transport)
        {
            var pickE = GameObjectExchangeableDict[transport.Pick];
            var putE = GameObjectExchangeableDict[transport.Put];
            var item = pickE.GetItem(transport.ItemId);
            return pickE.CheckGivable(transporter, item)==ExchangeMessage.Ok &&
                   putE.CheckReceivable(transporter, item)==ExchangeMessage.Ok;
        }

    }
}
