using System;
using System.Collections.Generic;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class ImportController : MonoBehaviour, IExchangeable, ILinkedToPlane
    {
        public PlaneController planeController { get; set; }

        public Dictionary<string,Item> rawItemsDict;
        

        private void Start()
        {
            foreach (var (id,itemState) in SceanrioLoader.ItemStateDict)
            {
                rawItemsDict.Add(id,planeController.InstantiateItem(id, gameObject));
            }
        }

        public Item GetItem(string itemType)
        {
            rawItemsDict.TryGetValue(itemType, out Item item);
            return item;
        }

        public bool Remove(Item item)
        {
            var id = item.itemState.id;
            if (!rawItemsDict.ContainsKey(id))
            {
                return false;
            }
            rawItemsDict[id] = planeController.InstantiateItem(id, gameObject);
            return true;
        }

        public bool Store(Item item)
        {
            return false;
        }

        public ExchangeMessage CheckReceivable(IExchangeable giver, Item item)
        {
            return ExchangeMessage.Unreceivable;
        }

        public ExchangeMessage CheckGivable(IExchangeable receiver, Item item)
        {
            if (rawItemsDict.ContainsKey(item.itemState.id))
            {
                return ExchangeMessage.Ok;
            }
            return ExchangeMessage.WrongType;
        }
    }
}
