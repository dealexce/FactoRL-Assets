using System;
using System.Collections;
using System.Collections.Generic;
using OD;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{

    public class AgvController : MonoBehaviour, IExchangeable, IResetable, ILinkedToPlane, IHasStatus<AGVStatus>
    {
        public PlaneController planeController { get; set; }
        public Agv agv;
        public OrderedDictionary<string,List<Item>> HoldingItems;

        public AGVDispatcherAgent agvDispatcherAgent;
        public AGVMoveAgent agvMoveAgent;
        
        private Rigidbody _rigidbody;
        private void Awake()
        {
            _rigidbody = GetComponentInParent<Rigidbody>();
            agvDispatcherAgent = GetComponentInChildren<AGVDispatcherAgent>();
            agvMoveAgent = GetComponentInChildren<AGVMoveAgent>();
        }

        private void Start()
        {
            InitHoldingItems();
            moveSpeed = agv.movespeed;
            rotateSpeed = agv.rotatespeed;
        }

        private void InitHoldingItems()
        {
            HoldingItems = new OrderedDictionary<string,List<Item>>();
            foreach (var iId in SceanrioLoader.ItemStateDict.Keys)
            {
                HoldingItems.Add(iId,new List<Item>());
            }
        }

        public AGVStatus GetStatus()
        {
            return new AGVStatus(_rigidbody, HoldingItems, CurrentTarget);
        }

        #region ItemHolderImplement
        public Item GetItem(string id)
        {
            if (!HoldingItems.ContainsKey(id) 
                || HoldingItems[id].Count <= 0)
            {
                return null;
            }
            return HoldingItems[id][0];
        }

        public bool Store(Item item)
        {
            if (!HoldingItems.ContainsKey(item.itemState.id))
                return false;
            HoldingItems[item.itemState.id].Add(item);
            PlaceItems();
            return true;
        }

        public bool Remove(Item item)
        {
            if (!HoldingItems.ContainsKey(item.itemState.id))
                return false;
            
            if (!HoldingItems[item.itemState.id].Remove(item))
                return false;
            
            PlaceItems();
            return true;
        }

        public ExchangeMessage CheckReceivable(IExchangeable giver, Item item)
        {
            if (item==null
                || !HoldingItems.ContainsKey(item.itemState.id))
            {
                return ExchangeMessage.WrongType;
            }
            if(agv.capacitySpecified
               && ItemOdUtils.ListsSumCount(HoldingItems.Values)>=agv.capacity)
            {
                return ExchangeMessage.Overload;
            }
            return ExchangeMessage.Ok;
        }

        public ExchangeMessage CheckGivable(IExchangeable receiver, Item item)
        {
            if (item != null 
                && HoldingItems.ContainsKey(item.itemState.id)
                && HoldingItems[item.itemState.id].Contains(item))
            {
                return ExchangeMessage.Ok;
            }
            return ExchangeMessage.NullItem;
        }
        #endregion
        
        public float itemInterval = 1f;
        /// <summary>
        /// TODO: Check whether should use local position
        /// </summary>
        private void PlaceItems()
        {
            int i = 1;
            foreach (var list in HoldingItems.Values)
            {
                foreach (var item in list)
                {
                    item.gameObject.transform.localPosition = Vector3.up * itemInterval * i;
                    i++;
                }
            }
        }

        public Target CurrentTarget = null;
        
        public bool fixDecision = true;

        //Move settings
        public float moveSpeed = 5;
        public float rotateSpeed = 3;
        
        public Vector2 PolarVelocity { get; private set; }
        public Vector2 PolarTargetPos { get; private set; }


        private void FixedUpdate()
        {
            UpdatePolarPosAndPolarVelocity();
        }

        private void UpdatePolarPosAndPolarVelocity()
        {

            PolarVelocity = new Vector2(
                _rigidbody.velocity.magnitude / moveSpeed,
                _rigidbody.angularVelocity.y / rotateSpeed);

            if (CurrentTarget.GameObject != null)
            {
                PolarTargetPos = Utils.NormalizedPolarRelativePosition(
                    transform,
                    CurrentTarget.GameObject.transform,
                    planeController.normDistanceMaxValue);
            }
        }

        private void Update()
        {

            //FOR TEST: 每次按R会将目标切换至下一个，顺序一定
            // if (Input.GetKeyDown(KeyCode.R))
            // {
            //     agvDispatcherAgent.RequestTargetDecision();
            // }
        }


        public void EpisodeReset()
        {
            transform.rotation = Quaternion.identity;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            ResetHolder();
            agvMoveAgent.EpisodeInterrupted();
            //agvDispatcherAgent.RequestTargetDecision();
        }
        public void ResetHolder()
        {
            ItemOdUtils.DestroyAndClearLists(HoldingItems.Values,Destroy);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            GameObject otherGameObject = other.gameObject;
            if (otherGameObject == null)
            {
                return;
            }
            //到达当前设定的target
            if (otherGameObject == CurrentTarget.GameObject)
            {
                
                ArriveTarget();
                agvDispatcherAgent.RequestTargetDecision();
            }
        }
        private void OnCollisionEnter(Collision other)
        {
            agvMoveAgent.collideTrain();
        }

        private void OnCollisionStay(Collision other)
        {
            agvMoveAgent.collideTrain();
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
            if (CurrentTarget == null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                return;
            }
            Vector3 movement = transform.forward * Mathf.Clamp(forward, -1f, 1f);
            Vector3 rotation = transform.up * Mathf.Clamp(rotate, -1f, 1f);
            _rigidbody.velocity = movement * moveSpeed;
            _rigidbody.angularVelocity = rotation * rotateSpeed;
        }
        public float holdActionDuration = 1f;
        IEnumerator Hold()
        {
            CurrentTarget = null;
            yield return new WaitForSeconds(holdActionDuration);
            Done();
        }
        private void OnDrawGizmosSelected()
        {
            if (CurrentTarget.GameObject != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position,CurrentTarget.GameObject.transform.position);
            }
        }
        
        private void ArriveTarget()
        {
            agvMoveAgent.ArriveTarget();
            ExchangeMessage exchangeMessage = ExchangeMessage.Fail;
            if (CurrentTarget.TargetAction == TargetAction.Give)
            {
                //把holdingItem给targetItemHolder
                exchangeMessage = ItemControlHost.PassItem(
                    this, 
                    planeController.GameObjectExchangeableDict[CurrentTarget.GameObject], 
                    CurrentTarget.ItemStateId);
            }
            else if(CurrentTarget.TargetAction == TargetAction.Get)
            {
                //从targetItemHolder拿一个targetItemType类型的Item
                exchangeMessage = ItemControlHost.PassItem(
                    planeController.GameObjectExchangeableDict[CurrentTarget.GameObject], 
                    this, 
                    CurrentTarget.ItemStateId);
            }
            //交换成功，重置target
            if (exchangeMessage == ExchangeMessage.Ok)
            {
                Done();
            }
            else
            {
                Debug.LogWarning("Arrived at target but failed to exchange");
            }
        }

        private void Done()
        {
            CurrentTarget = null;
            agvDispatcherAgent.RequestTargetDecision();
        }

    }
}
