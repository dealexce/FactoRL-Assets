using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class ScenarioEditor : ScenarioGenerator
    {
        public TMP_Dropdown workstationTypeDropdown;
        new void Start()
        {
            base.Start();
            InitPanel();
        }
        
        private void InitPanel()
        {
            InitWorkstationTypeDropdown();
        }

        private void InitWorkstationTypeDropdown()
        {
            workstationTypeDropdown.options.Clear();
            workstationTypeDropdown.AddOptions(
                _scenario.model.workstations.Select(workstation => new TMP_Dropdown.OptionData{text = workstation.name}).ToList());
            //workstationTypeDropdown.captionText.text = workstationTypeDropdown.options[0].text;
            workstationTypeDropdown.RefreshShownValue();
        }

        public void OnAddWorkstationClicked()
        {
            InstantiateWorkstation(_scenario.model.workstations[workstationTypeDropdown.value].id,0f,0f);
        }

        public string scenarioXmlOutputPath = "Assets/FactorProjects/MRP3D/Scenes/CMSv3/config/scenarios";
        public void OnSaveClicked()
        {
            _scenario.layout.groundSize.x = Ground.x;
            _scenario.layout.groundSize.y = Ground.z;

            _scenario.layout.workstationInstances = (
                from workstationController in GetComponentsInChildren<WorkstationController>() 
                let pos = workstationController.transform.position 
                select new WorkstationInstance {workstationRef = new Workstation {idref = workstationController.workstation.id}, x = pos.x, y = pos.z}).ToArray();

            _scenario.layout.agvInstances = GetComponentsInChildren<AgvController>()
                .Select(agvController => agvController.transform.position)
                .Select(pos => new AgvInstance() {x = pos.x, y = pos.z}).ToArray();
            
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

            // Save scenario to XML file
            var serializer = new XmlSerializer(typeof(Scenario));
            var path = scenarioXmlOutputPath
                          + "/Scenario"
                          + DateTime.Now.ToString("yyMMddHHmmss")
                          + ".xml";
            using var writer = new StreamWriter(path);
            serializer.Serialize(writer,_scenario);
            Debug.Log("Successfully saved scenario at "+path);
        }
    }
}
