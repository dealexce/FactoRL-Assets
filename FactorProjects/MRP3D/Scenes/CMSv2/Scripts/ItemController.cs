using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public static class ItemController
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="giver"></param>
        /// <param name="receiver"></param>
        /// <param name="itemType"></param>
        /// <returns></returns>
        public static ExchangeMessage PassItem(ItemHolder giver, ItemHolder receiver, string itemType)
        {
            return PassItem(giver, receiver, giver.GetItem(itemType));
        }
        public static ExchangeMessage PassItem(ItemHolder giver, ItemHolder receiver, Item item)
        {
            return giver.Give(receiver, item);
        }
        public static ExchangeMessage PassItem(ItemHolder giver, ItemHolder receiver, Item item, Transform newParent)
        {
            return PassItem(giver,receiver,item,newParent,Vector3.zero);
        }
        public static ExchangeMessage PassItem(ItemHolder giver, ItemHolder receiver, string itemType, Transform newParent)
        {
            return PassItem(giver,receiver,giver.GetItem(itemType),newParent);
        }
        public static ExchangeMessage PassItem(ItemHolder giver, ItemHolder receiver, string itemType, Transform newParent, Vector3 localPosition)
        {
            return PassItem(giver,receiver,giver.GetItem(itemType),newParent,localPosition);
        }
        public static ExchangeMessage PassItem(ItemHolder giver, ItemHolder receiver, Item item, Transform newParent, Vector3 localPosition)
        {
            ExchangeMessage exchangeMessage = PassItem(giver, receiver, item);
            if (exchangeMessage == ExchangeMessage.OK)
            {
                item.transform.parent = newParent;
                item.transform.localPosition = localPosition;
            }
            return exchangeMessage;
        }
    }
}
