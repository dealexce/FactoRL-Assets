using System;
using System.Collections.Generic;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{

    public class AgvController : MonoBehaviour, IExchangable, IResetable, ILinkedToPlane, IHasStatus<AGVStatus>
    {
        public PlaneController planeController { get; set; }
        public Agv agv;
        public List<Item> HoldingItems = new List<Item>();

        public AGVStatus GetStatus()
        {
            throw new NotImplementedException();
        }

        #region ItemHolderImplement
        public Item GetItem(string id)
        {
            foreach (var item in HoldingItems)
            {
                if (id.Equals(item.itemState.id))
                {
                    return item;
                }
            }
            return null;
        }

        public bool Store(Item item)
        {
            HoldingItems.Add(item);
            PlaceItems();
            return true;
        }

        public bool Remove(Item item)
        {
            if (!HoldingItems.Contains(item))
            {
                return false;
            }
            HoldingItems.Remove(item);
            PlaceItems();
            return true;
        }

        public ExchangeMessage CheckReceivable(IExchangable giver, Item item)
        {
            if (HoldingItems.Count<agv.capacity)
            {
                return ExchangeMessage.Ok;
            }
            return ExchangeMessage.Overload;
        }

        public ExchangeMessage CheckGivable(IExchangable receiver, Item item)
        {
            if (HoldingItems.Contains(item))
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
            for (int i = 0; i < HoldingItems.Count; i++)
            {
                HoldingItems[i].transform.position = transform.position
                                                     + Vector3.up * itemInterval * i;
            }
        }
        
        public AGVMoveAgent agvMoveAgent;
        //public AGVDispatcherAgent agvDispatcherAgent;
        
        private Rigidbody _rigidbody;
        
        public bool activateAward = false;

        public float noTargetHoldTime = 1f;
        private float noTargetTime = 0f;

        public Target target = PConsts.NullTarget;
        
        public bool fixDecision = true;

        //Move settings
        public float moveSpeed = 5;
        public float rotateSpeed = 3;
        
        public Vector2 polarVelocity { get; private set; }
        public Vector2 polarTargetPos { get; private set; }
        
        public Dictionary<GameObject,ItemHolder> TargetableGameObjectItemHolderDict { get; private set; }
        public List<GameObject> targetableGameObjects;
        private List<Target> _availableTargetCombinations;

        public int autoDispatcherRequestStep = 100;
        private int dispatcherAcademicStep = 0;

        // public AGVStatus GetStatus()
        // {
        //     string holdingItemType = holdingItem != null ? holdingItem.itemType : PConsts.NullItem;
        //     return new AGVStatus(
        //         _rigidbody,
        //         PlaneController.ItemTypeIndexDict[holdingItemType],
        //         PlaneController.TargetCombinationIndexDict[target]);
        // }

        #region MonoBehaviorInitialization

        private void Awake()
        {
            _rigidbody = GetComponentInParent<Rigidbody>();
            //agvDispatcherAgent = GetComponentInChildren<AGVDispatcherAgent>();
            agvMoveAgent = GetComponentInChildren<AGVMoveAgent>();
        }

        // private void Start()
        // {
        //     TargetableGameObjectItemHolderDict = PlaneController.GameObjectItemHolderDict;
        //     targetableGameObjects = new List<GameObject>(TargetableGameObjectItemHolderDict.Keys);
        //     _availableTargetCombinations = PlaneController.TargetCombinationList;
        //     agvDispatcherAgent.RequestTargetDecision();
        // }

        // private void FixedUpdate()
        // {            
        //     Vector3 position = transform.position;
        //     polarVelocity = new Vector2(_rigidbody.velocity.magnitude/moveSpeed, _rigidbody.angularVelocity.y/rotateSpeed);
        //
        //     Vector3 targetPos = Vector3.zero;
        //     if (target.GameObject != null)
        //     {
        //         targetPos = (target.GameObject.transform.position - position) / PlaneController.MAXDiameter;
        //         Vector3 cross = Vector3.Cross(targetPos, transform.forward);
        //         float angle = Vector3.Angle(targetPos, transform.forward) / 180f;
        //         polarTargetPos = new Vector2(cross.y > 0 ? -angle : angle, targetPos.magnitude);
        //     }
        //     
        //     //如果没有指派任何目标且距离上一次分配目标已经超过闲置时间
        //     if (target.GameObject == null)
        //     {
        //         if (noTargetTime > noTargetHoldTime)
        //         {
        //             agvDispatcherAgent.RequestTargetDecision();
        //         }
        //         else
        //         {
        //             noTargetTime += Time.deltaTime;
        //         }
        //     }
        //     if (fixDecision)
        //     {
        //         return;
        //     }
        //     dispatcherAcademicStep++;
        //     if (dispatcherAcademicStep > autoDispatcherRequestStep)
        //     {
        //         agvDispatcherAgent.RequestTargetDecision();
        //     }
        // }
        private void Update()
        {

            //FOR TEST: 每次按R会将目标切换至下一个，顺序一定
            // if (Input.GetKeyDown(KeyCode.R))
            // {
            //     agvDispatcherAgent.RequestTargetDecision();
            // }
        }

        #endregion
        
        
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
            foreach (var holdingItem in HoldingItems)
            {
                Destroy(holdingItem.gameObject);
            }
            HoldingItems.Clear();
        }
        
        // private void OnTriggerEnter(Collider other)
        // {
        //     GameObject otherGameObject = other.gameObject;
        //     if (otherGameObject == null)
        //     {
        //         return;
        //     }
        //     //到达当前设定的target
        //     if (otherGameObject == target.GameObject)
        //     {
        //         agvMoveAgent.arriveTargetTrain();
        //         arriveTarget();
        //         agvDispatcherAgent.RequestTargetDecision();
        //     }
        // }
        private void OnCollisionEnter(Collision other)
        {
            agvMoveAgent.collideTrain();
        }

        private void OnCollisionStay(Collision other)
        {
            agvMoveAgent.collideTrain();
        }
        
        public void AssignNewTarget(Target target)
        {
            noTargetTime = 0f;
            dispatcherAcademicStep = 0;
            target = target;
        }


        public void Move(float forward, float rotate)
        {
            if (target == PConsts.NullTarget)
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
        
        // public void AssignNewTarget(int targetIndex)
        // {
        //     noTargetTime = 0f;
        //     dispatcherAcademicStep = 0;
        //     target = PlaneController.TargetCombinationList[targetIndex];
        //     
        // }
        private void OnDrawGizmosSelected()
        {
            if (target.GameObject != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position,target.GameObject.transform.position);
            }
        }
        //
        // private void arriveTarget()
        // {
        //     ExchangeMessage exchangeMessage = ExchangeMessage.Fail;
        //     if (target.TargetAction == TargetAction.Give)
        //     {
        //         //把holdingItem给targetItemHolder
        //         exchangeMessage = ItemController.PassItem(this, TargetableGameObjectItemHolderDict[target.GameObject], holdingItem);
        //     }
        //     else if(target.TargetAction == TargetAction.Get)
        //     {
        //         //从targetItemHolder拿一个targetItemType类型的Item
        //         exchangeMessage = ItemController.PassItem(TargetableGameObjectItemHolderDict[target.GameObject], this, target.ItemType);
        //     }
        //     //交换成功，重置target
        //     if (exchangeMessage == ExchangeMessage.Ok)
        //     {
        //         target = PConsts.NullTarget;
        //     }
        // }

    }
}
