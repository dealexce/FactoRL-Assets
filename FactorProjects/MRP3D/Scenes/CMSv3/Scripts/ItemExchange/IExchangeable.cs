using System.Collections;
using System.Collections.Generic;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public interface IExchangeable
    {
        public Item GetItem(string id);

        public void OnReceived(ExchangeMessage exchangeMessage)
        {
            
        }
        
        public void OnRequest(ExchangeMessage exchangeMessage)
        {
            
        }

        public void OnGiven(ExchangeMessage exchangeMessage)
        {
            
        }

        public  ExchangeMessage CheckReceivable(IExchangeable giver, Item item);
        public  ExchangeMessage CheckGivable(IExchangeable receiver, Item item);
        
        public bool Store(Item item);
        public bool Remove(Item item);
    }
}
