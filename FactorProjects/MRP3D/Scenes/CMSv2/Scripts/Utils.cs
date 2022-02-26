using System.Collections.Generic;
using UnityEngine;
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

        public static Vector2 PolarRelativePosition(Transform self, Transform other, float maxDistance)
        {
            Vector3 relativePos = (other.position - self.position) / maxDistance;
            Vector3 cross = Vector3.Cross(relativePos, self.forward);
            float angle = Vector3.Angle(relativePos, self.forward) / 180f;
            return new Vector2(cross.y > 0 ? -angle : angle, relativePos.magnitude);
        }
        
        public static float[] ToOneHotObservation(int observation, int range)
        {
            float[] oh = new float[range];
            for (var i = 0; i < range; i++)
            {
                oh[i] = (i == observation ? 1.0f : 0.0f);
            }
            return oh;
        }
    }
}
