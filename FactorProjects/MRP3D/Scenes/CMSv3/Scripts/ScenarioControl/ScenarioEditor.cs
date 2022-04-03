using System;
using System.Collections.Generic;
using System.Globalization;
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
        public TMP_InputField groundSizeXInput,groundSizeYInput;
        public new void Start()
        {
            base.Start();
            InitPanel();
        }
        
        private void InitPanel()
        {
            InitGroundSizeInput();
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

        private void InitGroundSizeInput()
        {
            groundSizeXInput.text = ground.GroundSize.x.ToString(CultureInfo.CurrentCulture);
            groundSizeYInput.text = ground.GroundSize.y.ToString(CultureInfo.CurrentCulture);
        }

        public void OnAddWorkstationClicked()
        {
            InstantiateWorkstation(_scenario.model.workstations[workstationTypeDropdown.value].id,0f,0f);
        }

        public void OnAddAgvClicked()
        {
            InstantiateAgvInstance(new AgvInstance{x = 0f,y = 0f});
        }

        public void OnSetGroundSizeClicked()
        {
            ground.ChangeSize(float.Parse(groundSizeXInput.text),float.Parse(groundSizeYInput.text));
        }

        public string scenarioXmlOutputPath = "Assets/FactorProjects/MRP3D/Scenes/CMSv3/config/scenarios";
        public void OnSaveClicked()
        {
            _scenario.layout.groundSize = ground.GroundSize;

            // Save workstation instances in EntityGameObjectDict
            _scenario.layout.workstationInstances = (
                from workstationObj in EntityGameObjectsDict[typeof(Workstation)]
                let pos = workstationObj.transform.position
                let controller = workstationObj.GetComponent<WorkstationControllerBase>()
                select new WorkstationInstance {workstationRef = new Workstation {idref = controller.Workstation.id}, x = pos.x, y = pos.z}).ToArray();

            // Save AGV instances in EntityGameObjectDict
            _scenario.layout.agvInstances = EntityGameObjectsDict[typeof(Agv)]
                .Select(o => o.transform.position)
                .Select(pos => new AgvInstance {x = pos.x, y = pos.z}).ToArray();
            
            // Save import station and export station in EntityGameObjectDict
            var it = EntityGameObjectsDict[typeof(ImportStation)][0].transform.position;
            _scenario.layout.importStation = new ImportStation
            {
                x = it.x,
                y = it.z
            };
            var ot = EntityGameObjectsDict[typeof(ExportStation)][0].transform.position;
            _scenario.layout.exportStation = new ExportStation
            {
                x = ot.x,
                y = ot.z
            };

            // Serialize and output scenario to XML file
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
