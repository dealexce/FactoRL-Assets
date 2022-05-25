using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using FactorProjects.MRP3D.Scenes.CMSv3.Scripts.Visualize;
using OD;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class ScenarioGenerator : MonoBehaviour
    {
        public GlobalSetting globalSetting;
        
        //TODO: Show only when randomizeLayout is set to true
        public float minGroundX = 40f, maxGroundX = 60f;
        public float minGroundY = 40f, maxGroundY = 60f;
        
        protected Scenario _scenario;

        public Ground ground;

        public GameObject workstationPrefab;
        public string machinePrefabPath = "Machines";
        public bool showMachine = false;

        public GameObject agvPrefab;

        public GameObject importStationPrefab;
        public GameObject exportStationPrefab;

        public OrderedDictionary<Type, List<GameObject>> EntityGameObjectsDict = new OrderedDictionary<Type, List<GameObject>>()
        {
            {typeof(Agv), new List<GameObject>()},
            {typeof(Workstation), new List<GameObject>()},
            {typeof(ImportStation), new List<GameObject>()},
            {typeof(ExportStation), new List<GameObject>()}
        };


        public void Start()
        {
            ScenarioLoader.Load(globalSetting.scenarioXmlPath);
            _scenario = ScenarioLoader.getScenario();
            Assert.IsNotNull(_scenario);
            InitLayout();
            if (globalSetting.randomizeLayout)
            {
                RandomizeLayout();
            }
        }

        protected void InitLayout()
        {
            //change ground size
            ground.ChangeSize(_scenario.layout.groundSize.x, _scenario.layout.groundSize.y);

            //instantiate workstation instances according to layout
            WorkstationUtil.Init(_scenario.model.workstations, machinePrefabPath);
            foreach (var wsi in _scenario.layout.workstationInstances)
            {
                InstantiateWorkstation(wsi);
            }
            //instantiate import station and export station according to layout
            InstantiateImportStation();
            InstantiateExportStation();

            //instantiate agv instances according to layout
            foreach (var agv in _scenario.layout.agvInstances)
            {
                InstantiateAgvInstance(agv);
            }

        }


        #region Randomization

        protected void RandomizeLayout()
        {
            float randomX = Random.Range(minGroundX, maxGroundX);
            float randomY = Random.Range(minGroundY, maxGroundY);
            ground.ChangeSize(randomX, randomY);

            // Deactivate objects before reset position for better position randomization
            // TODO: Check whether this will cause problems
            foreach (var list in EntityGameObjectsDict.Values)
            {
                foreach (var obj in list)
                {
                    obj.SetActive(false);
                }
            }
            foreach (var list in EntityGameObjectsDict.Values)
            {
                foreach (var obj in list)
                {
                    obj.SetActive(true);
                    ResetToSafeRandomPosition(obj);
                }
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
            var g = InstantiateEntityOnGround(typeof(Agv), agvPrefab, agv.x, agv.y);
            var agvController = g.GetComponent<AgvControllerBase>();
            agvController.Init(_scenario.model.agv);
        }

        protected void InstantiateWorkstation(WorkstationInstance wsi)
        {
            InstantiateWorkstation(wsi.workstationRef.idref,wsi.x,wsi.y);
        }

        protected void InstantiateWorkstation(string id, float x, float y)
        {
            GameObject g = InstantiateEntityOnGround(typeof(Workstation), workstationPrefab, x, y);
            if (showMachine)
            {
                Instantiate(WorkstationUtil.GetMachinePrefab(id), g.transform);
            }
            var controller = g.GetComponent<WorkstationControllerBase>();
            controller.Init(ScenarioLoader.getWorkstation(id));
        }
        
        private void InstantiateImportStation()
        {
            ImportStation importStation = _scenario.layout.importStation;
            var g = InstantiateEntityOnGround(typeof(ImportStation), importStationPrefab, importStation.x, importStation.y);
            g.GetComponent<ImportController>()?.Init(_scenario.layout.importStation);
        }
        private void InstantiateExportStation()
        {
            ExportStation exportStation = _scenario.layout.exportStation;
            var g = InstantiateEntityOnGround(typeof(ExportStation), exportStationPrefab, exportStation.x, exportStation.y);
            g.GetComponent<ExportController>()?.Init(exportStation);
        }

        protected virtual GameObject InstantiateEntityOnGround(Type type, GameObject prefab, float x, float z)
        {
            GameObject entityGameObject = Instantiate(
                prefab, 
                ground.transform.position+new Vector3(x, Utils.GetEncapsulateBoxColliderBounds(prefab).extents.y+0.3f, z),
                new Quaternion(),
                transform);
            EntityGameObjectsDict[type].Add(entityGameObject);
            return entityGameObject;
        }

        #endregion

    }
}
