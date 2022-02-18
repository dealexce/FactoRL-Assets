using System;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class AGVController : ItemHolder
    {
        public Item holdingItem;


        public override Item GetItem(string itemType)
        {
            return holdingItem;
        }

        protected override bool Remove(Item item)
        {
            if (item == holdingItem)
            {
                item = null;
                return true;
            }
            return true;
        }

        protected override bool Store(Item item)
        {
            if (holdingItem == null)
            {
                holdingItem = item;
                return true;
            }
            return false;
        }

        public override ExchangeMessage CheckReceivable(ItemHolder giver, Item item)
        {
            if (holdingItem == null)
            {
                return ExchangeMessage.OK;
            }
            else
            {
                return ExchangeMessage.Overload;
            }
        }

        public override ExchangeMessage CheckGivable(ItemHolder receiver, Item item)
        {
            if (holdingItem != null)
            {
                return ExchangeMessage.OK;
            }
            else
            {
                return ExchangeMessage.NullItem;
            }
        }
    }
}
