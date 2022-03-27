using System.Collections;
using System.Collections.Generic;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public interface IExchangable
    {
        public Item GetItem(string id);

        internal void OnReceived(ExchangeMessage exchangeMessage)
        {
            
        }
        
        internal void OnRequest(ExchangeMessage exchangeMessage)
        {
            
        }

        public  ExchangeMessage CheckReceivable(IExchangable giver, Item item);
        public  ExchangeMessage CheckGivable(IExchangable receiver, Item item);
        
        public bool Store(Item item);
        public bool Remove(Item item);
    }
}
