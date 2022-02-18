using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class ExportController : ItemHolder
    {
        public string productType;
        public PlaneController _planeController;
        //TODO:实现ExportController

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
                return true;
            }
            return false;
        }

        protected override bool Remove(Item item)
        {
            return false;
        }
    }
}
