using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;

public class RobotController : Agent
{
    private Rigidbody _rigidbody;
    [FormerlySerializedAs("_area")]
    public GameObject _plane;

    public GameObject _bracket;
    

    private PlaneController _planeController;
    public bool trainingMode = true;
    public float moveSpeed = 1;
    public float maxSpeed = 1;
    public float rotateSpeed = 1;
    public int capacity = 3;

    private List<GameObject> carriages = new List<GameObject>();
    
    // Start is called before the first frame update
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _planeController = _plane.GetComponent<PlaneController>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // float hori = Input.GetAxis("Horizontal");
        // float vert = Input.GetAxis("Vertical");
        //
        // Vector3 movement = transform.forward * vert;
        // Vector3 rotation = transform.up * hori;
        // //transform.position = transform.position + movement*moveSpeed;
        // transform.Translate(movement * Time.deltaTime * moveSpeed, Space.Self);
        // transform.Rotate(rotation*rotateSpeed);
        
        int count = 1;
        foreach (var carriage in carriages)
        {
            carriage.transform.position = transform.position + Vector3.up*.5f*count++;
        }
    }
    
    /// <summary>
    /// This override method handles the action decisions made by neural network
    /// or gameplay actions and use the actions to operate in the game
    /// action.DiscreteActions[i] is 1 or -1
    /// Index   Meaning (1, -1)
    /// 0       move forthright (forward, backward)
    /// 1       rotate (right, left)
    /// </summary>
    /// <param name="actions"></param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        ActionSegment<float> act = actions.ContinuousActions;
        Vector3 movement = transform.forward * act[0];
        Vector3 rotation = transform.up * act[1];
        _rigidbody.AddForce(movement * moveSpeed * (1-_rigidbody.velocity.magnitude/maxSpeed), ForceMode.VelocityChange);
        transform.Rotate(rotation*rotateSpeed);
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(carriages.Count);
        sensor.AddObservation(carriages.Count<capacity);
        sensor.AddObservation(_bracket.transform.position-transform.position);
    }
    
    public override void OnEpisodeBegin()
    {
        resetRobot();
        _planeController.resetPlane();
    }

    private void resetRobot()
    {
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        carriages.Clear();
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousAction = actionsOut.ContinuousActions;
        continuousAction[0] = Input.GetAxis("Vertical");
        continuousAction[1] = Input.GetAxis("Horizontal");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (carriages.Count < capacity && other.CompareTag("package"))
        {
            carriages.Add(other.gameObject);
        }
        if (other.CompareTag("bracket"))
        {
            foreach (var carriage in carriages)
            {
                _planeController.returnCarriage(carriage);
                AddReward(1f);
                if (trainingMode)
                {
                    Debug.Log("Delivered!");
                }
            }
            carriages.Clear();
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.collider.CompareTag("wall")||other.collider.CompareTag("bracket"))
        {
            AddReward(-1f);
        }
    }

}
