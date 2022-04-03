using System;
using System.Collections.Generic;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class ImportController : MonoBehaviour, IExchangeable, ILinkedToPlane, IManualInit<ImportStation>
    {
        public PlaneController PlaneController { get; set; }

        public Dictionary<string,Item> RawItemsDict;
        

        public void Init(ImportStation importStation=null)
        {
            RawItemsDict = new Dictionary<string, Item>();
            foreach (var (id,itemState) in SceanrioLoader.ItemStateDict)
            {
                RawItemsDict.Add(id,PlaneController.InstantiateItem(id, gameObject));
            }
        }

        public Item GetItem(string itemType)
        {
            RawItemsDict.TryGetValue(itemType, out Item item);
            return item;
        }

        public bool Remove(Item item)
        {
            var id = item.itemState.id;
            if (!RawItemsDict.ContainsKey(id))
            {
                return false;
            }
            RawItemsDict[id] = PlaneController.InstantiateItem(id, gameObject);
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
            if (RawItemsDict.ContainsKey(item.itemState.id))
            {
                return ExchangeMessage.Ok;
            }
            return ExchangeMessage.WrongType;
        }
    }
}
