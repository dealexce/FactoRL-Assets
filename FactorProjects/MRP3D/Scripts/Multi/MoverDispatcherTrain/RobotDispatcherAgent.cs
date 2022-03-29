using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Multi
{
    public struct TravelStatus
    {
        public int CollisionCount;
        public int WrongTriggerCount;
    }
    public class RobotDispatcherAgent : Agent
    {
        BufferSensorComponent _bufferSensor;
        VectorSensorComponent _goalSensor;
        private Rigidbody _rigidbody;
        private RobotHolder _robotHolder;
        private RobotMoveAgent _robotMoveAgent;
        private GameObject _plane;
        private PlaneController _planeController;
        
        private Dictionary<GameObject, ResetableAgent> _robotMoveAgentDict = new Dictionary<GameObject, ResetableAgent>();

        private Dictionary<GameObject, WorkStationController> _workstationControllerDict =
            new Dictionary<GameObject, WorkStationController>();

        private GameObject rawStack;
        private GameObject exportPlate;

        private float scale;
        private int maxEnvSteps;

        public GameObject _robot;
        private List<GameObject> possibleTargets;

        private int testAllocationCount = 0;

        public float wrongTargetAllocatedReward = -0.1f;
        public float correctTargetAllocatedReward = 0.5f;

        public float travelCollisionRewardFactor = -.005f;
        public float travelWrongTriggerEnterRewardFactor = -.005f;
        public float existanceRewardFactor = -1f;

        public bool isTraining = true;



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
            _robotMoveAgentDict = _planeController._robotMoveAgentDict;
            _workstationControllerDict = _planeController._workstationControllerDict;
            rawStack = _planeController.rawStack;
            exportPlate = _planeController.exportPlate;
            possibleTargets = _planeController.possibleTargets;

            scale = _robotMoveAgent.scale;
            maxEnvSteps = _planeController.MaxEnvSteps;
        }

        private void FixedUpdate()
        {
            // if (!isTraining)
            // {
            //     PrintDebugInfo();
            // }
        }



        public void OnMoveAgentEnterTarget(ExchangeMessage exchangeMessage)
        {
            if (exchangeMessage == ExchangeMessage.OK)
            {
                AddReward(correctTargetAllocatedReward);
            }
            else
            {
                AddReward(wrongTargetAllocatedReward);
            }
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            var action = actions.DiscreteActions[0];
            _robotMoveAgent.target = possibleTargets[action];
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var act = actionsOut.DiscreteActions;
            act[0] = Random.Range(0, possibleTargets.Count);
            // act[0] = testAllocationCount++;
            // if (testAllocationCount >= possibleTargets.Count)
            // {
            //     testAllocationCount = 0;
            // }
        }

        private Vector2 rect2polar(Vector3 rect, Vector3 forward)
        {
            Vector3 inputCross = Vector3.Cross(rect, forward);
            float inputAngle = Vector3.Angle(rect, forward) / 180f;
            Vector2 polar = new Vector2(inputCross.y > 0 ? -inputAngle : inputAngle, rect.magnitude);
            return polar;
        }

        private void PrintDebugInfo()
        {
            // StringBuilder stringBuilder = new StringBuilder();
            // Vector3 position = _robot.transform.position;
            // Vector3 forward = _robot.transform.forward;
            // var workstationController = _workstationControllerDict.Values.FirstOrDefault();
            // stringBuilder.Append(workstationController.GetInputCapacityRatio());
            // stringBuilder.Append(workstationController.GetOutputCapacityRatio());
            // Debug.Log(stringBuilder);
        }
        
        /// <summary>
        /// 30 = 5*2*(2+1) : 5 workstations (5) * 2 triggers (2) * (polar relative position (2) + buffer capacity ratio (1))
        /// 2 : polar relative position to raw stack
        /// 2 : polar relative position to export plate
        /// 2 : self polar velocity
        /// ----------
        /// = 36
        /// </summary>
        /// <param name="sensor"></param>
        public override void CollectObservations(VectorSensor sensor)
        {

            Vector3 position = _robot.transform.position;
            Vector3 forward = _robot.transform.forward;
            foreach (var workstationController in _workstationControllerDict.Values)
            {
                GameObject input = workstationController.inputPlate;
                Vector3 inputPos = (input.transform.position - position) / scale;
                sensor.AddObservation(rect2polar(inputPos,forward));
                sensor.AddObservation(workstationController.getInputCapacityRatio());

                GameObject output = workstationController.outputPlate;
                Vector3 outputPos = (output.transform.position - position) / scale;
                sensor.AddObservation(rect2polar(outputPos,forward));
                sensor.AddObservation(workstationController.getOutputCapacityRatio());
            }
            
            Vector3 rawStackPos = (rawStack.transform.position - position) / scale;
            sensor.AddObservation(rect2polar(rawStackPos,forward));

            Vector3 exportPos = (exportPlate.transform.position - position) / scale;
            sensor.AddObservation(rect2polar(exportPos,forward));
            
            sensor.AddObservation(_robotMoveAgent.polarVelocity);
            
            
            // Buffer Sensor: variable observation for other agents:
            // one-hot holding item type(6+1), null for last digit
            // agent PolarVelocity(2)
            // agent polar relative position(2)
            // = 11 for each agent
            int enumLength = Enum.GetNames(typeof(ItemType)).Length;
            foreach (var a in _robotMoveAgentDict.Values)
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
                Vector3 cross = Vector3.Cross(targetPos, forward);
                float angle = Vector3.Angle(targetPos, forward) / 180f;
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

        public void RequestNewTarget(TravelStatus lastTravelStatus)
        {
            AddReward(lastTravelStatus.CollisionCount * travelCollisionRewardFactor +
                      lastTravelStatus.WrongTriggerCount * travelWrongTriggerEnterRewardFactor);
            RequestDecision();
        }

        public void UnallocatedCorrectTargetEntered()
        {
            AddReward(wrongTargetAllocatedReward);
        }
    }
}
