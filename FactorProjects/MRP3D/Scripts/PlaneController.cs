using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PlaneController : MonoBehaviour
{

    public int maxPackage = 5;
    [FormerlySerializedAs("initPacckage")]
    public int initPackage = 5;
    public GameObject package_prefab;
    public float generateCD = 2f;

    private List<GameObject> allPackages = new List<GameObject>();
    private List<GameObject> todoPackages = new List<GameObject>(); //Packages returned and wait for put back
    private float cd = 0f;

    private int activePackage = 0;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < initPackage; i++)
        {
            GameObject package = GameObject.Instantiate(package_prefab);
            allPackages.Add(package);
            safeResetPackage(package);
        }
        activePackage = initPackage;
    }

    // Update is called once per frame
    void Update()
    {
        if (activePackage < maxPackage)
        {
            cd += Time.deltaTime;
            if (cd > generateCD)
            {
                todoPackages[0].SetActive(true);
                safeResetPackage(todoPackages[0]);
                todoPackages.RemoveAt(0);
                activePackage += 1;
                cd = 0f;
            }
        }
        else
        {
            cd = 0f;
        }
    }

    public void returnCarriage(GameObject carriage)
    {
        carriage.SetActive(false);
        todoPackages.Add(carriage);
        activePackage -= 1;
    }

    public void resetPlane()
    {
        foreach (var package in allPackages)
        {
            safeResetPackage(package);
        }
        todoPackages.Clear();
        cd = 0f;
        activePackage = initPackage;
    }
    
    private void safeResetPackage(GameObject package)
    {
        package.SetActive(true);
        var potentialPosition = Vector3.zero;
        int remainAttempts = 100;
        bool safePositionFound = false;
        while (!safePositionFound && remainAttempts>0)
        {
            Vector3 carriageSize = package.GetComponent<BoxCollider>().size;
            potentialPosition = new Vector3(UnityEngine.Random.Range(-8f, 8f), carriageSize[1], UnityEngine.Random.Range(-8f, 8f));
            potentialPosition = transform.position + potentialPosition;
            remainAttempts--;
            Collider[] colliders = Physics.OverlapBox(potentialPosition, carriageSize/2f);
            safePositionFound = colliders.Length == 0;
        }
        if (safePositionFound)
        {
            package.transform.position = potentialPosition;
        }
        else
        {
            Debug.LogError("Unable to find a safe position to reset work point");
        }
    }
}
