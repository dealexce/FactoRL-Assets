using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class AGVDispatcherAgent : MonoBehaviour
    {
        private AGVController _agvController;

        private void Awake()
        {
            _agvController = GetComponentInParent<AGVController>();

        }

        private void Start()
        {

        }



        

        public Target RequestNewTarget()
        {
            int randomTargetIndex = Random.Range(0, _agvController.availableTargetsObj_forTest.Count);
            return new Target(_agvController.availableTargetsObj_forTest[randomTargetIndex],null);
        }

        // //Mask invalid actions based on AGV's holding item (and workstations' status?)
        // public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        // {
        //     base.WriteDiscreteActionMask(actionMask);
        // }
        //
        // //action is the target to where the AGV is going to navigate
        // //用一个[所有WS*WS的输出类型]的大的离散动作分支，每种代表一个“去某WS拿某Item”的动作
        // // *plus what info the AGV is going to broadcast
        // public override void OnActionReceived(ActionBuffers actions)
        // {
        //     base.OnActionReceived(actions);
        // }
        //
        // //collect relative position of all workstations
        // // *collect received broadcast info from other agents
        // public override void CollectObservations(VectorSensor sensor)
        // {
        //     base.CollectObservations(sensor);
        // }
        //
        // public override void Heuristic(in ActionBuffers actionsOut)
        // {
        //
        // }
    }
}
