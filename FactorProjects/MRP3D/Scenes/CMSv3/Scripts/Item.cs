using System;
using TMPro;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class Item : MonoBehaviour
    {
        public string itemType { get; private set; }
        public GameObject FloatingTextObject;
        private TextMeshPro _textMesh;

        private void Awake()
        {
            _textMesh = FloatingTextObject.GetComponent<TextMeshPro>();
            _textMesh.text = itemType;
        }

        public void setItemType(string itemType)
        {
            this.itemType = itemType;
            _textMesh.text = this.itemType;
        }
    }
}
