﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using FactorProjects.MRP3D.Scenes.CMSv3.Scripts.Visualize;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class ScenarioGenerator : MonoBehaviour
    {
        public string scenarioXmlPath = "Assets/FactorProjects/MRP3D/Scenes/CMSv3/config/scenarios/Scenario220327032734.xml";
        public bool randomizeLayout = false;
        //TODO: Show only when randomizeLayout is set to true
        public float minGroundX = 40f, maxGroundX = 60f;
        public float minGroundY = 40f, maxGroundY = 60f;
        
        protected Scenario _scenario;

        private Ground _ground;

        public GameObject workstationPrefab;
        public string machinePrefabPath = "Machines";
        public bool showMachine = false;

        public GameObject agvPrefab;

        public GameObject importStationPrefab;
        public GameObject exportStationPrefab;

        public List<GameObject> entityGameObjects = new List<GameObject>();


        public void Start()
        {
            _ground = GetComponentInChildren<Ground>();
            SceanrioLoader.Load(scenarioXmlPath);
            _scenario = SceanrioLoader.getScenario();
            Assert.IsNotNull(_scenario);
            InitLayout();
            if (randomizeLayout)
            {
                RandomizeLayout();
            }
        }

        protected void InitLayout()
        {
            //change ground size
            _ground.changeSize(_scenario.layout.groundSize.x, _scenario.layout.groundSize.y);

            //instantiate workstation instances according to layout
            WorkstationUtil.Init(_scenario.model.workstations, machinePrefabPath);
            foreach (var wsi in _scenario.layout.workstationInstances)
            {
                InstantiateWorkstation(wsi);
            }

            //instantiate agv instances according to layout
            foreach (var agv in _scenario.layout.agvInstances)
            {
                InstantiateAgvInstance(agv);
            }

            //instantiate import station and export station according to layout
            ImportStation importStation = _scenario.layout.importStation;
            InstantiateEntityOnGround(importStationPrefab, importStation.x, importStation.y);
            ExportStation exportStation = _scenario.layout.exportStation;
            InstantiateEntityOnGround(exportStationPrefab, exportStation.x, exportStation.y);
        }

        #region Randomization

        protected void RandomizeLayout()
        {
            float randomX= Random.Range(minGroundX, maxGroundX);
            float randomY = Random.Range(minGroundY, maxGroundY);
            _ground.changeSize(randomX, randomY);

            // Deactivate objects before reset position for better position randomization
            // TODO: Check whether this will cause problems
            foreach (var eo in entityGameObjects)
            {
                eo.SetActive(false);
            }

            foreach (var eo in entityGameObjects)
            {
                eo.SetActive(true);
                ResetToSafeRandomPosition(eo);
            }
        }
        
        private Vector3 ResetToSafeRandomPosition(GameObject prefab)
        {
            //更改物体位置之后要手动调用Physics.Simulate，否则可能无法检测到碰撞！
            Utils.ForcePhysicsSimulate();

            float groundSizeX = _scenario.layout.groundSize.x;
            float groundSizeY = _scenario.layout.groundSize.y;
            
            //100次以内尝试
            int remainAttempts = 100;
            bool safePositionFound = false;
            var potentialPosition = Vector3.zero;
            //获取碰撞盒外接正方体
            Bounds bounds = Utils.GetEncapsulateBoxColliderBounds(prefab);
            while (!safePositionFound && remainAttempts>0)
            {
                potentialPosition = new Vector3(Random.Range(-groundSizeX/2f, groundSizeX/2f),bounds.extents.y,Random.Range(-groundSizeY/2f, groundSizeY/2f));
                potentialPosition = transform.position + potentialPosition;
                remainAttempts--;
                LayerMask mask = LayerMask.GetMask("Default","Trigger");    //添加遮罩：只检测Default层
                Collider[] colliders = Physics.OverlapBox(potentialPosition, bounds.extents,Quaternion.identity,mask);

                safePositionFound = colliders.Length == 0;
            }
            if (safePositionFound)
            {
                potentialPosition = new Vector3(potentialPosition.x, transform.position.y+prefab.transform.position.y - bounds.min.y+0.1f, potentialPosition.z);
                prefab.transform.position = potentialPosition;
            }
            else
            {
                Debug.LogError("Unable to find a safe position to reset work point: "+prefab.name);
            }
            return potentialPosition;
        }

        #endregion

        #region Instantiation

        protected void InstantiateAgvInstance(AgvInstance agv)
        {
            var g = InstantiateEntityOnGround(agvPrefab, agv.x, agv.y);
            AgvBase agvBase = g.GetComponent<AgvBase>();
            agvBase.agvConfig = _scenario.model.agv;
        }

        protected void InstantiateWorkstation(WorkstationInstance wsi)
        {
            InstantiateWorkstation(wsi.workstationRef.idref,wsi.x,wsi.y);
        }

        protected void InstantiateWorkstation(string id, float x, float y)
        {
            GameObject g = InstantiateEntityOnGround(workstationPrefab, x, y);
            if (showMachine)
            {
                Instantiate(WorkstationUtil.GetMachinePrefab(id), g.transform);
            }
            WorkstationBase controller = g.GetComponent<WorkstationBase>();
            controller.workstation = SceanrioLoader.getWorkstation(id);
        }

        public GameObject InstantiateEntityOnGround(GameObject prefab, float x, float z)
        {
            GameObject entityGameObject = Instantiate(
                prefab, 
                transform.position+new Vector3(x, Utils.GetEncapsulateBoxColliderBounds(prefab).extents.y, z),
                new Quaternion(),
                transform);
            entityGameObjects.Add(entityGameObject);
            return entityGameObject;
        }

        #endregion

    }
}
