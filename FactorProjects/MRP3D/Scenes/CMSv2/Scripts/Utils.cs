using System.Collections.Generic;
using UnityEngine.UI;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class Utils
    {
        public static Dictionary<T,int> ToIndexDict<T>(List<T> list)
        {
            Dictionary<T, int> dict = new Dictionary<T, int>();
            for (int i = 0; i < list.Count; i++)
            {
                dict.Add(list[i],i);
            }
            return dict;
        }
    }
}
