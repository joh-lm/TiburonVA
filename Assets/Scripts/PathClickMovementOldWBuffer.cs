// using UnityEngine;
// using UnityEngine.AI;

// [RequireComponent(typeof(NavMeshAgent))]
// [RequireComponent(typeof(Animator))]
// public class PathClickMovement : MonoBehaviour
// {
//     private NavMeshAgent agent;
//     private Animator animator;

//     private static readonly int SpeedHash = Animator.StringToHash("Speed");
//     private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

//     public bool IsMoving => agent != null && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;

//     private void Awake()
//     {
//         agent = GetComponent<NavMeshAgent>();
//         animator = GetComponent<Animator>();

//         agent.updatePosition = true;
//         agent.updateRotation = true;
        
//         // Prevent agent from trying to get closer than its own radius
//         agent.stoppingDistance = 0.2f; 
//     }

//     private void Update()
//     {
//         bool isGrounded = agent.isOnNavMesh;
//         animator.SetBool(IsGroundedHash, isGrounded);

//         UpdateAnimator();
//     }

//     private void UpdateAnimator()
//     {
//         float currentNormalizedSpeed = agent.velocity.magnitude / agent.speed;
//         animator.SetFloat(SpeedHash, currentNormalizedSpeed, 0.1f, Time.deltaTime);
//     }

//     // Move to a specific target position with a clamped radius
//     public void MoveToLocation(Vector3 destination, float platformRadius = 0.5f)
//     {
//         Vector3 offsetDestination = GetOffsetDestination(destination, platformRadius);
//         agent.SetDestination(offsetDestination);
//     }

//     private Vector3 GetOffsetDestination(Vector3 centerPoint, float maxRadius)
//     {
//         // Clamp the offset circle so it stays strictly within the platform top
//         Vector2 randomCircle = Random.insideUnitCircle * maxRadius;
//         Vector3 offsetPoint = centerPoint + new Vector3(randomCircle.x, 0f, randomCircle.y);

//         if (NavMesh.SamplePosition(offsetPoint, out NavMeshHit navHit, maxRadius + 0.5f, NavMesh.AllAreas))
//         {
//             return navHit.position;
//         }

//         return centerPoint;
//     }
// }