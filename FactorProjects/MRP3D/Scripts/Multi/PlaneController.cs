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
        public float InputTypeErrorReward = -2f;
        private SimpleMultiAgentGroup _simpleMultiAgentGroup;
        public List<GameObject> _workstationList = new List<GameObject>();

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
        }

        public void OnRewardEvent(Event eventType)
        {
            switch (eventType)
            {
                case Event.InputTypeError:
                    _simpleMultiAgentGroup.AddGroupReward(InputTypeErrorReward);
                    break;
                case Event.Collision:
                    //TODO
                    break;
            }
        }

        public GameObject GetTypePrefab(ItemType type)
        {
            if (_prefabDict.ContainsKey(type))
            {
                return _prefabDict[type];
            }
            return null;
        }
    }
}
