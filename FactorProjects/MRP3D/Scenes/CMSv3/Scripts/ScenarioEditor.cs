using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class ScenarioEditor : ScenarioGenerator
    {
        public GameObject uiCanvas;
        public void Start()
        {
            base.Start();
            InitPanel();
        }
        
        private void InitPanel()
        {
            InitDropdown();
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
            //TODO: Save Ground Size
            
            List<WorkstationInstance> wsiList = new List<WorkstationInstance>();
            foreach (var wsc in GetComponentsInChildren<WorkstationController>())
            {
                Workstation w = new Workstation();
                w.idref = wsc.workstation.id;
                wsiList.Add(new WorkstationInstance()
                {
                    workstationRef = w,
                    x=wsc.transform.position.x,
                    y=wsc.transform.position.z
                });
            }
            _scenario.layout.workstationInstances = wsiList.ToArray();

            List<AgvInstance> agviList = new List<AgvInstance>();
            foreach (var agvc in GetComponentsInChildren<AgvController>())
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
