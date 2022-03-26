using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Multi;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class WorkstationBase : MonoBehaviour
    {
        public Workstation workstation;
        public GameObject nameText;
        public GameObject processText;

        public void Start()
        {
            RefreshText();
        }

        private void RefreshText()
        {
            nameText.GetComponent<TextMeshPro>().text = workstation.name;
            StringBuilder sb = new StringBuilder();
            foreach (var pref in workstation.supportProcesses)
            {
                Process p = SceanrioLoader.getProcess(pref.idref);
                foreach (var iref in p.inputs)
                {
                    sb.Append(SceanrioLoader.getItemState(iref.idref).name);
                    sb.Append('+');
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append("=>");
                foreach (var iref in p.outputs)
                {
                    sb.Append(SceanrioLoader.getItemState(iref.idref).name);
                    sb.Append('+');
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append('\n');
            }
            processText.GetComponent<TextMeshPro>().text = sb.ToString();
        }
    }
}

