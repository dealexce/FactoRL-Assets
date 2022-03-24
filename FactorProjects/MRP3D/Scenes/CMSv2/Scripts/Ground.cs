using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class Ground : MonoBehaviour
    {
        public enum GroundSwitchColor
        {
            Green,Red
        }
        public float x, z;
        public GameObject ground;
        public GameObject northWall, southWall, westWall, eastWall;
        public MeshFilter groundMF, wallMF;
        public float groundMFx, groundMFz, wallMFx, wallMFz;
        
        //for visualization
        public Material _successMaterial;
        public Material _failedMaterial;
        private Material _originMaterial;
        private MeshRenderer _meshRenderer;

        public GameObject FloatingTextObject;
        private TextMeshPro _textMeshPro;

        private void Awake()
        {
            _meshRenderer = ground.GetComponent<MeshRenderer>();
            _originMaterial = _meshRenderer.material;
            _textMeshPro = FloatingTextObject.GetComponent<TextMeshPro>();
        }

        private void Start()
        {
            groundMF = ground.GetComponent<MeshFilter>();
            groundMFx = groundMF.mesh.bounds.size.x;
            groundMFz = groundMF.mesh.bounds.size.z;
            wallMF = northWall.GetComponent<MeshFilter>();
            wallMFx = wallMF.mesh.bounds.size.x;
            wallMFz = wallMF.mesh.bounds.size.z;
            changeSize(x,z);
        }

        public void changeSize(float x, float z)
        {
            Vector3 newScale = new Vector3(x / groundMFx, 1, z / groundMFz);
            ground.transform.localScale = newScale;
            westWall.transform.localPosition = new Vector3(-x / 2f - wallMFx / 2f, .4f, 0f);
            westWall.transform.localScale = new Vector3(1f, 1f, z / wallMFz+2*wallMFz);
            eastWall.transform.localPosition = new Vector3(x / 2f + wallMFx/2f, .4f, 0f);
            eastWall.transform.localScale = new Vector3(1f, 1f, z / wallMFz+2*wallMFz);
            northWall.transform.localPosition = new Vector3(0f, .4f, z / 2f+wallMFz/2f);
            northWall.transform.localScale = new Vector3(x / wallMFx, 1f, 1f);
            southWall.transform.localPosition = new Vector3(0f, .4f, -z / 2f-wallMFz/2f);
            southWall.transform.localScale = new Vector3(x / wallMFx, 1f, 1f);
        }

        public void changeText(string text)
        {
            _textMeshPro.text = text;
        }

        //visualization indicate product finished
        public void FlipColor(GroundSwitchColor color)
        {
            switch (color)
            {
                case GroundSwitchColor.Green:
                    StartCoroutine(ProductReceivedSwapMaterial(1f,_successMaterial));
                    break;
                case GroundSwitchColor.Red:
                    StartCoroutine(ProductReceivedSwapMaterial(1f, _failedMaterial));
                    break;
            }
        }
        /// <summary>
        /// Swap ground material, wait time seconds, then swap back to the regular material.
        /// </summary>
        IEnumerator ProductReceivedSwapMaterial(float time,Material material)
        {
            _meshRenderer.material = material;
            yield return new WaitForSeconds(time);
            _meshRenderer.material = _originMaterial;
        }
    }
}
