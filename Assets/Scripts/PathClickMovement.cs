using UnityEngine;
using UnityEngine.AI;
using System;

[RequireComponent(typeof(NavMeshAgent))]
public class PathClickMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    private bool wasMoving = false;
    public event Action OnDestinationReached;

    public bool IsMoving => agent != null && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    public void MoveToLocation(Vector3 destination, float surfaceRadius)
    {
        agent.stoppingDistance = surfaceRadius;
        agent.SetDestination(destination);
        wasMoving = true;
    }

    private void Update()
    {
        if (animator == null || agent == null) return;

        float currentSpeed = agent.velocity.magnitude / Mathf.Max(0.1f, agent.speed);
        animator.SetFloat(SpeedHash, currentSpeed);

        bool grounded = agent.isOnNavMesh;
        animator.SetBool(IsGroundedHash, grounded);

        // Detect arrival
        if (wasMoving && !IsMoving)
        {
            wasMoving = false;
            OnDestinationReached?.Invoke();
        }
    }
}