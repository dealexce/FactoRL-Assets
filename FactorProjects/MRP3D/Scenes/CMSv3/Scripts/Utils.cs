using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
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

        public static Vector2 NormalizedPolarRelativePosition(Transform self, Transform other, float maxDistance)
        {
            Vector3 relativePos = (other.position - self.position);
            Vector3 cross = Vector3.Cross(relativePos, self.forward);
            float angle = Vector3.Angle(relativePos, self.forward) / 180f;
            return new Vector2(
                cross.y > 0 ? -angle : angle, 
                NormalizeValue(relativePos.magnitude,0f,maxDistance));
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
        
        public static Bounds GetEncapsulateBoxColliderBounds(GameObject assetModel)
        {
            var pos = assetModel.transform.localPosition;
            var rot = assetModel.transform.localRotation;
            var scale = assetModel.transform.localScale;

            // need to clear out transforms while encapsulating bounds
            assetModel.transform.localPosition = Vector3.zero;
            assetModel.transform.localRotation = Quaternion.identity;
            assetModel.transform.localScale = Vector3.one;

            // start with root object's bounds
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            if (assetModel.transform.TryGetComponent<BoxCollider>(out var mainBoxCollider))
            {
                // as mentioned here https://forum.unity.com/threads/what-are-bounds.480975/
                // new Bounds() will include 0,0,0 which you may not want to Encapsulate
                // because the vertices of the mesh may be way off the model's origin
                // so instead start with the first renderer bounds and Encapsulate from there
                bounds = mainBoxCollider.bounds;
            }

            var descendants = assetModel.GetComponentsInChildren<Transform>();
            foreach (Transform desc in descendants)
            {
                if (desc.TryGetComponent<BoxCollider>(out var childBoxCollider))
                {
                    // use this trick to see if initialized to renderer bounds yet
                    // https://answers.unity.com/questions/724635/how-does-boundsencapsulate-work.html
                    if (bounds.extents == Vector3.zero)
                        bounds = childBoxCollider.bounds;
                    bounds.Encapsulate(childBoxCollider.bounds);
                }
            }

            // restore transforms
            assetModel.transform.localPosition = pos;
            assetModel.transform.localRotation = rot;
            assetModel.transform.localScale = scale;

            return bounds;
        }

        public static void ForcePhysicsSimulate()
        {
            Physics.autoSimulation=false;
            Physics.Simulate(Time.fixedDeltaTime);
            Physics.autoSimulation = true;
        }
        public static float NormalizeValue(float value, float minValue, float maxValue)
        {
            return (Math.Clamp(value, minValue, maxValue) - minValue) / (maxValue - minValue);
        }
    }
}
