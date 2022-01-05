using System;
using System.Collections.Generic;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

namespace Multi
{
    public class RawStackController : ItemHolder
    {
        public ItemType rawItemType = ItemType.Raw;
        private GameObject rawPrefab;
        private PlaneController _planeController;
        private Item bufferItem;

        private void Start()
        {
            _planeController = GetComponentInParent<PlaneController>();
            rawPrefab = _planeController.GetTypePrefab(rawItemType);
            bufferItem = new Item(rawItemType,Instantiate(rawPrefab, transform.position,Quaternion.identity));
        }

        public override Item GetItem()
        {
            return bufferItem;
        }

        public override ExchangeMessage CheckOfferable(ItemHolder receiver, Item item)
        {
            if (item.itemType == rawItemType)
            {
                return ExchangeMessage.OK;
            }
            return ExchangeMessage.WrongType;
        }

        public override ExchangeMessage CheckReceivable(ItemHolder giver, Item item)
        {
            return ExchangeMessage.Unreceivable;
        }

        protected override bool Store(Item item)
        {
            return false;
        }

        protected override bool Remove(Item item)
        {
            bufferItem = new Item(rawItemType,Instantiate(rawPrefab, transform.position,Quaternion.identity));
            return true;
        }
        

    }
}