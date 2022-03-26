using System;
using System.Collections.Generic;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{

    public class AgvBase : MonoBehaviour
    {
        [HideInInspector]
        public PlaneController _planeController { get; set; }

        [HideInInspector]
        public Agv agvConfig;

        private Rigidbody _rigidbody;

        //Move settings
        public float moveSpeed = 5;
        public float rotateSpeed = 3;

    }
}
