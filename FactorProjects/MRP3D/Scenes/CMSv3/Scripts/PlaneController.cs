using System.Collections.Generic;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class PlaneController : ScenarioGenerator
    {
        public Dictionary<string, int> stockDict = new Dictionary<string, int>();
        public GameObject itemPrefab;

        new void Start()
        {
            base.Start();
            // init stockDict
            foreach (var itemState in _scenario.model.itemStates)
            {
                if (itemState.type == SpecialItemStateType.Product)
                {
                    stockDict.Add(itemState.id,0);
                }
            }
        }
        public Item InstantiateItem(string id, GameObject parentObj)
        {
            ItemState itemState = SceanrioLoader.getItemState(id);
            if (itemState!=null)
            {
                GameObject obj = Instantiate(itemPrefab, parentObj.transform);
                Item item = obj.GetComponent<Item>();
                item.SetItemState(itemState);
                return item;
            }
            else
            {
                Debug.LogError("Tried to instantiate an item not defined in description model: "+id);
                return null;
            }
        }
        public void DeliverProduct(Item item)
        {
            stockDict[item.itemState.id]++;
            Destroy(item.gameObject);
        }
    }
}
