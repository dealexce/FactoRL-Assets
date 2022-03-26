using System;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class ImportController : ItemHolder, LinkedToPlane
    {
        public string rawType;
        public PlaneController _planeController { get; set; }

        public Item bufferItem;


        private void Awake()
        {
            OutputGameObject = gameObject;
            _planeController = GetComponentInParent<PlaneController>();
        }

        private void Start()
        {
            bufferItem = _planeController.InstantiateItem(rawType, gameObject);
        }

        public override Item GetItem(string itemType)
        {
            return bufferItem;
        }

        protected override bool Remove(Item item)
        {
            if (item != bufferItem)
                return false;
            bufferItem = _planeController.InstantiateItem(rawType, gameObject);
            return true;
        }

        protected override bool Store(Item item)
        {
            return false;
        }

        public override ExchangeMessage CheckReceivable(ItemHolder giver, Item item)
        {
            return ExchangeMessage.Unreceivable;
        }

        public override ExchangeMessage CheckGivable(ItemHolder receiver, Item item)
        {
            if (rawType.Equals(item.itemType))
            {
                return ExchangeMessage.OK;
            }
            return ExchangeMessage.WrongType;
        }
    }
}
