using System;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class ImportController : ItemHolder
    {
        public string rawType;
        public PlaneController _planeController;

        public Item bufferItem;
        //TODO:实现ImportController
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
