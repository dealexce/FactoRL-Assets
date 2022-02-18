using System;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class Item : MonoBehaviour
    {
        public string itemType { get; private set; }
        public GameObject FloatingTextObject;
        private TextMesh _textMesh;

        private void Awake()
        {
            _textMesh = FloatingTextObject.GetComponent<TextMesh>();
            _textMesh.text = itemType;
        }

        public void setItemType(string itemType)
        {
            this.itemType = itemType;
            _textMesh.text = this.itemType;
        }
    }
}
