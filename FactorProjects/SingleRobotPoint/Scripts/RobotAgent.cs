using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;

public class RobotAgent : Agent
{
    public float moveSpeed = 0.1f;
    public float moveForce = 10f;
    private Rigidbody2D rigidbody;
    private int lastCollisionStep = 0;

    public bool trainingMode;
    [FormerlySerializedAs("stayTimeInWorkPoint")]
    public float RequireStayTime = 2f;
    
    private float stayTime = 0f;

    private WorkArea workArea;

    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        workArea = GetComponentInParent<WorkArea>();
    }

    private void FixedUpdate()
    {
        
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
        Vector2 move = new Vector2(0f, act[0]);
        Quaternion rotation = Quaternion.Euler(0f, 0f, act[1]);
        transform.position = transform.position + transform.rotation*move*moveSpeed;
        transform.rotation = transform.rotation * rotation;
    }

    public override void OnEpisodeBegin()
    {
        rigidbody.velocity = Vector2.zero;
        rigidbody.angularVelocity = 0;
        MoveToRandomPosition();
        workArea.ResetWorkPoint();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localRotation.eulerAngles.normalized.z);
        Vector2 toWorkPoint = new Vector2();
        if (workArea.WorkPoint != null)
        {
            toWorkPoint = workArea.WorkPoint.transform.position - transform.position;
        }
        sensor.AddObservation(toWorkPoint.normalized);
        sensor.AddObservation(toWorkPoint.magnitude/workArea.AreaDiameter);
    }

    private void MoveToRandomPosition()
    {
        transform.position = workArea.transform.position + new Vector3(UnityEngine.Random.Range(-7f, 7f), UnityEngine.Random.Range(-7f, 7f),0f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousAction = actionsOut.ContinuousActions;
        continuousAction[0] = Input.GetAxis("Vertical");
        continuousAction[1] = -Input.GetAxis("Horizontal");
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        CollisionReward(other);
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        CollisionReward(other);
    }

    private void CollisionReward(Collision2D other)
    {
        if (trainingMode && other.collider.CompareTag("boundary"))
        {
            AddReward(-1f);
            //Debug.Log("[Train]Collide with" + other.collider.name);
        }else if(!trainingMode)
        {
            Debug.Log("Collide with" + other.collider.name);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("work_point"))
        {
            stayTime = 0f;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("work_point"))
        {
            stayTime = 0f;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("work_point"))
        {
            StayInPoint(Time.deltaTime);
        }
    }

    private void StayInPoint(float deltaStayTime)
    {
        stayTime += deltaStayTime;
        if (this.stayTime > RequireStayTime)
        {
            this.stayTime = 0;
            AddReward(1f);
            workArea.ResetWorkPoint();
            if (trainingMode)
            {
                //Debug.Log("[Train]StayInPoint");
            }
            else
            {
                Debug.Log("StayInPoint");
            }
        }
    }
    
    
    


}
