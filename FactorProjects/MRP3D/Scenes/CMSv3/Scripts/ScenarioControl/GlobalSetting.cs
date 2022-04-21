using System;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class GlobalSetting : MonoBehaviour
    {
        public bool OnlyLogError = false;
        private void Start()
        {
            if(OnlyLogError)
                Debug.unityLogger.filterLogType = LogType.Error;
        }

        public bool UseUnionActionSpace = false;
    }
}
