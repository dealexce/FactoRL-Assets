using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class MRPWorkArea : MonoBehaviour
{
    public class WorkPointInfo
    {
        public Transform Transform;
        public MRPWorkPoint WorkPoint;
        public Collider2D Col;
        public float safeRadius;
    }
    public class AgentInfo
    {
        public Transform Transform;
        public MRPAgent Agent;
        public Collider2D Col;
        public float safeRadius;
    }
    public List<GameObject> WorkPoints = new List<GameObject>();
    public List<WorkPointInfo> WorkPointInfos = new List<WorkPointInfo>();
    public List<GameObject> Agents = new List<GameObject>();
    public List<AgentInfo> AgentInfos = new List<AgentInfo>();
    public float AreaDiameter = 17f;
    
    [Header("Max Environment Steps")] public int MaxEnvironmentSteps = 5000;
    private int m_ResetTimer;

    private SimpleMultiAgentGroup _simpleMultiAgentGroup;
    private int _resetTimer;
    
    // Start is called before the first frame update
    void Start()
    {
        _simpleMultiAgentGroup = new SimpleMultiAgentGroup();
        FindChildObject(transform, WorkPoints, "work_point");
        foreach (var item in WorkPoints)
        {
            WorkPointInfo workPointInfo = new WorkPointInfo();
            workPointInfo.Transform = item.transform;
            workPointInfo.WorkPoint = item.GetComponent<MRPWorkPoint>();
            workPointInfo.Col = item.GetComponent<CircleCollider2D>();
            workPointInfo.safeRadius = workPointInfo.Col.bounds.extents.magnitude;
            WorkPointInfos.Add(workPointInfo);
        }
        FindChildObject(transform, Agents, "agent");
        foreach (var item in Agents)
        {
            AgentInfo agentInfo = new AgentInfo();
            agentInfo.Transform = item.transform;
            MRPAgent agent = item.GetComponent<MRPAgent>();
            _simpleMultiAgentGroup.RegisterAgent(agent);
            agentInfo.Agent = agent;
            agentInfo.Col = item.GetComponent<BoxCollider2D>();
            agentInfo.safeRadius = agentInfo.Col.bounds.extents.magnitude;
            AgentInfos.Add(agentInfo);
        }
        
        ResetScene();
    }

    private void FixedUpdate()
    {
        _resetTimer += 1;
        if (_resetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            _simpleMultiAgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }

    private void FindChildObject(Transform parent, List<GameObject> list, String tagToFind)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (!child.CompareTag(tagToFind))
                continue;
            GameObject agent = child.gameObject;
            list.Add(agent);
            FindChildObject(child, list, tagToFind);
        }
    }

    private void ResetScene()
    {
        _resetTimer = 0;
        
        foreach (var item in WorkPointInfos)
        {
            SafeResetGameObject(item.Transform.gameObject,item.safeRadius);
        }
        foreach (var item in AgentInfos)
        {
            SafeResetGameObject(item.Transform.gameObject,item.safeRadius);
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
/// <summary>
/// Invoked when a work point is finished by an agent
/// Add group reward and reset this work point to another safe position
/// </summary>
/// <param name="workPoint"></param>
    public void FinishWorkPoint(GameObject workPoint)
    {
        _simpleMultiAgentGroup.AddGroupReward(1f);
        SafeResetGameObject(workPoint, workPoint.GetComponent<CircleCollider2D>().radius);
    }

private void SafeResetGameObject(GameObject obj, float safeRadius)
{
    var potentialPosition = Vector3.zero;
    int remainAttempts = 100;
    bool safePositionFound = false;
    while (!safePositionFound && remainAttempts>0)
    {
        potentialPosition = new Vector3(UnityEngine.Random.Range(-7f, 7f), UnityEngine.Random.Range(-7f, 7f), 0f);
        potentialPosition = transform.position + potentialPosition;
        remainAttempts--;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(potentialPosition, safeRadius);
        safePositionFound = colliders.Length == 0;
    }
    if (safePositionFound)
    {
        obj.transform.position = potentialPosition;
    }
    else
    {
        Debug.LogError("Unable to find a safe position to reset work point");
    }
}

    public void CollideWithRobot()
    {
        _simpleMultiAgentGroup.AddGroupReward(-1f);
    }

    public void CollideWithBoundary()
    {
        _simpleMultiAgentGroup.AddGroupReward(-1f);
    }
}
