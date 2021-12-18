using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkArea : MonoBehaviour
{
    public GameObject WorkPoint { get; private set; }
    public float AreaDiameter = 17f;
    
    // Start is called before the first frame update
    void Start()
    {
        WorkPoint = GetComponentInChildren<WorkPoint>().gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ResetWorkPoint()
    {
        Vector3 newPosition = new Vector3(UnityEngine.Random.Range(-7f, 7f), UnityEngine.Random.Range(-7f, 7f), 0f);
        WorkPoint.transform.position = transform.position + newPosition;
    }
}
