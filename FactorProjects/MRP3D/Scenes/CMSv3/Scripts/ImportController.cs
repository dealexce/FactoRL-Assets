using System;
using System.Collections.Generic;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class ImportController : MonoBehaviour, IExchangable, ILinkedToPlane
    {
        public PlaneController planeController { get; set; }

        public Dictionary<string,Item> bufferRawItems;
        

        private void Start()
        {
            foreach (var (id,itemState) in SceanrioLoader.ItemStateDict)
            {
                bufferRawItems.Add(id,planeController.InstantiateItem(id, gameObject));
            }
        }

        public Item GetItem(string itemType)
        {
            bufferRawItems.TryGetValue(itemType, out Item item);
            return item;
        }

        public bool Remove(Item item)
        {
            var id = item.itemState.id;
            if (!bufferRawItems.ContainsKey(id))
            {
                return false;
            }
            bufferRawItems[id] = planeController.InstantiateItem(id, gameObject);
            return true;
        }

        public bool Store(Item item)
        {
            return false;
        }

        public ExchangeMessage CheckReceivable(IExchangable giver, Item item)
        {
            return ExchangeMessage.Unreceivable;
        }

        public ExchangeMessage CheckGivable(IExchangable receiver, Item item)
        {
            if (bufferRawItems.ContainsKey(item.itemState.id))
            {
                return ExchangeMessage.Ok;
            }
            return ExchangeMessage.WrongType;
        }
    }
}
