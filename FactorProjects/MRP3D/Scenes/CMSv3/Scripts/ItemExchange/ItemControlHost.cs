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
                receiver.OnReceived(exchangeMessage);
                return ExchangeMessage.Ok;
            }
            Debug.LogError("Error occured when exchanging item");
            return ExchangeMessage.Error;
        }
        public static ExchangeMessage PassItem(IExchangeable giver, IExchangeable receiver, Item item, Transform newParent)
        {
            return PassItem(giver,receiver,item,newParent,Vector3.zero);
        }
        public static ExchangeMessage PassItem(IExchangeable giver, IExchangeable receiver, string id, Transform newParent)
        {
            return PassItem(giver,receiver,giver.GetItem(id),newParent);
        }
        public static ExchangeMessage PassItem(IExchangeable giver, IExchangeable receiver, string id, Transform newParent, Vector3 localPosition)
        {
            return PassItem(giver,receiver,giver.GetItem(id),newParent,localPosition);
        }
        public static ExchangeMessage PassItem(IExchangeable giver, IExchangeable receiver, Item item, Transform newParent, Vector3 localPosition)
        {
            ExchangeMessage exchangeMessage = PassItem(giver, receiver, item);
            if (exchangeMessage == ExchangeMessage.Ok)
            {
                var originScale = item.transform.localScale;
                Transform transform;
                (transform = item.transform).SetParent(newParent);
                transform.localPosition = localPosition;
                transform.localScale = originScale;
            }
            return exchangeMessage;
        }
    }
}
