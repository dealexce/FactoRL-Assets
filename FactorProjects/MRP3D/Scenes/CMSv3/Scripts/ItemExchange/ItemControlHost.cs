using System;
using System.Collections.Generic;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public static class ItemControlHost
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="giver"></param>
        /// <param name="receiver"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private static ExchangeMessage PassItem(IExchangeable giver, IExchangeable receiver, string id)
        {
            return PassItem(giver, receiver, giver.GetItem(id));
        }
        private static ExchangeMessage PassItem(IExchangeable giver, IExchangeable receiver, Item item)
        {
            ExchangeMessage exchangeMessage = giver.CheckGivable(receiver,item);
            giver.OnRequest(exchangeMessage);
            if (exchangeMessage != ExchangeMessage.Ok)
                return exchangeMessage;
            
            exchangeMessage = receiver.CheckReceivable(giver, item);
            if (exchangeMessage != ExchangeMessage.Ok)
                return exchangeMessage;
            
            if(giver.Remove(item)&&receiver.Store(item))
            {
                return ExchangeMessage.Ok;
            }
            Debug.LogError("Error occured when exchanging item");
            return ExchangeMessage.Error;
        }
        public static ExchangeMessage PassItem(IExchangeable giver, IExchangeable receiver, Item item, Transform newParent)
        {
            ExchangeMessage exchangeMessage = PassItem(giver, receiver, item);
            if (exchangeMessage == ExchangeMessage.Ok)
            {
                item.transform.SetParent(newParent);
            }
            receiver.OnReceived(exchangeMessage);
            giver.OnGiven(exchangeMessage);
            return exchangeMessage;
        }
        public static ExchangeMessage PassItem(IExchangeable giver, IExchangeable receiver, string id, Transform newParent)
        {
            return PassItem(giver,receiver,giver.GetItem(id),newParent);
        }
    }
}
