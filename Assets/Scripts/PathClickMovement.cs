using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PathClickMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    // Animator Parameter Hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

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
    }

    private void Update()
    {
        if (animator == null || agent == null) return;

        // 1. Calculate normalized speed (0 = Idle, 1 = Moving)
        float currentSpeed = agent.velocity.magnitude / Mathf.Max(0.1f, agent.speed);
        animator.SetFloat(SpeedHash, currentSpeed);

        // 2. Use agent.isOnNavMesh instead of agent.isGrounded
        bool grounded = agent.isOnNavMesh;
        animator.SetBool(IsGroundedHash, grounded);
    }
}