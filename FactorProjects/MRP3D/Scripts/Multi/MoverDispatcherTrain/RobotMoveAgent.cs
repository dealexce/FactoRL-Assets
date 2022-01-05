using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

namespace Multi
{
    public class RobotMoveAgent : ResetableAgent
    {
        private Rigidbody _rigidbody;
        private BoxCollider _collider;
        public GameObject _plane;
        public GameObject _dispatcher;

        public float scale = 30f;

        private PlaneController _planeController;
        private RobotDispatcherAgent _robotDispatcher;
        public RobotHolder _robotHolder { get; private set; }

        public bool trainingMode = true;
        public float moveSpeed = 5;
        public float maxSpeed = 8;
        public float rotateSpeed = 3;
        private float ypos = 0f;

        public float collisionReward = -.5f;
        public float collisionStayReward = -.005f;
        public float arrivalReward = 1.0f;

        public Vector2 polarVelocity { get; private set; }
        public Vector2 polarTargetPos { get; private set; }


        public GameObject target { get; set; }

        // Start is called before the first frame update
        void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<BoxCollider>();
            _robotHolder = GetComponent<RobotHolder>();
            _planeController = _plane.GetComponent<PlaneController>();
            _robotDispatcher = _dispatcher.GetComponent<RobotDispatcherAgent>();

            ypos = transform.localPosition.y;
            target = null;
            _robotDispatcher.RequestNewTarget();
            SafeResetRandomPosition();
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
                targetPos = (target.transform.position - position) / scale;
                Vector3 cross = Vector3.Cross(targetPos, transform.forward);
                float angle = Vector3.Angle(targetPos, transform.forward) / 180f;
                polarTargetPos = new Vector2(cross.y > 0 ? -angle : angle, targetPos.magnitude);
            }
            else
            {
                _robotDispatcher.RequestNewTarget();
            }
            // if (!trainingMode)
            // {
            //     Debug.Log(GetCumulativeReward());
            // }
        }

        private void OnDrawGizmosSelected()
        {
            if (target != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position,target.transform.position);
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
            
            if (!trainingMode)
            {
                Debug.Log(polarVelocity);
            }

        }
        

        override public void ResetRobot()
        {
            SafeResetRandomPosition();
            transform.rotation = Quaternion.identity;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _robotHolder.ResetHolder();
            target = null;
            _robotDispatcher.RequestNewTarget();
            if (!trainingMode)
            {
                Debug.Log(GetCumulativeReward());
            }
            EndEpisode();
        }

        Vector3 Skode_Vec3Mul(Vector3 value1, Vector3 value2)
        {
            return new Vector3(value1.x * value2.x, value1.y * value2.y, value1.z * value2.z);
        }

        private void SafeResetRandomPosition()
        {
            Vector3 scale = transform.localScale;
            Transform tempTrans = transform.parent;
            while (tempTrans != null)
            {
                scale = Skode_Vec3Mul(scale, tempTrans.localScale);
                tempTrans = tempTrans.parent;
            }

            int remainAttempts = 100;
            bool safePositionFound = false;
            var potentialPosition = Vector3.zero;
            while (!safePositionFound && remainAttempts>0)
            {
                potentialPosition = new Vector3(UnityEngine.Random.Range(-13f, 13f),0.15f, UnityEngine.Random.Range(-8f, 8f));
                potentialPosition = _plane.transform.position + potentialPosition;
                remainAttempts--;
                Collider[] colliders = Physics.OverlapBox(potentialPosition,scale/2);
                safePositionFound = colliders.Length == 0;
            }
            if (safePositionFound)
            {
                transform.position = potentialPosition;
            }
            else
            {
                Debug.LogError("Unable to find a safe position to reset work point");
            }
        }
        
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            if (trainingMode)
            {
                return;
            }
            var continuousAction = actionsOut.ContinuousActions;
            continuousAction[0] = Input.GetAxis("Vertical");
            continuousAction[1] = Input.GetAxis("Horizontal");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == target)
            {
                AddReward(arrivalReward);
                target = null;
                _robotDispatcher.RequestNewTarget();
                Debug.Log("Arrived at Target");
            }
            // else
            // {
            //     AddReward(wrongTriggerEnterReward);
            // }
        }

        private void OnCollisionEnter(Collision other)
        {
            AddReward(collisionReward);
        }

        private void OnCollisionStay(Collision other)
        {
            AddReward(collisionStayReward);
        }

    }
}
