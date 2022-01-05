using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Multi
{
    public class RobotDispatcher : MonoBehaviour
    {
        public List<GameObject> possibleTargets;
        private int targetsLength;

        private void Start()
        {
            targetsLength = possibleTargets.Count;
        }

        public GameObject DispatchNewTarget()
        {
            return possibleTargets[Random.Range(0, targetsLength)];
        }
        
        
        
    }
}
