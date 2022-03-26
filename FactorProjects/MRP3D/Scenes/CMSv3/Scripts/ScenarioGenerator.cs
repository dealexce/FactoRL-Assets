using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using FactorProjects.MRP3D.Scenes.CMSv3.Scripts.Visualize;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class ScenarioGenerator : MonoBehaviour
    {
        private static string ModelConfigPath = "Assets/FactorProjects/MRP3D/Scenes/CMSv3/config/scenarios/Scenario220327032734.xml";
        private static Scenario _scenario=null;
        static ScenarioGenerator()
        {
            SceanrioLoader.Load(ModelConfigPath);
            _scenario = SceanrioLoader.getScenario();
        }

        private Ground _ground;

        public GameObject workstationPrefab;
        public string machinePrefabPath = "Machines";
        public bool showMachine = false;

        public GameObject agvPrefab;

        public GameObject importStationPrefab;
        public GameObject exportStationPrefab;

        public GameObject uiCanvas;

        private void Awake()
        {
            
        }

        private void Start()
        {
            _ground = GetComponentInChildren<Ground>();
            Assert.IsNotNull(_scenario);
            InitPanel();
            InitLayout();
        }

        private void InitPanel()
        {
            InitDropdown();
        }

        private void InitLayout()
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
            InstantiateOnGround(importStationPrefab, importStation.x, importStation.y);
            ExportStation exportStation = _scenario.layout.exportStation;
            InstantiateOnGround(exportStationPrefab, exportStation.x, exportStation.y);
        }

        private void InstantiateAgvInstance(AgvInstance agv)
        {

            var g = InstantiateOnGround(agvPrefab, agv.x, agv.y);
            AgvBase agvBase = g.GetComponent<AgvBase>();
            agvBase.agvConfig = _scenario.model.agv;
        }

        private void InstantiateWorkstation(WorkstationInstance wsi)
        {
            InstantiateWorkstation(wsi.workstationModel.idref,wsi.x,wsi.y);
        }

        private void InstantiateWorkstation(string id, float x, float y)
        {
            GameObject g = InstantiateOnGround(workstationPrefab, x, y);
            if (showMachine)
            {
                Instantiate(WorkstationUtil.GetMachinePrefab(id), g.transform);
            }
            MFWSController controller = g.GetComponent<MFWSController>();
            controller.workstation = SceanrioLoader.getWorkstation(id);
        }

        public GameObject InstantiateOnGround(GameObject prefab, float x, float z)
        {
            return Instantiate(
                prefab, 
                transform.position+new Vector3(x, Utils.GetEncapsulateBoxColliderBounds(prefab).extents.y, z),
                new Quaternion(),
                transform);
        }
        
        private TMP_Dropdown _dropdown;
        private Workstation[] _workstationsForDropDown;

        private void InitDropdown()
        {
            _dropdown = uiCanvas.GetComponentInChildren<TMP_Dropdown>();
            _workstationsForDropDown = _scenario.model.workstations;
            _dropdown.options.Clear();
            TMP_Dropdown.OptionData tempData;
            foreach (var ws in _workstationsForDropDown)
            {
                tempData = new TMP_Dropdown.OptionData();
                tempData.text = ws.name;
                _dropdown.options.Add(tempData);
            }
            _dropdown.captionText.text = _workstationsForDropDown[0].name;
        }

        public void OnAddWorkstationClicked()
        {
            InstantiateWorkstation(_workstationsForDropDown[_dropdown.value].id,0f,0f);
        }


        public string scenarioXmlOutputPath = "Assets/FactorProjects/MRP3D/Scenes/CMSv3/config/scenarios";
        public void OnSaveClicked()
        {
            List<WorkstationInstance> wsiList = new List<WorkstationInstance>();
            foreach (var wsc in GetComponentsInChildren<MFWSController>())
            {
                Workstation w = new Workstation();
                w.idref = wsc.workstation.id;
                wsiList.Add(new WorkstationInstance()
                {
                    workstationModel = w,
                    x=wsc.transform.position.x,
                    y=wsc.transform.position.z
                });
            }
            _scenario.layout.workstationInstances = wsiList.ToArray();

            List<AgvInstance> agviList = new List<AgvInstance>();
            foreach (var agvc in GetComponentsInChildren<AgvBase>())
            {
                agviList.Add(new AgvInstance()
                {
                    x=agvc.transform.position.x,
                    y=agvc.transform.position.z
                });
            }
            _scenario.layout.agvInstances = agviList.ToArray();
            var it = GetComponentInChildren<ImportControllerBase>().transform.position;
            _scenario.layout.importStation = new ImportStation()
            {
                x = it.x,
                y = it.z
            };
            var ot = GetComponentInChildren<ExportControllerBase>().transform.position;
            _scenario.layout.exportStation = new ExportStation()
            {
                x = ot.x,
                y = ot.z
            };

            XmlSerializer serializer = new XmlSerializer(typeof(Scenario));
            String path = scenarioXmlOutputPath
                          + "/Scenario"
                          + DateTime.Now.ToString("yyMMddHHmmss")
                          + ".xml";
            using (StreamWriter writer = new StreamWriter(path))
            {
                serializer.Serialize(writer,_scenario);
                Debug.Log("Successfully saved scenario at "+path);
            }
        }
    }
}
