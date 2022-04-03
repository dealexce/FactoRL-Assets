using System;
using TMPro;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class Item : MonoBehaviour
    {
        public ItemState itemState;
        
        public GameObject floatingTextObject;
        private TextMeshPro _textMesh;

        private void Awake()
        {
            _textMesh = floatingTextObject.GetComponent<TextMeshPro>();
        }

        private void Start()
        {
            RefreshText();
        }

        private void RefreshText()
        {
            _textMesh.text = itemState.name;
        }

        public void SetItemState(ItemState itemState)
        {
            this.itemState = itemState;
            RefreshText();
        }
    }
}
