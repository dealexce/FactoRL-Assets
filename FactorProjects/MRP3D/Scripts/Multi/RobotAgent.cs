using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Multi
{
    public class RobotAgent : Agent
    {
        private Rigidbody _rigidbody;
        public GameObject _plane;

        private float scale = 30f;

        private PlaneController _planeController;
        private RobotHolder _robotHolder;

        private GameObject rawStack;
        private GameObject exportPlate;

        private List<GameObject> _workstationList;
        private Dictionary<GameObject, WorkStationController> _workstationControllerDict;
        private int workstationNum;
        
        public bool trainingMode = true;
        public float moveSpeed = 1;
        public float maxSpeed = 1;
        public float rotateSpeed = 1;
        private Vector3 initPosition;

        // Start is called before the first frame update
        void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _robotHolder = GetComponent<RobotHolder>();
            _planeController = _plane.GetComponent<PlaneController>();

            rawStack = _planeController.rawStack;
            exportPlate = _planeController.exportPlate;
            
            _workstationList = _planeController._workstationList;
            _workstationControllerDict = _planeController._workstationControllerDict;
            workstationNum = _workstationList.Count;

            initPosition = transform.position;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
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
            ActionSegment<float> act = actions.ContinuousActions;
            Vector3 movement = transform.forward * act[0];
            Vector3 rotation = transform.up * act[1];
            _rigidbody.AddForce(movement * moveSpeed * (1-_rigidbody.velocity.magnitude/maxSpeed), ForceMode.VelocityChange);
            transform.Rotate(rotation*rotateSpeed);
        }
        /// <summary>
        /// Total Observation:
        /// 2 (position x,z)
        /// 2 (rotation x,z)
        /// 1 (whether holding item)
        /// 9*2*5 (5(workstation one-hot)+1(input/output)+2(relative position x,z)+1(buffer count/capacity))*2 buffer*5 workstation
        /// 2 (raw stack relative position x,z)
        /// 2 (export plate relative position x,z)
        /// = 99 continuous
        /// </summary>
        /// <param name="sensor"></param>
        public override void CollectObservations(VectorSensor sensor)
        {
            Vector3 position = transform.position;
            sensor.AddObservation((position.x - _plane.transform.position.x) / scale);
            sensor.AddObservation((position.z - _plane.transform.position.z) / scale);

            sensor.AddObservation(transform.forward.x);
            sensor.AddObservation(transform.forward.z);
            
            sensor.AddObservation(_robotHolder.GetItem() != null ? 1.0f : 0.0f);
            
            int tempCount = 0;
            foreach (var workstation in _workstationList)
            {
                float[] inputObservation = new float[workstationNum + 4];
                inputObservation[tempCount] = 1.0f;
                inputObservation[workstationNum] = 0.0f;
                Vector3 inputPos = (position - workstation.transform.Find("InputPlate").position)/scale;
                inputObservation[workstationNum + 1] = inputPos.x;
                inputObservation[workstationNum + 2] = inputPos.y;
                inputObservation[workstationNum + 3] = _workstationControllerDict[workstation].getInputCapacityRatio();
                sensor.AddObservation(inputObservation);
                
                float[] outputObservation = new float[workstationNum + 4];
                outputObservation[tempCount] = 1.0f;
                outputObservation[workstationNum] = 1.0f;
                Vector3 outputPos = (position - workstation.transform.Find("OutputPlate").position)/scale;
                outputObservation[workstationNum + 1] = outputPos.x;
                outputObservation[workstationNum + 2] = outputPos.y;
                outputObservation[workstationNum + 3] = _workstationControllerDict[workstation].getOutputCapacityRatio();
                sensor.AddObservation(outputObservation);
            }

            Vector3 rawStackPos = (position - rawStack.transform.position) / scale;
            sensor.AddObservation(new Vector2(rawStackPos.x,rawStackPos.z));
            Vector3 exportPlatePos = (position - exportPlate.transform.position) / scale;
            sensor.AddObservation(new Vector2(exportPlatePos.x,exportPlatePos.z));
            
        }
        
        public override void OnEpisodeBegin()
        {
            ResetRobot();
            _planeController.ResetPlane();
        }

        private void ResetRobot()
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _robotHolder.ResetHolder();
        }
        
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var continuousAction = actionsOut.ContinuousActions;
            continuousAction[0] = Input.GetAxis("Vertical");
            continuousAction[1] = Input.GetAxis("Horizontal");
        }
        
        private void OnCollisionEnter(Collision other)
        {
            if (other.collider.CompareTag("wall")||other.collider.CompareTag("machine"))
            {
                AddReward(-1f);
            }
        }
    }
}
