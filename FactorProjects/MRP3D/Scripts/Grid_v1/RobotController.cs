using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;

namespace Grid_v1
{
    public class RobotController : MonoBehaviour
    {
        private Grid_v1.AgentController _agentController;

        private void Start()
        {
            _agentController = GetComponentInParent<AgentController>();
        }

        private void OnCollisionEnter(Collision other)
        {
            _agentController.CollisionDetected(other);
        }

        private void OnTriggerEnter(Collider other)
        {
            _agentController.TriggerDetected(other);
        }

    }
}

