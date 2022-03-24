using System;
using TMPro;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class ExportController : ItemHolder, LinkedToPlane, Resetable
    {
        public string productType;
        public PlaneController _planeController { get; set; }
        public GameObject FloatingTextObject;
        private TextMeshPro _textMesh;
        public int stock = 0;

        public void EpisodeReset()
        {
            stock = 0;
            refreshText();
        }

        private void Awake()
        {
            InputGameObject = gameObject;
            _planeController = GetComponentInParent<PlaneController>();
            _textMesh = FloatingTextObject.GetComponent<TextMeshPro>();
        }

        private void Start()
        {
            refreshText();
        }

        protected override void OnReceived(ExchangeMessage exchangeMessage)
        {
            //TODO:告知planeController
        }

        public override Item GetItem(string itemType)
        {
            return null;
        }

        public override ExchangeMessage CheckGivable(ItemHolder receiver, Item item)
        {
            return ExchangeMessage.Ungivable;
        }

        public override ExchangeMessage CheckReceivable(ItemHolder giver, Item item)
        {
            if (productType.Equals(item.itemType))
            {
                return ExchangeMessage.OK;
            }
            return ExchangeMessage.WrongType;
        }

        protected override bool Store(Item item)
        {
            if (productType.Equals(item.itemType))
            {
                Destroy(item.gameObject);
                if(!_planeController.TryFinishOrder(productType)){
                    stock++;
                    refreshText();
                }
                return true;
            }
            return false;
        }

        protected override bool Remove(Item item)
        {
            return false;
        }

        public void RemoveOneStock()
        {
            if (stock > 0)
            {
                stock--;
                refreshText();
            }
            else
                Debug.LogError("NO MORE STOCK BUT ASKED TO REMOVE ONE: "+productType);
        }

        public void refreshText()
        {
            _textMesh.text = productType + "*" + stock;
        }
    }
}
