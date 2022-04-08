using System;
using System.Collections;
using System.Collections.Generic;
using OD;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.Assertions;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{

    public class AgvController : AgvControllerBase, IExchangeable, IResettable, ILinkedToPlane, IHaveStatus<AgvStatus>, IHaveAgent
    {
        public PlaneController PlaneController { get; set; }
        public OrderedDictionary<string,List<Item>> HoldingItemsDict;

        [SerializeField]
        private AgvDispatcherAgent agvDispatcherAgent;
        [SerializeField]
        private AGVMoveAgent agvMoveAgent;
        private Rigidbody _rigidbody;
        private void Awake()
        {
            _rigidbody = GetComponentInParent<Rigidbody>();
        }

        public override void Init(Agv model)
        {
            base.Init(model);
            InitHoldingItems();
            moveSpeed = model.movespeed;
            rotateSpeed = model.rotatespeed;
            PlaneController.RegisterAgent(agvDispatcherAgent, "AD",agvDispatcherAgent.InitActionSpace);
        }

        private void InitHoldingItems()
        {
            HoldingItemsDict = new OrderedDictionary<string,List<Item>>();
            foreach (var iId in ScenarioLoader.ItemStateDict.Keys)
            {
                HoldingItemsDict.Add(iId,new List<Item>());
            }
        }

        public AgvStatus GetStatus()
        {
            return new AgvStatus(_rigidbody, HoldingItemsDict, CurrentTarget,agvDispatcherAgent.ActionSpace);
        }

        public Agent GetAgent()
        {
            return agvDispatcherAgent;
        }

        #region ItemHolderImplement
        public Item GetItem(string id)
        {
            if (!HoldingItemsDict.ContainsKey(id) 
                || HoldingItemsDict[id].Count <= 0)
            {
                return null;
            }
            return HoldingItemsDict[id][0];
        }

        public bool Store(Item item)
        {
            if (!HoldingItemsDict.ContainsKey(item.itemState.id))
                return false;
            HoldingItemsDict[item.itemState.id].Add(item);
            return true;
        }

        public bool Remove(Item item)
        {
            if (!HoldingItemsDict.ContainsKey(item.itemState.id))
                return false;
            
            if (!HoldingItemsDict[item.itemState.id].Remove(item))
                return false;
            
            PlaceItems();
            return true;
        }

        public void OnReceived(ExchangeMessage exchangeMessage)
        {
            PlaceItems();
        }

        public ExchangeMessage CheckReceivable(IExchangeable giver, Item item)
        {
            if (item==null
                || !HoldingItemsDict.ContainsKey(item.itemState.id))
            {
                return ExchangeMessage.WrongType;
            }
            if(Agv.capacitySpecified
               && ItemOdUtils.ListsSumCount(HoldingItemsDict.Values)>=Agv.capacity)
            {
                return ExchangeMessage.Overload;
            }
            return ExchangeMessage.Ok;
        }

        public ExchangeMessage CheckGivable(IExchangeable receiver, Item item)
        {
            if (item != null 
                && HoldingItemsDict.ContainsKey(item.itemState.id)
                && HoldingItemsDict[item.itemState.id].Contains(item))
            {
                return ExchangeMessage.Ok;
            }
            return ExchangeMessage.NullItem;
        }
        #endregion
        
        public float itemInterval = .2f;
        private void PlaceItems()
        {
            int i = 1;
            foreach (var list in HoldingItemsDict.Values)
            {
                foreach (var item in list)
                {
                    var transform1 = item.transform;
                    transform1.rotation=Quaternion.identity;
                    transform1.position = transform.position+Vector3.up * itemInterval * i;
                    i++;
                }
            }
        }

        public Target CurrentTarget = null;
        
        public bool fixDecision = true;
        public int autoDecisionInterval = 100;

        //Move settings
        public float moveSpeed = 5;
        public float rotateSpeed = 3;
        
        public Vector2 PolarVelocity { get; private set; }
        public Vector2 PolarTargetPos { get; private set; }


        private int lastDecisionStep = 0;
        private void FixedUpdate()
        {
            UpdatePolarPosAndPolarVelocity();
            lastDecisionStep++;
            if (!fixDecision&&lastDecisionStep > autoDecisionInterval)
            {
                RequestDispatchDecision();
            }
        }

        private void RequestDispatchDecision()
        {
            agvDispatcherAgent.RequestTargetDecision();
            lastDecisionStep = 0;
        }

        private void UpdatePolarPosAndPolarVelocity()
        {

            PolarVelocity = new Vector2(
                _rigidbody.velocity.magnitude / moveSpeed,
                _rigidbody.angularVelocity.y / rotateSpeed);

            if (CurrentTarget != null)
            {
                PolarTargetPos = Utils.NormalizedPolarRelativePosition(
                    transform,
                    CurrentTarget.GameObject.transform,
                    NormValues.DistanceMaxValue);
            }
        }

        private void Update()
        {
            // //FOR TEST: 每次按R会将目标切换至下一个，顺序一定
            // if (Input.GetKeyDown(KeyCode.R))
            // {
            //     RequestDispatchDecision();
            // }
        }

        public Vector3 InitPosition { get; set; }

        public void EpisodeReset()
        {
            transform.position = InitPosition;
            transform.rotation = Quaternion.identity;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            ResetHolder();
            agvMoveAgent.EpisodeInterrupted();
            Done();
        }
        public void ResetHolder()
        {
            ItemOdUtils.DestroyAndClearLists(HoldingItemsDict.Values,Destroy);
        }

        private void Start()
        {
            RequestDispatchDecision();
        }

        private void OnTriggerEnter(Collider other)
        {
            GameObject otherGameObject = other.gameObject;
            if (otherGameObject == null)
            {
                return;
            }
            //到达当前设定的target
            if (otherGameObject == CurrentTarget?.GameObject)
            {
                ArriveTarget();
            }
        }
        private void OnCollisionEnter(Collision other)
        {
            agvMoveAgent.CollideTrain();
        }

        private void OnCollisionStay(Collision other)
        {
            agvMoveAgent.CollideTrain();
        }
        
        public void AssignNewTarget(Target newTarget)
        {
            CurrentTarget = newTarget;
            if (CurrentTarget == null)
            {
                StartCoroutine(nameof(Hold));
            }
        }

        public void Move(float forward, float rotate)
        {
            Vector3 movement = transform.forward * Mathf.Clamp(forward, -1f, 1f);
            Vector3 rotation = transform.up * Mathf.Clamp(rotate, -1f, 1f);
            _rigidbody.velocity = movement * moveSpeed;
            _rigidbody.angularVelocity = rotation * rotateSpeed;
        }
        public float holdActionDuration = 1f;
        IEnumerator Hold()
        {
            CurrentTarget = null;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            yield return new WaitForSeconds(holdActionDuration);
            Done();
        }
        private void OnDrawGizmosSelected()
        {
            if (CurrentTarget != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position,CurrentTarget.GameObject.transform.position);
            }
        }
        
        private void ArriveTarget()
        {
            Assert.IsNotNull(CurrentTarget);
            Assert.AreNotEqual(CurrentTarget.TargetAction,TargetAction.Hold);
            agvMoveAgent.ArriveTarget();
            ExchangeMessage exchangeMessage = CurrentTarget.TargetAction switch
            {
                TargetAction.Give =>
                    //把holdingItem给targetItemHolder
                    ItemControlHost.PassItem(
                        this, 
                        PlaneController.GameObjectExchangeableDict[CurrentTarget.GameObject], 
                        CurrentTarget.ItemStateId,
                        CurrentTarget.GameObject.transform),
                TargetAction.Get =>
                    //从targetItemHolder拿一个targetItemType类型的Item
                    ItemControlHost.PassItem(
                        PlaneController.GameObjectExchangeableDict[CurrentTarget.GameObject], 
                        this,
                        CurrentTarget.ItemStateId,
                        transform),
                _ => ExchangeMessage.Fail
            };
            //交换成功，重置target
            if (exchangeMessage != ExchangeMessage.Ok)
            {
                Debug.LogWarning("Arrived at target but failed to exchange");
            }
            Done();
        }

        private void Done()
        {
            CurrentTarget = null;
            RequestDispatchDecision();
        }

    }
}
