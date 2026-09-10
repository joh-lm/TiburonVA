using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PathClickMovement : MonoBehaviour
{
    [Header("Visual Models")]
    [Tooltip("The standard character mesh GameObject.")]
    [SerializeField] private GameObject characterModel;

    [Tooltip("The boat mesh GameObject.")]
    [SerializeField] private GameObject boatModel;

    private NavMeshAgent agent;
    private Animator animator;
    private PlayerInventory inventory;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    private bool wasMoving = false;
    private bool isInWater = false;

    public event Action OnDestinationReached;

    public bool IsMoving => agent != null && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        inventory = GetComponent<PlayerInventory>();

        if (boatModel != null) boatModel.SetActive(false);
        if (characterModel != null) characterModel.SetActive(true);
    }

    private void Start()
    {
        if (inventory != null)
        {
            UpdateWaterTraversalPermission(inventory.HasBoat);
        }
    }

    private void Update()
    {
        if (agent == null) return;

        // 1. Sample NavMesh surface directly beneath character position to trigger boat model swap
        CheckWaterSurfaceStatus();

        // 2. Drive Animator parameters for character mesh
        if (animator != null && characterModel != null && characterModel.activeSelf)
        {
            float currentSpeed = agent.velocity.magnitude / Mathf.Max(0.1f, agent.speed);
            animator.SetFloat(SpeedHash, currentSpeed);

            bool grounded = agent.isOnNavMesh;
            animator.SetBool(IsGroundedHash, grounded);
        }

        // 3. Detect movement arrival and trigger turn/node callbacks
        if (wasMoving && !IsMoving)
        {
            wasMoving = false;
            OnDestinationReached?.Invoke();
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

    private void CheckWaterSurfaceStatus()
    {
        if (!agent.isOnNavMesh) return;

        // Sample position directly under unit transform
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 0.8f, NavMesh.AllAreas))
        {
            int waterAreaIndex = NavMesh.GetAreaFromName("Water");
            if (waterAreaIndex >= 0)
            {
                int waterMask = 1 << waterAreaIndex;
                bool currentlyOnWater = (hit.mask & waterMask) != 0;

                if (currentlyOnWater != isInWater)
                {
                    isInWater = currentlyOnWater;
                    SetModelVisualState(isInWater);
                }
            }
        }
    }

    private void SetModelVisualState(bool onWater)
    {
        if (characterModel != null) characterModel.SetActive(!onWater);
        if (boatModel != null) boatModel.SetActive(onWater);
    }
}