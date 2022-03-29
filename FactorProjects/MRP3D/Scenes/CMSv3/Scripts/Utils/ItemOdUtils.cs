using System;
using System.Collections.Generic;
using OD;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class ItemOdUtils
    {
        public static void ClearLists(IEnumerable<List<Item>> lists)
        {
            foreach (var list in lists)
            {
                list.Clear();
            }
        }
        
        public static void DestroyAndClearLists(IEnumerable<List<Item>> lists, Action<GameObject> destroyAction)
        {
            IterateLists(lists,
                l=>l.Clear(),
                i=>destroyAction(i.gameObject));
        }

        public static void IterateLists<T>(IEnumerable<List<T>> lists, Action<List<T>> listAction = null, Action<T> itemAction= null)
        {
            foreach (var list in lists)
            {
                foreach (var item in list)
                {
                    if(itemAction!=null)
                        itemAction(item);
                }
                if(listAction!=null)
                    listAction(list);
            }
        }

        public static int ListsSumCount<T>(IEnumerable<List<T>> lists)
        {
            int countSum = 0;
            foreach (var list in lists)
            {
                countSum += list.Count;
            }
            return countSum;
        }
    }
}
