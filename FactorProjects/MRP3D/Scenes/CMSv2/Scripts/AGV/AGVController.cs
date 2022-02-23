using System;
using System.Collections.Generic;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{

    public class AGVController : ItemHolder, Resetable, LinkedToPlane, IHasStatus<AGVStatus>
    {
        [HideInInspector]
        public PlaneController _planeController { get; set; }
        [HideInInspector]
        public AGVMoveAgent agvMoveAgent;
        [HideInInspector]
        public AGVDispatcherAgent agvDispatcherAgent;
        
        private Rigidbody _rigidbody;
        
        public bool activateAward = false;

        public float noTargetHoldTime = 1f;
        private float noTargetTime = 0f;
        
        public Item holdingItem;
        
        public Target target;

        //Move settings
        public float moveSpeed = 5;
        public float rotateSpeed = 3;
        
        public Vector2 polarVelocity { get; private set; }
        public Vector2 polarTargetPos { get; private set; }
        
        public Dictionary<GameObject,ItemHolder> TargetableGameObjectItemHolderDict { get; private set; }
        public List<GameObject> targetableGameObjects;
        private List<Target> _availableTargetCombinations;
        private int cur = 0;

        public AGVStatus GetStatus()
        {
            return new AGVStatus(
                _rigidbody,
                _planeController.ItemTypeIndexDict[holdingItem.itemType],
                _planeController.TargetCombinationIndexDict[target]);
        }

        #region MonoBehaviorInitialization

        private void Awake()
        {
            _rigidbody = GetComponentInParent<Rigidbody>();
            agvDispatcherAgent = GetComponentInChildren<AGVDispatcherAgent>();
            agvMoveAgent = GetComponentInChildren<AGVMoveAgent>();
        }

        private void Start()
        {
            TargetableGameObjectItemHolderDict = _planeController.GameObjectItemHolderDict;
            targetableGameObjects = new List<GameObject>(TargetableGameObjectItemHolderDict.Keys);
            _availableTargetCombinations = _planeController.TargetCombinationList;
            agvDispatcherAgent.RequestTargetDecision();
        }

        #endregion
        
        #region ItemHolderImplement
        public override Item GetItem(string itemType)
        {
            return holdingItem;
        }

        protected override bool Remove(Item item)
        {
            if (item == holdingItem)
            {
                holdingItem = null;
                return true;
            }
            return true;
        }

        protected override bool Store(Item item)
        {
            if (holdingItem == null)
            {
                holdingItem = item;
                item.transform.SetParent(transform, true);
                item.transform.position = transform.position + new Vector3(0f,GetComponentInChildren<BoxCollider>().size.y,0f);
                return true;
            }
            return false;
        }

        public override ExchangeMessage CheckReceivable(ItemHolder giver, Item item)
        {
            if (holdingItem == null)
            {
                return ExchangeMessage.OK;
            }
            else
            {
                return ExchangeMessage.Overload;
            }
        }

        public override ExchangeMessage CheckGivable(ItemHolder receiver, Item item)
        {
            if (holdingItem != null)
            {
                return ExchangeMessage.OK;
            }
            else
            {
                return ExchangeMessage.NullItem;
            }
        }
        #endregion
        public void EpisodeReset()
        {
            transform.rotation = Quaternion.identity;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            ResetHolder();
            agvMoveAgent.EpisodeInterrupted();
            agvDispatcherAgent.RequestTargetDecision();
        }
        public void ResetHolder()
        {
            if (holdingItem != null)
            {
                Destroy(holdingItem.gameObject);
            }
            holdingItem = null;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            GameObject otherGameObject = other.gameObject;
            if (otherGameObject == null)
            {
                return;
            }
            //到达当前设定的target
            if (otherGameObject == target.GameObject)
            {
                agvMoveAgent.arriveTargetTrain();
                arriveTarget();
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



        private void Update()
        {
            Vector3 position = transform.position;
            polarVelocity = new Vector2(_rigidbody.velocity.magnitude/moveSpeed, _rigidbody.angularVelocity.y/rotateSpeed);

            Vector3 targetPos = Vector3.zero;
            if (target.GameObject != null)
            {
                targetPos = (target.GameObject.transform.position - position) / _planeController.MAXDiameter;
                Vector3 cross = Vector3.Cross(targetPos, transform.forward);
                float angle = Vector3.Angle(targetPos, transform.forward) / 180f;
                polarTargetPos = new Vector2(cross.y > 0 ? -angle : angle, targetPos.magnitude);
            }
            
            //如果没有指派任何目标且距离上一次分配目标已经超过闲置时间
            if (target.GameObject == null)
            {
                if (noTargetTime > noTargetHoldTime)
                {
                    agvDispatcherAgent.RequestTargetDecision();
                }
                else
                {
                    noTargetTime += Time.deltaTime;
                }
            }

            //FOR TEST: 每次按R会将目标切换至下一个，顺序一定
            // if (Input.GetKeyDown(KeyCode.R))
            // {
            //     agvDispatcherAgent.RequestTargetDecision();
            // }
        }

        public void Move(float forward, float rotate)
        {
            Vector3 movement = transform.forward * Mathf.Clamp(forward, -1f, 1f);
            Vector3 rotation = transform.up * Mathf.Clamp(rotate, -1f, 1f);
            _rigidbody.velocity = movement * moveSpeed;
            _rigidbody.angularVelocity = rotation * rotateSpeed;
        }
        
        public void AssignNewTarget(int targetIndex)
        {
            noTargetTime = 0f;
            target = _planeController.TargetCombinationList[targetIndex];
            
        }
        private void OnDrawGizmosSelected()
        {
            if (target.GameObject != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position,target.GameObject.transform.position);
            }
        }
        
        private void arriveTarget()
        {
            ExchangeMessage exchangeMessage = ExchangeMessage.Fail;
            if (target.TargetAction == TargetAction.Give)
            {
                //把holdingItem给targetItemHolder
                exchangeMessage = ItemController.PassItem(this, TargetableGameObjectItemHolderDict[target.GameObject], holdingItem);
            }
            else if(target.TargetAction == TargetAction.Get)
            {
                //从targetItemHolder拿一个targetItemType类型的Item
                exchangeMessage = ItemController.PassItem(TargetableGameObjectItemHolderDict[target.GameObject], this, target.ItemType);
            }
            //交换成功，重置target
            if (exchangeMessage == ExchangeMessage.OK)
            {
                target = ProductionConstants.NullTarget;
            }
        }

    }
}
