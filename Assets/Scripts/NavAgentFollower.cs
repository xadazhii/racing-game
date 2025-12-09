using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavAgentFollower : MonoBehaviour
{
    public Transform pathParent;

    [Header("AI 'Chaos' Settings")]
    public float pathOffset = 1.5f;

    private List<Transform> waypoints = new List<Transform>();
    private int currentWaypointIndex = 0;

    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = agent.speed * (1.0f + Random.Range(-0.1f, 0.1f));

        if (pathParent != null)
        {
            foreach (Transform child in pathParent)
            {
                waypoints.Add(child);
            }
        }

        GoToNextWaypoint();
    }

    void Update()
    {
        if (!agent.pathPending && agent.remainingDistance < 5.0f)
        {
            GoToNextWaypoint();
        }
    }

    void GoToNextWaypoint()
    {
        if (waypoints.Count == 0) return;

        Vector3 targetPosition = waypoints[currentWaypointIndex].position;
        Vector3 randomOffset = new Vector3(
            Random.Range(-pathOffset, pathOffset),
            0,
            Random.Range(-pathOffset, pathOffset)
        );

        agent.destination = targetPosition + randomOffset;

        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
    }
}