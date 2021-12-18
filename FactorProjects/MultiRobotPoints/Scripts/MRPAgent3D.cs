using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

public class MRPAgent3D : Agent
{
    private Rigidbody rigidbody;
    public MRPAgentSettings _agentSettings; 
    public float safeRadius { get; private set; }
    

    public bool trainingMode;
    [FormerlySerializedAs("stayTimeInWorkPoint")]
    public float RequireStayTime = 2f;
    
    private float stayTime = 0f;

    private MRPWorkArea workArea;

    public override void Initialize()
    {

    }

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        workArea = GetComponentInParent<MRPWorkArea>();
        safeRadius = GetComponent<BoxCollider>().bounds.extents.magnitude;
        Debug.Log(rigidbody);
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
        MoveAgent(actions.DiscreteActions);
    }

    private void MoveAgent(ActionSegment<int> act)
    {
        var moveAct = act[0];
        var moveDir = Vector3.zero;
        switch (moveAct)
        {
            case 1:
                moveDir = transform.up * 1f;
                break;
            case 2:
                moveDir = transform.up * -1f;
                break;
            case 3:
                break;
        }
        transform.position = transform.position + moveDir*_agentSettings.moveSpeed;
        
        var rotateAct = act[1];
        var rotateDir = Vector3.zero;
        switch (rotateAct)
        {
            case 1:
                rotateDir = transform.forward * 1f;
                break;
            case 2:
                rotateDir = transform.forward * -1f;
                break;
            case 3:
                break;
        }
        transform.Rotate(rotateDir*_agentSettings.rotateSpeed);
    }

    public override void OnEpisodeBegin()
    {
        AgentReset();
    }

    public void AgentReset()
    {
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteAction = actionsOut.DiscreteActions;
        int move = 3;
        if (Input.GetKey(KeyCode.W))
        {
            move = 1;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            move = 2;
        }
        int rot = 3;
        if (Input.GetKey(KeyCode.A))
        {
            rot = 1;
        }else if (Input.GetKey(KeyCode.D))
        {
            rot = 2;
        }
        discreteAction[0] = move;
        discreteAction[1] = rot;
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
        if (trainingMode)
        {
            if(other.collider.CompareTag("boundary"))
            {
                workArea.CollideWithBoundary();
                
            }else if (other.collider.CompareTag("agent"))
            {
                workArea.CollideWithRobot();
            }
            //Debug.Log("[Train]Collide with" + other.collider.name);
        }else
        {
            //Debug.Log("Collide with" + other.collider.name);
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
            StayInPoint(other.gameObject,Time.deltaTime);
        }
    }

    private void StayInPoint(GameObject workPoint, float deltaStayTime)
    {
        stayTime += deltaStayTime;
        if (stayTime > _agentSettings.RequiredStayTime)
        {
            stayTime = 0;
            workArea.FinishWorkPoint(workPoint);
            if (trainingMode)
            {
                //Debug.Log("[Train]Finished a work point");
            }
            else
            {
                Debug.Log("Finished a work point");
            }
        }
    }
    
    
    


}
