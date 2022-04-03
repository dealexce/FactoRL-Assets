﻿using System;
using System.Collections;
using System.Collections.Generic;
using OD;
using UnityEngine;
using UnityEngine.Assertions;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{

    public class AgvController : AgvControllerBase, IExchangeable, IResetable, ILinkedToPlane, IHasStatus<AgvStatus>, IManualInit<Agv>
    {
        public PlaneController PlaneController { get; set; }
        public OrderedDictionary<string,List<Item>> HoldingItemsDict;

        [HideInInspector]
        public AgvDispatcherAgent agvDispatcherAgent;
        [HideInInspector]
        public AGVMoveAgent agvMoveAgent;
        
        private Rigidbody _rigidbody;
        private void Awake()
        {
            _rigidbody = GetComponentInParent<Rigidbody>();
            agvDispatcherAgent = GetComponentInChildren<AgvDispatcherAgent>();
            agvMoveAgent = GetComponentInChildren<AGVMoveAgent>();
        }

        public override void Init(Agv model)
        {
            base.Init(model);
            InitHoldingItems();
            moveSpeed = model.movespeed;
            rotateSpeed = model.rotatespeed;
        }

        private void InitHoldingItems()
        {
            HoldingItemsDict = new OrderedDictionary<string,List<Item>>();
            foreach (var iId in SceanrioLoader.ItemStateDict.Keys)
            {
                HoldingItemsDict.Add(iId,new List<Item>());
            }
        }

        public AgvStatus GetStatus()
        {
            return new AgvStatus(_rigidbody, HoldingItemsDict, CurrentTarget);
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
            PlaceItems();
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
        
        public float itemInterval = 1f;
        /// <summary>
        /// TODO: Check whether should use local position
        /// </summary>
        private void PlaceItems()
        {
            int i = 1;
            foreach (var list in HoldingItemsDict.Values)
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
            //FOR TEST: 每次按R会将目标切换至下一个，顺序一定
            if (Input.GetKeyDown(KeyCode.R))
            {
                agvDispatcherAgent.RequestTargetDecision();
            }
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
            ItemOdUtils.DestroyAndClearLists(HoldingItemsDict.Values,Destroy);
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
                        CurrentTarget.ItemStateId),
                TargetAction.Get =>
                    //从targetItemHolder拿一个targetItemType类型的Item
                    ItemControlHost.PassItem(
                        PlaneController.GameObjectExchangeableDict[CurrentTarget.GameObject], 
                        this,
                        CurrentTarget.ItemStateId),
                _ => ExchangeMessage.Fail
            };
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