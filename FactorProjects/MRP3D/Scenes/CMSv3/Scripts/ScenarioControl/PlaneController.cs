using System;
using System.Collections.Generic;
using System.Linq;
using OD;
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
            foreach (var (id,actionSpace) in AgentTypeActionSpaceDict.Values)
            {
                MaxActionSpaceSize = Math.Max(actionSpace.Count, MaxActionSpaceSize);
            }
            Debug.LogFormat("Max action space size is: {0}",MaxActionSpaceSize);
            Debug.LogFormat("Goal sensor size should be: {0}",GetAgentTypeCount());
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
        }

        public int MaxStep = 30000;
        [SerializeField]
        [InspectorUtil.DisplayOnly]
        private int currentStep = 0;

        private void FixedUpdate()
        {
            currentStep++;
            if (currentStep > MaxStep)
            {
                currentStep = 0;
                ResetPlane();
            }
        }

        private void ResetPlane()
        {
            AgentGroup.EndGroupEpisode();
            //TODO: Reset Layout
            foreach (var c in WorkstationControllerDict.Values)
            {
                c.EpisodeReset();
            }
            foreach (var c in AgvControllerDict.Values)
            {
                c.EpisodeReset();
            }
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
        private static Dictionary<string, (int,List<object>)> AgentTypeActionSpaceDict = new Dictionary<string, (int,List<object>)>();
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
        public static int GetAgentTypeCount()
        {
            return AgentTypeActionSpaceDict.Count;
        }
        


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
        public float minDeadline { get; } = 60f;
        public float maxDeadline { get; } = 120f;

        private void GenerateOneRandomOrder()
        {
            Order o = new Order(
                ScenarioLoader.ProductItemStates[Random.Range(0, ScenarioLoader.ProductItemStates.Count)].id, 
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
            AgentGroup.AddGroupReward(.1f);
            ground.FlipColor(Ground.GroundSwitchColor.Green);
        }

        #endregion
        
        
    }
}
