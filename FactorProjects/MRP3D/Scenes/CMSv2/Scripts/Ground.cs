using System;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public class Ground : MonoBehaviour
    {
        public float x, z;
        public GameObject ground;
        public GameObject northWall, southWall, westWall, eastWall;
        public MeshFilter groundMF, wallMF;
        public float groundMFx, groundMFz, wallMFx, wallMFz;

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
    }
}
