using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

namespace Multi
{
    /// <summary>
    /// 整个场景的Controller，包含各种全局属性和方法
    /// </summary>
    public class PlaneController : MonoBehaviour
    {
        public int MaxEnvSteps = 25000;
        private int resetTimer = 0;
        
        public float inputTypeErrorReward = -1f;
        public float collisionReward = -1f;
        public float correctItemDeliveredReward = 1f;
        public float productDeliveredReward = 10f;

        // private SimpleMultiAgentGroup _simpleMultiAgentGroup;

        public List<GameObject> _agentList = new List<GameObject>();
        public Dictionary<GameObject, ResetableAgent> _agentDict = new Dictionary<GameObject, ResetableAgent>();

        public List<GameObject> _workstationList = new List<GameObject>();
        public Dictionary<GameObject, WorkStationController> _workstationControllerDict =
            new Dictionary<GameObject, WorkStationController>();

        public GameObject rawStack;
        public GameObject exportPlate;
        
        public List<GameObject> possibleTargets;

        public GameObject ground;
        private Material groundOriginalMaterial;
        private Renderer _groundRenderer;
        public Material ProductDeliverdSuccessMaterial;

        [System.Serializable]
        public struct TypePrefab
        {
            public ItemType itemType;
            public GameObject prefab;
        }
        public TypePrefab[] _typePrefabs;
        
        private Dictionary<ItemType, GameObject> _prefabDict = new Dictionary<ItemType, GameObject>();

        private void Start()
        {
            resetTimer = 0;
            
            // _simpleMultiAgentGroup = new SimpleMultiAgentGroup();
            foreach (var agent in _agentList)
            {
                RobotMoveAgent robotAgent = agent.GetComponent<RobotMoveAgent>();
                _agentDict[agent] = robotAgent;
                //_simpleMultiAgentGroup.RegisterAgent(robotAgent);
            }

            foreach (var typePrefab in _typePrefabs)
            {
                if (!_prefabDict.ContainsKey(typePrefab.itemType))
                {
                    _prefabDict[typePrefab.itemType] = typePrefab.prefab;
                }
                else
                {
                    Debug.LogError("itemType prefab pair conflict:"+typePrefab.itemType);
                }
            }
            
            _groundRenderer = ground.GetComponent<Renderer>();
            groundOriginalMaterial = _groundRenderer.material;
            
            foreach (var workstation in _workstationList)
            {
                _workstationControllerDict[workstation] = workstation.GetComponent<WorkStationController>();
            }
        }

        public void OnRewardEvent(Event eventType, float factor=1.0f)
        {
            //TODO 设置MA Group
            // switch (eventType)
            // {
            //     case Event.InputTypeError:
            //         _simpleMultiAgentGroup.AddGroupReward(InputTypeErrorReward);
            //         break;
            //     case Event.Collision:
            //         _simpleMultiAgentGroup.AddGroupReward(CollisionReward);
            //         break;
            //     case Event.CorrectItemDelivered:
            //         _simpleMultiAgentGroup.AddGroupReward(CorrectItemDeliveredReward*factor);
            //         break;
            //     case Event.ProductDelivered:
            //         _simpleMultiAgentGroup.AddGroupReward(ProductDeliveredReward);
            //         StartCoroutine(ProductReceivedSwapMaterial(ProductDeliverdSuccessMaterial, 1f));
            //         break;
            // }
        }

        public GameObject GetTypePrefab(ItemType type)
        {
            if (_prefabDict.ContainsKey(type))
            {
                return _prefabDict[type];
            }
            return null;
        }

        public void ResetPlane()
        {
            foreach (var item in _agentDict.Values)
            {
                item.ResetRobot();
            }
            
            foreach (var item in _workstationControllerDict.Values)
            {
                item.ResetStation();
            }
        }

        private void FixedUpdate()
        {
            resetTimer += 1;
            if (resetTimer >= MaxEnvSteps && MaxEnvSteps > 0)
            {
                //_simpleMultiAgentGroup.GroupEpisodeInterrupted();
                ResetPlane();
                resetTimer = 0;
            }

            //Hurry Up Penalty
            //_simpleMultiAgentGroup.AddGroupReward(-0.5f / MaxEnvSteps);
        }
        
        /// <summary>
        /// Swap ground material, wait time seconds, then swap back to the regular material.
        /// </summary>
        IEnumerator ProductReceivedSwapMaterial(Material mat, float time)
        {
            _groundRenderer.material = mat;
            yield return new WaitForSeconds(time);
            _groundRenderer.material = groundOriginalMaterial;
        }
    }
}
