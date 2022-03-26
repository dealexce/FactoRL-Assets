using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public abstract class ItemHolder : MonoBehaviour
    {
        //获取一个类型为itemType的Item引用
        public abstract Item GetItem(string itemType);
        public HashSet<string> supportInputs = new HashSet<string>();
        public GameObject InputGameObject;
        public GameObject OutputGameObject;


        /// <summary>
        /// 将指定的item给予指定的receiver，给予时先用giver的CheckOfferable检查是否
        /// 可以给予，如果不能给予返回对应的ExchangeMessage。然后调用receiver的
        /// Receive方法，让receiver接收该item，如果返回OK说明接收成功，随后调用giver
        /// 的Remove方法移除giver手中的item
        /// PS：giver负责检查offerable和remove，receiver负责检查receivable和store
        /// </summary>
        /// <param name="receiver">收到该item的接收者</param>
        /// <param name="item">指定一个item</param>
        /// <returns></returns>
        public ExchangeMessage Give(ItemHolder receiver, Item item)
        {
            ExchangeMessage exchangeMessage = CheckGivable(receiver,item);
            if (exchangeMessage != ExchangeMessage.OK)
            {
                return exchangeMessage;
            }
            exchangeMessage = receiver.CheckReceivable(this, item);
            if (exchangeMessage != ExchangeMessage.OK)
            {
                return exchangeMessage;
            }
            exchangeMessage = receiver.Receive(this, item);
            if (exchangeMessage == ExchangeMessage.OK)
            {
                this.Remove(item);
            }
            return exchangeMessage;
        }

        /// <summary>
        /// 检查来自giver的item
        /// </summary>
        /// <param name="giver"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private ExchangeMessage Receive(ItemHolder giver, Item item)
        {
            ExchangeMessage exchangeMessage = ExchangeMessage.OK;
            if (!Store(item))
            {
                    exchangeMessage = ExchangeMessage.StoreFail;
            }
            OnReceived(exchangeMessage);
            return exchangeMessage;
        }
        
        /// <summary>
        /// 无giver地收到一个item，设置giver为null
        /// 可以通过override CheckReceivable()来阻止无giver地接收item行为
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public ExchangeMessage Receive(Item item)
        {
            return Receive(null, item);
        }

        private IEnumerator CallOnReceived(ExchangeMessage exchangeMessage)
        {
            yield return null;
            OnReceived(exchangeMessage);
        }
        
        protected virtual void OnReceived(ExchangeMessage exchangeMessage)
        {
            
        }

        private IEnumerator CallOnRequest(ExchangeMessage exchangeMessage)
        {
            yield return null;
            OnRequest(exchangeMessage);
        }

        protected virtual void OnRequest(ExchangeMessage exchangeMessage)
        {
            
        }

        public abstract ExchangeMessage CheckReceivable(ItemHolder giver, Item item);
        public abstract ExchangeMessage CheckGivable(ItemHolder receiver, Item item);

        protected abstract bool Store(Item item);
        protected abstract bool Remove(Item item);
        
    }
}
