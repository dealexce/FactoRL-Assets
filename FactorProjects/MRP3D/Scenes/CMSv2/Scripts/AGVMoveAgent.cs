using Multi;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class AGVMoveAgent : Agent, Resetable
    {
        public AGVController agvController;
        public PlaneController planeController;
        public string targetItemType;
        public ItemHolder targetItemHolder;

        private Rigidbody _rigidbody;
        private BoxCollider _collider;

        public float moveSpeed = 5;
        public float maxSpeed = 8;
        public float rotateSpeed = 3;

        public Vector2 polarVelocity { get; private set; }
        public Vector2 polarTargetPos { get; private set; }


        public GameObject target { get; set; }

        private TravelStatus _travelStatus = new TravelStatus();
        private void OnTriggerEnter(Collider other)
        {
            if (targetItemHolder == null)
            {
                return;
            }
            if (other.gameObject == targetItemHolder.gameObject)
            {
                ExchangeMessage exchangeMessage;
                if (targetItemType == null)
                {
                    //把holdingItem给targetItemHolder
                    exchangeMessage = ItemController.PassItem(agvController, targetItemHolder, agvController.holdingItem);
                }
                else
                {
                    //从targetItemHolder拿一个targetItemType类型的Item
                    exchangeMessage = ItemController.PassItem(targetItemHolder, agvController, targetItemType);
                }
                //交换成功，重置target
                if (exchangeMessage == ExchangeMessage.OK)
                {
                    targetItemType = null;
                    targetItemHolder = null;
                }
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            agvController = GetComponent<AGVController>();
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<BoxCollider>();
        }

        private void Update()
        {
            Vector3 position = transform.position;
            // sensor.AddObservation((position.x - _plane.transform.position.x) / scale);
            // sensor.AddObservation((position.z - _plane.transform.position.z) / scale);
            //
            // sensor.AddObservation(transform.forward.x);
            // sensor.AddObservation(transform.forward.z);

            polarVelocity = new Vector2(_rigidbody.velocity.magnitude/moveSpeed, _rigidbody.angularVelocity.y/rotateSpeed);

            Vector3 targetPos = Vector3.zero;
            if (target != null)
            {
                targetPos = (target.transform.position - position) / planeController.curX;
                Vector3 cross = Vector3.Cross(targetPos, transform.forward);
                float angle = Vector3.Angle(targetPos, transform.forward) / 180f;
                polarTargetPos = new Vector2(cross.y > 0 ? -angle : angle, targetPos.magnitude);
            }
            // if (!trainingMode)
            // {
            //     Debug.Log(GetCumulativeReward());
            // }
        }

        private void OnDrawGizmosSelected()
        {
            if (targetItemHolder != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position,targetItemHolder.gameObject.transform.position);
            }
        }

        /// <summary>
        /// This override method handles the action decisions made by neural network
        /// or gameplay actions and use the actions to operate in the game
        /// action.DiscreteActions[i] is 1 or -1
        /// Index   Meaning (1, -1)
        /// 0       move forthright (forward, backward)
        /// 1       rotate (right, left)
        /// </summary>
        /// <param name="actions"></param>
        public override void OnActionReceived(ActionBuffers actions)
        {
            //AddReward(-1f/_planeController.MaxEnvSteps);
            ActionSegment<float> act = actions.ContinuousActions;
            Vector3 movement = transform.forward * Mathf.Clamp(act[0], -1f, 1f);
            Vector3 rotation = transform.up * Mathf.Clamp(act[1], -1f, 1f);
            // _rigidbody.AddForce(movement * moveSpeed * (1-_rigidbody.velocity.magnitude/maxSpeed), ForceMode.VelocityChange);
            // transform.Rotate(rotation*rotateSpeed);

            _rigidbody.velocity = movement * moveSpeed;
            _rigidbody.angularVelocity = rotation * rotateSpeed;
        }

        /// <summary>
        /// Total Observation:
        /// 2 (polar velocity x,z)
        /// 2 (target relative position x,z)
        /// = 4 continuous
        /// </summary>
        /// <param name="sensor"></param>
        public override void CollectObservations(VectorSensor sensor)
        {
            // sensor.AddObservation((position.x - _plane.transform.position.x) / scale);
            // sensor.AddObservation((position.z - _plane.transform.position.z) / scale);
            //
            // sensor.AddObservation(transform.forward.x);
            // sensor.AddObservation(transform.forward.z);
            
            sensor.AddObservation(polarVelocity);
            sensor.AddObservation(polarTargetPos);

        }
        
        //TODO:
        public void EpisodeReset()
        {
            transform.rotation = Quaternion.identity;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
        

    }
}
