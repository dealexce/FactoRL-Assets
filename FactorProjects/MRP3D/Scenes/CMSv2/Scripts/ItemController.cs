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
            Item item = giver.GetItem(itemType);
            return giver.Give(receiver, item);
        }
        public static ExchangeMessage PassItem(ItemHolder giver, ItemHolder receiver, Item item)
        {
            return giver.Give(receiver, item);
        }
    }
}
