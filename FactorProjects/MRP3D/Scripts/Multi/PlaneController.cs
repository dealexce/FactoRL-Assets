using System;
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
        public float InputTypeErrorReward = -1f;
        public float CollisionReward = -1f;
        public float CorrectItemDeliveredReward = 1f;
        public float ProductDeliveredReward = 10f;

        private SimpleMultiAgentGroup _simpleMultiAgentGroup;
        public List<GameObject> _workstationList = new List<GameObject>();
        public Dictionary<GameObject, WorkStationController> _workstationControllerDict =
            new Dictionary<GameObject, WorkStationController>();

        public GameObject rawStack;
        public GameObject exportPlate;

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
            
            foreach (var workstation in _workstationList)
            {
                _workstationControllerDict[workstation] = workstation.GetComponent<WorkStationController>();
            }
        }

        public void OnRewardEvent(Event eventType)
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
            //         _simpleMultiAgentGroup.AddGroupReward(CorrectItemDeliveredReward);
            //         break;
            //     case Event.ProductDelivered:
            //         _simpleMultiAgentGroup.AddGroupReward(ProductDeliveredReward);
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
            foreach (var workstation in _workstationList)
            {
                _workstationControllerDict[workstation].ResetStation();
            }
        }
    }
}
