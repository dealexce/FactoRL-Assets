using System;
using System.Text;
using TMPro;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class WorkstationControllerBase : MonoBehaviour, IManualInit<Workstation>
    {
        public Workstation Workstation { get; private set; }

        public virtual void Init(Workstation model)
        {
            this.Workstation = model;
            RefreshText();
        }

        public TextMeshPro nameTextMesh;
        private string workstationName;
        public TextMeshPro processTextMesh;
        private string processInfo;
        /// <summary>
        /// Parse and show workstation name and support process information on the game object.
        /// </summary>
        private void RefreshText()
        {
            nameTextMesh.text = "";
            processTextMesh.text = "";
            workstationName = Workstation.name;
            StringBuilder sb = new StringBuilder();
            foreach (var pref in Workstation.supportProcessesRef)
            {
                Process p = ScenarioLoader.getProcess(pref.idref);
                sb.Append($"[{p.duration}s]");
                foreach (var iRef in p.inputItemsRef)
                {
                    sb.Append(ScenarioLoader.getItemState(iRef.idref).name);
                    sb.Append('+');
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append("->");
                foreach (var iRef in p.outputItemsRef)
                {
                    sb.Append(ScenarioLoader.getItemState(iRef.idref).name);
                    sb.Append('+');
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append('\n');
            }
            processInfo = sb.ToString();
        }

        private void OnMouseEnter()
        {
            nameTextMesh.text = workstationName;
            processTextMesh.text = processInfo;
        }

        private void OnMouseExit()
        {
            nameTextMesh.text = "";
            processTextMesh.text = "";
        }
    }
}
