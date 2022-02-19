using System;
using System.Collections.Generic;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public struct Target
    {
        public GameObject gameObject;
        public string itemType;

        public Target(GameObject gameObject, string itemType)
        {
            this.gameObject = gameObject;
            this.itemType = itemType;
        }
    }
    public class AGVController : ItemHolder, Resetable
    {
        public PlaneController planeController;
        public AGVMoveAgent agvMoveAgent;
        public bool activateAward = false;
        public AGVDispatcherAgent agvDispatcherAgent;

        public Target target;
        
        public float moveSpeed = 5;
        public float maxSpeed = 8;
        public float rotateSpeed = 3;
        
        private Rigidbody _rigidbody;
        private BoxCollider _collider;
        public Vector2 polarVelocity { get; private set; }
        public Vector2 polarTargetPos { get; private set; }
        
        public Dictionary<GameObject,ItemHolder> AvailableTargetsItemHolderDict { get; private set; }
        public List<GameObject> availableTargetsObj_forTest;
        private int cur = 0;
        #region ItemHolderImplement
        public Item holdingItem;
        public override Item GetItem(string itemType)
        {
            return holdingItem;
        }

        protected override bool Remove(Item item)
        {
            if (item == holdingItem)
            {
                item = null;
                return true;
            }
            return true;
        }

        protected override bool Store(Item item)
        {
            if (holdingItem == null)
            {
                holdingItem = item;
                item.transform.parent = transform;
                item.transform.localPosition = new Vector3(0f,GetComponent<BoxCollider>().size.y,0f);
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
            target = agvDispatcherAgent.RequestNewTarget();
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
            if (otherGameObject == target.gameObject)
            {
                agvMoveAgent.arriveTargetTrain();
                target = agvDispatcherAgent.RequestNewTarget();
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

        private void Awake()
        {
            _rigidbody = GetComponentInParent<Rigidbody>();
            _collider = GetComponent<BoxCollider>();
            agvDispatcherAgent = GetComponentInChildren<AGVDispatcherAgent>();
            agvMoveAgent = GetComponentInChildren<AGVMoveAgent>();
        }

        private void Start()
        {
            planeController = GetComponentInParent<PlaneController>();
            AvailableTargetsItemHolderDict = planeController.AvailableTargets;
            availableTargetsObj_forTest = new List<GameObject>(AvailableTargetsItemHolderDict.Keys);
            target = agvDispatcherAgent.RequestNewTarget();
        }

        private void Update()
        {
            Vector3 position = transform.position;
            polarVelocity = new Vector2(_rigidbody.velocity.magnitude/moveSpeed, _rigidbody.angularVelocity.y/rotateSpeed);

            Vector3 targetPos = Vector3.zero;
            if (target.gameObject != null)
            {
                targetPos = (target.gameObject.transform.position - position) / planeController.maxDiameter;
                Vector3 cross = Vector3.Cross(targetPos, transform.forward);
                float angle = Vector3.Angle(targetPos, transform.forward) / 180f;
                polarTargetPos = new Vector2(cross.y > 0 ? -angle : angle, targetPos.magnitude);
            }
            
            //FOR TEST: 每次按R会将目标切换至下一个，顺序一定
            if (Input.GetKeyDown(KeyCode.R))
            {
                target = new Target(availableTargetsObj_forTest[cur++%availableTargetsObj_forTest.Count],"A0");
            }
        }

        public void Move(float forward, float rotate)
        {
            Vector3 movement = transform.forward * Mathf.Clamp(forward, -1f, 1f);
            Vector3 rotation = transform.up * Mathf.Clamp(rotate, -1f, 1f);
            _rigidbody.velocity = movement * moveSpeed;
            _rigidbody.angularVelocity = rotation * rotateSpeed;
        }
        
        public void AssignNewTarget(GameObject gameObject, string itemType)
        {
            target = new Target(gameObject,itemType);
        }
        private void OnDrawGizmosSelected()
        {
            if (target.gameObject != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position,target.gameObject.transform.position);
            }
        }
        
        private void arriveTarget()
        {
            ExchangeMessage exchangeMessage;
            if (target.itemType == null)
            {
                //把holdingItem给targetItemHolder
                exchangeMessage = ItemController.PassItem(this, AvailableTargetsItemHolderDict[target.gameObject], holdingItem);
            }
            else
            {
                //从targetItemHolder拿一个targetItemType类型的Item
                exchangeMessage = ItemController.PassItem(AvailableTargetsItemHolderDict[target.gameObject], this, target.itemType);
            }
            //交换成功，重置target
            if (exchangeMessage == ExchangeMessage.OK)
            {
                target = new Target(null, null);
            }
        }

    }
}
