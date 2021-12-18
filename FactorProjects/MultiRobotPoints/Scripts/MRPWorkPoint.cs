using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MRPWorkPoint : MonoBehaviour
{
    public float safeRadius { get; private set; }
    // Start is called before the first frame update
    void Start()
    {
        safeRadius = GetComponent<CircleCollider2D>().radius;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
