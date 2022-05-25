using System;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class GlobalSetting : MonoBehaviour
    {
        
        public string scenarioXmlPath;
        public bool randomizeLayout = false;
        public bool OnlyLogError = false;
        private void Start()
        {
            if(OnlyLogError)
                Debug.unityLogger.filterLogType = LogType.Error;
        }

        public bool UseUnionActionSpace = false;
        
        public enum DecisionMethod
        {
            HETE,RVA,EDD,FCFS,LCFS,LET,SET
        }

        public DecisionMethod agvDecisionMethod;
        public DecisionMethod workstationDecisionMethod;
    }
}
