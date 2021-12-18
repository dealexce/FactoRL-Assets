using System;
using System.Collections.Generic;
using UnityEngine;

namespace Multi
{
    public class TypePrefabDictionary : MonoBehaviour
    {
        public GameObject Raw;
        public GameObject S1;
        public GameObject S2;
        public GameObject S3;
        public GameObject S4;
        public GameObject Product;

        private Dictionary<ItemType, GameObject> dict = new Dictionary<ItemType, GameObject>();

        private void Start()
        {
            dict[ItemType.Raw] = Raw;
            dict[ItemType.S1] = S1;
            dict[ItemType.S2] = S2;
            dict[ItemType.S3] = S3;
            dict[ItemType.S4] = S4;
            dict[ItemType.Product] = Product;
        }

        public GameObject GetPrefab(ItemType type)
        {
            return dict[type];
        }
    }
}
