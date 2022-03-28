using System;
using System.Collections.Generic;
using System.Text;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class AGVDispatcherAgent : Agent
    {
        private AgvController _AGVController;
        private PlaneController _planeController;
        private int cur = 1;
        public bool showDebug = false;
        private BufferSensorComponent _bufferSensor;

        private List<Target> actionSpace;

        private void Awake()
        {
            _AGVController = GetComponentInParent<AgvController>();
            _planeController = _AGVController.planeController;
            actionSpace = _planeController.AgvDispatcherActionSpace;
            _bufferSensor = GetComponent<BufferSensorComponent>();
        }

        private void Start()
        {

        }


        public override void OnActionReceived(ActionBuffers actions)
        {
            var action = actions.DiscreteActions[0];
            _AGVController.AssignNewTarget(actionSpace[action]);
        }


        public int RequestNewRandomTarget()
        {
            return Random.Range(0, actionSpace.Count);
        }

        public void RequestTargetDecision()
        {
            RequestDecision();
        }

        //Mask invalid actions based on AGV's holding item
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            // foreach (var target in actionSpace)
            // {
            //     if (target == TargetAction.Get)
            //     {
            //         
            //     }
            // }
            // int mc = 0;
            // List<Target> comb = _AGVController.PlaneController.TargetCombinationList;
            // //手里没有物体，只能拿不能给，屏蔽所有给的动作（comb.ItemType==null）
            // if (_AGVController.holdingItem == null)
            // {
            //     for (int i = 1; i < comb.Count; i++)
            //     {
            //         if (comb[i].TargetAction != TargetAction.Get
            //             || _AGVController.TargetableGameObjectItemHolderDict[comb[i].GameObject].GetItem(comb[i].ItemStateId)==null)
            //         {
            //             actionMask.SetActionEnabled(0, i, false);
            //             mc++;
            //         }
            //     }
            // }
            // //手里有一个itemType的物体，不能再拿只能给，TODO:屏蔽掉所有拿的动作和收不了的target
            // else
            // {
            //     for (int i = 1; i < comb.Count; i++)
            //     {
            //         if (comb[i].TargetAction != TargetAction.Give||
            //             !_AGVController.TargetableGameObjectItemHolderDict[comb[i].GameObject]
            //                 .supportInputs.Contains(_AGVController.holdingItem.itemType))
            //         {
            //             actionMask.SetActionEnabled(0, i, false);
            //             mc++;
            //         }
            //     }
            // }
            // if (mc < comb.Count - 1)
            // {
            //     actionMask.SetActionEnabled(0, 0, false);
            // }
        }
        
        /// <summary>
        /// How many orders can be observed
        /// </summary>
        public int orderObservationLength;

        //collect relative position of all workstations
        //collect status of all workstations (input/output buffer capacity ratio)
        //collect relative position of all AGVs
        //collect target of all AGVs in one-hot
        //SIZE = TargetableGameObjectItemHolderDict.Keys.Count*2+MFWSControllers.Count*2+AGVControllers.Count*TargetCombinationList.Count
        // *collect received broadcast info from other agents
        public override void CollectObservations(VectorSensor sensor)
        {
            //collect relative position and status of all workstations
            foreach (var (wsObj,wsController) in _planeController.workstationControllerDict)
            {
                Vector2 polarTargetPos = new Vector2();
                if (wsObj != null)
                {
                    polarTargetPos = Utils.NormalizedPolarRelativePosition(
                        transform,
                        wsObj.transform,
                        _AGVController.planeController.normalizationMaxDiameter);
                }
                sensor.AddObservation(polarTargetPos);
                
                var s = wsController.GetStatus();
                throw new NotImplementedException();
            }
            
            // collect relative position and status of other AGVs
            foreach (var (agvObj,agvController) in _planeController.agvControllerDict)
            {
                if (agvController == _AGVController)
                {
                    continue;
                }
                Vector2 polarTargetPos = new Vector2();
                if (agvObj != null)
                {
                    polarTargetPos = Utils.NormalizedPolarRelativePosition(
                        transform,
                        agvObj.transform,
                        _AGVController.planeController.normalizationMaxDiameter);
                }
                sensor.AddObservation(polarTargetPos);
                
                var s = agvController.GetStatus();
                throw new NotImplementedException();
            }

            // Collect product stock info
            foreach (var stock in _planeController.productStockDict.Values)
            {
                sensor.AddObservation(Utils.NormalizeValue(stock,0,_planeController.normalizationMaxStock));
            }
            
            // Collect order info
            int orderCount = 0;
            foreach (var (ddl,order) in _planeController.orderList)
            {
                throw new NotImplementedException();
                orderCount++;
                if (orderCount >= orderObservationLength)
                {
                    break;
                }
            }
        }
        
        //give a random valid target
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            throw new NotImplementedException();
        }
    }
}
