using System.Collections;
using System.Collections.Generic;
using Multi;
using UnityEngine;

public class Item
{
    public ItemType itemType { get; private set; }
    public GameObject gameObject { get; private set; }

    public Item(ItemType itemType, GameObject gameObject)
    {
        this.itemType = itemType;
        this.gameObject = gameObject;
    }

}
