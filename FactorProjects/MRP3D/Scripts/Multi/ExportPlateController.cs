using UnityEngine;

namespace Multi
{
    public class ExportPlateController : ItemHolder
    {
        public ItemType productType = ItemType.Product;
        private PlaneController _planeController;

        private void Start()
        {
            _planeController = GetComponentInParent<PlaneController>();
        }

        public override Item GetItem()
        {
            return null;
        }

        public override ExchangeMessage CheckOfferable(ItemHolder receiver, Item item)
        {
            return ExchangeMessage.Ungivable;
        }

        public override ExchangeMessage CheckReceivable(ItemHolder giver, Item item)
        {
            if (item == null)
            {
                return ExchangeMessage.NullItem;
            }
            if (item.itemType == productType)
            {
                return ExchangeMessage.OK;
            }
            return ExchangeMessage.WrongType;
        }

        protected override bool Store(Item item)
        {
            Destroy(item.gameObject);
            return true;
        }

        protected override bool Remove(Item item)
        {
            return false;
        }

        protected override void OnReceived(ExchangeMessage exchangeMessage)
        {
            if (exchangeMessage == ExchangeMessage.OK)
            {
                _planeController.OnRewardEvent(Event.ProductDelivered);
                
            }
        }
        

    }
}
