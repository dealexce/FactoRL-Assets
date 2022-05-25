using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class Ground : MonoBehaviour
    {
        public enum GroundSwitchColor
        {
            Green,Red,Yellow
        }

        public GroundSize GroundSize;
        public GameObject ground;
        public GameObject northWall, southWall, westWall, eastWall;

        [HideInInspector]
        public MeshFilter groundMF;
        [HideInInspector]
        public MeshFilter wallMF;
        [HideInInspector]
        public float groundMFx, groundMFz, wallMFx, wallMFz;
        
        //for visualization
        public Material _successMaterial;
        public Material _failedMaterial;
        public Material _stockSuccessMaterial;
        private Material _originMaterial;
        private MeshRenderer _meshRenderer;


        private void Awake()
        {
            _meshRenderer = ground.GetComponent<MeshRenderer>();
            _originMaterial = _meshRenderer.material;
            groundMF = ground.GetComponent<MeshFilter>();
            groundMFx = groundMF.mesh.bounds.size.x;
            groundMFz = groundMF.mesh.bounds.size.z;
            wallMF = northWall.GetComponent<MeshFilter>();
            wallMFx = wallMF.mesh.bounds.size.x;
            wallMFz = wallMF.mesh.bounds.size.z;
        }

        private void Start()
        {

        }

        public void ChangeSize(float x, float z)
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
            GroundSize = new GroundSize {x = x, y = z};
        }



        //visualization indicate product finished
        public void FlipColor(GroundSwitchColor color)
        {
            switch (color)
            {
                case GroundSwitchColor.Green:
                    StartCoroutine(ProductReceivedSwapMaterial(2f,_successMaterial));
                    break;
                case GroundSwitchColor.Red:
                    StartCoroutine(ProductReceivedSwapMaterial(5f, _failedMaterial));
                    break;
                case GroundSwitchColor.Yellow:
                    StartCoroutine(ProductReceivedSwapMaterial(2f, _stockSuccessMaterial));
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
