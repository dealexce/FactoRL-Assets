using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Multi
{
    public class RobotDispatcherAgent : Agent
    {
        BufferSensorComponent _bufferSensor;
        VectorSensorComponent _goalSensor;
        private Rigidbody _rigidbody;
        private RobotHolder _robotHolder;
        private RobotMoveAgent _robotMoveAgent;
        private GameObject _plane;
        private PlaneController _planeController;
        
        private List<GameObject> _agentList = new List<GameObject>();
        private Dictionary<GameObject, ResetableAgent> _agentDict = new Dictionary<GameObject, ResetableAgent>();

        private List<GameObject> _workstationList = new List<GameObject>();
        private Dictionary<GameObject, WorkStationController> _workstationControllerDict =
            new Dictionary<GameObject, WorkStationController>();

        private GameObject rawStack;
        private GameObject exportPlate;

        private float scale;

        public GameObject _robot;
        private List<GameObject> possibleTargets;

        private int testAllocationCount = 0;

        public struct RobotStatus
        {
            private ItemType holdingItemType;
            private Transform transform;
        }

        private void Awake()
        {
            _bufferSensor = GetComponent<BufferSensorComponent>();
        }

        private void Start()
        {
            
            _goalSensor = GetComponent<VectorSensorComponent>();
            _rigidbody = _robot.GetComponent<Rigidbody>();
            _robotHolder = _robot.GetComponent<RobotHolder>();
            _robotMoveAgent = _robot.GetComponentInParent<RobotMoveAgent>();
            
            _plane = _robotMoveAgent._plane;
            _planeController = _plane.GetComponent<PlaneController>();
            _agentList = _planeController._agentList;
            _agentDict = _planeController._agentDict;
            _workstationList = _planeController._workstationList;
            _workstationControllerDict = _planeController._workstationControllerDict;
            rawStack = _planeController.rawStack;
            exportPlate = _planeController.exportPlate;
            possibleTargets = _planeController.possibleTargets;

            scale = _robotMoveAgent.scale;
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            var action = actions.DiscreteActions[0];
            _robotMoveAgent.target = possibleTargets[action];
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var act = actionsOut.DiscreteActions;
            act[0] = testAllocationCount++;
            if (testAllocationCount >= possibleTargets.Count)
            {
                testAllocationCount = 0;
            }
            Debug.Log(act[0]);
        }

        private Vector2 rect2polar(Vector3 rect)
        {
            Vector3 inputCross = Vector3.Cross(rect, transform.forward);
            float inputAngle = Vector3.Angle(rect, transform.forward) / 180f;
            Vector2 polar = new Vector2(inputCross.y > 0 ? -inputAngle : inputAngle, rect.magnitude);
            return polar;
        }

        /// <summary>
        /// 20 = 5*2*2 : 5 workstations (5) * 2 triggers (2) * polar relative position (2)
        /// 2 : polar relative position to raw stack
        /// 2 : polar relative position to export plate
        /// 2 : self polar velocity
        /// ----------
        /// = 26
        /// </summary>
        /// <param name="sensor"></param>
        public override void CollectObservations(VectorSensor sensor)
        {
            Vector3 position = transform.position;
            foreach (var workstationController in _workstationControllerDict.Values)
            {
                GameObject input = workstationController.inputPlate;
                Vector3 inputPos = (input.transform.position - position) / scale;
                sensor.AddObservation(rect2polar(inputPos));
                
                GameObject output = workstationController.outputPlate;
                Vector3 outputPos = (output.transform.position - position) / scale;
                sensor.AddObservation(rect2polar(outputPos));
            }
            
            Vector3 rawStackPos = (rawStack.transform.position - position) / scale;
            sensor.AddObservation(rect2polar(rawStackPos));

            Vector3 exportPos = (exportPlate.transform.position - position) / scale;
            sensor.AddObservation(rect2polar(exportPos));
            
            sensor.AddObservation(_robotMoveAgent.polarVelocity);
            
            
            // Buffer Sensor: variable observation for other agents:
            // one-hot holding item type(6+1), null for last digit
            // agent polarVelocity(2)
            // agent polar relative position(2)
            // = 11 for each agent
            int enumLength = Enum.GetNames(typeof(ItemType)).Length;
            foreach (var a in _agentDict.Values)
            {
                RobotMoveAgent agent = (RobotMoveAgent) a;
                if (agent == this._robotMoveAgent)
                {
                    continue;
                }
                float[] agentInfo = new float[enumLength + 5];
                Item item = agent._robotHolder.GetItem();
                if (item != null)
                {
                    agentInfo[(int)item.itemType] = 1.0f;
                }
                else
                {
                    agentInfo[enumLength] = 1.0f;
                }
                
                agentInfo[enumLength + 1] = agent.polarVelocity.x;
                agentInfo[enumLength + 2] = agent.polarVelocity.y;

                Vector3 targetPos = Vector3.zero;
                targetPos = (agent.transform.position - position) / scale;
                Vector3 cross = Vector3.Cross(targetPos, transform.forward);
                float angle = Vector3.Angle(targetPos, transform.forward) / 180f;
                agentInfo[enumLength + 3] = cross.y > 0 ? -angle : angle;
                agentInfo[enumLength + 4] = targetPos.magnitude;
                
                _bufferSensor.AppendObservation(agentInfo);
            }
            
            // Goal Signal: One-hot for item type holding
            // Size = 7
            float[] selfItemOneHot = new float[enumLength + 1];
            Item selfItem = _robotHolder.GetItem();
            if (selfItem != null)
            {
                selfItemOneHot[(int)selfItem.itemType] = 1.0f;
            }
            else
            {
                selfItemOneHot[enumLength] = 1.0f;
            }
            _goalSensor.GetSensor().AddObservation(selfItemOneHot);
        }

        public void RequestNewTarget()
        {
            RequestDecision();
        }
    }
}
