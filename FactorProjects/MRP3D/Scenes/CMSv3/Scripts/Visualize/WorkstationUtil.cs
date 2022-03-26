using System.Collections.Generic;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts.Visualize
{
    public class WorkstationUtil
    {
        private static Dictionary<string, GameObject> _workstationMachinePrefabDict = new Dictionary<string, GameObject>();

        public static void Init(Workstation[] workstations, string prefabPath)
        {
            GameObject[] prefabs = Resources.LoadAll<GameObject>(prefabPath);
            if (prefabs.Length < workstations.Length)
            {
                Debug.LogWarning("Workstation types are more than machine prefabs. " +
                                 "Some workstations may not have allocated machine prefab to show");
            }
            int pid = 0;
            foreach (var ws in workstations)
            {
                if(pid<prefabs.Length)
                    _workstationMachinePrefabDict.Add(ws.id,prefabs[pid++]);
                else
                    break;
            }
        }

        public static GameObject GetMachinePrefab(string id)
        {
            return _workstationMachinePrefabDict[id];
        }
    }
}
