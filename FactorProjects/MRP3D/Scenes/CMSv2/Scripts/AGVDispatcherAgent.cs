using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class AGVDispatcherAgent : Agent
    {
        public GameObject robot;
        private AGVMoveAgent _agvMoveAgent;
        public PlaneController _planeController;
        public Dictionary<GameObject,ItemHolder> AvailableTargets { get; private set; }
        public List<GameObject> availableTargetsObj;
        private int cur = 0;

        private void Start()
        {
            _agvMoveAgent = robot.GetComponent<AGVMoveAgent>();
            _agvMoveAgent.planeController = _planeController;
            AvailableTargets = _planeController.AvailableTargets;
            availableTargetsObj = new List<GameObject>(AvailableTargets.Keys);
        }

        public void assignNewTarget(ItemHolder itemHolder, string itemType)
        {
            _agvMoveAgent.targetItemHolder = itemHolder;
            _agvMoveAgent.targetItemType = itemType;
        }

        private void Update()
        {
            if (Input.anyKeyDown)
            {
                assignNewTarget(AvailableTargets[availableTargetsObj[cur%availableTargetsObj.Count]],"A0");
                cur++;
            }
        }

        //Mask invalid actions based on AGV's holding item (and workstations' status?)
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            base.WriteDiscreteActionMask(actionMask);
        }
        
        //action is the target to where the AGV is going to navigate
        //用一个[所有WS*WS的输出类型]的大的离散动作分支，每种代表一个“去某WS拿某Item”的动作
        // *plus what info the AGV is going to broadcast
        public override void OnActionReceived(ActionBuffers actions)
        {
            base.OnActionReceived(actions);
        }
        
        //collect relative position of all workstations
        // *collect received broadcast info from other agents
        public override void CollectObservations(VectorSensor sensor)
        {
            base.CollectObservations(sensor);
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {

        }
    }
}
