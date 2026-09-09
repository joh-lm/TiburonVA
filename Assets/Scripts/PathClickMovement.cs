using UnityEngine;
using UnityEngine.AI;
using System;

[RequireComponent(typeof(NavMeshAgent))]
public class PathClickMovement : MonoBehaviour
{
    [Header("Visual Models")]
    [SerializeField] private GameObject characterModel;
    [SerializeField] private GameObject boatModel;

    private NavMeshAgent agent;
    private Animator animator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    private bool wasMoving = false;
    private bool isInBoatMode = false;

    public event Action OnDestinationReached;

    public bool IsMoving => agent != null && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (boatModel != null) boatModel.SetActive(false);
        if (characterModel != null) characterModel.SetActive(true);
    }

    private void Start()
    {
        PlayerInventory inventory = GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            UpdateWaterTraversalPermission(inventory.HasBoat);
        }
    }

    public void MoveToLocation(Vector3 destination, float surfaceRadius)
    {
        agent.stoppingDistance = surfaceRadius;
        agent.SetDestination(destination);
        wasMoving = true;
    }

    /// <summary>
    /// Enables or disables the Water area mask bit on the NavMeshAgent based on boat ownership.
    /// </summary>
    public void UpdateWaterTraversalPermission(bool canUseWater)
    {
        if (agent == null) return;

        int waterAreaIndex = NavMesh.GetAreaFromName("Water");
        if (waterAreaIndex < 0) return;

        int waterMask = 1 << waterAreaIndex;

        if (canUseWater)
        {
            agent.areaMask |= waterMask;  // Enable Water area traversal
        }
        else
        {
            agent.areaMask &= ~waterMask; // Disable Water area traversal
        }
    }

    private void Update()
    {
        if (agent == null) return;

        // 1. Model Swap based on NavMeshLink status
        HandleLinkModelSwap();

        // 2. Drive Animator parameters
        if (animator != null && characterModel != null && characterModel.activeSelf)
        {
            float currentSpeed = agent.velocity.magnitude / Mathf.Max(0.1f, agent.speed);
            animator.SetFloat(SpeedHash, currentSpeed);

            bool grounded = agent.isOnNavMesh;
            animator.SetBool(IsGroundedHash, grounded);
        }

        // 3. Detect arrival
        if (wasMoving && !IsMoving)
        {
            wasMoving = false;
            OnDestinationReached?.Invoke();
        }
    }

    private void HandleLinkModelSwap()
    {
        if (agent == null) return;

        // 1. Check if the agent is actively crossing a NavMeshLink
        bool onLink = agent.isOnOffMeshLink;

        // 2. Check if the agent is standing directly on a "Water" NavMesh surface
        bool onWaterArea = IsStandingOnWaterArea();

        // Unit should be in boat mode if either condition is true
        bool shouldBeInBoat = onLink || onWaterArea;

        if (shouldBeInBoat != isInBoatMode)
        {
            isInBoatMode = shouldBeInBoat;
            SetModelVisualState(isInBoatMode);
        }
    }

    private bool IsStandingOnWaterArea()
    {
        if (!agent.isOnNavMesh) return false;

        // Sample the NavMesh area directly beneath the agent
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 0.8f, NavMesh.AllAreas))
        {
            int waterAreaIndex = NavMesh.GetAreaFromName("Water");
            if (waterAreaIndex >= 0)
            {
                int waterMask = 1 << waterAreaIndex;
                return (hit.mask & waterMask) != 0;
            }
        }

        return false;
    }
    
    private void SetModelVisualState(bool useBoat)
    {
        if (characterModel != null) characterModel.SetActive(!useBoat);
        if (boatModel != null) boatModel.SetActive(useBoat);
    }
}