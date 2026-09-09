using UnityEngine;
using UnityEngine.AI;
using System;

[RequireComponent(typeof(NavMeshAgent))]
public class PathClickMovement : MonoBehaviour
{
    [Header("Visual Model Swapping")]
    [Tooltip("The standard character mesh model GameObject.")]
    [SerializeField] private GameObject characterModel;

    [Tooltip("The boat model GameObject (parented to unit or assigned prefab).")]
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

        if (boatModel != null)
        {
            boatModel.SetActive(false);
        }
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

        // 1. Monitor NavMesh Area under feet to trigger Boat visual model swap
        CheckWaterSurfaceStatus();

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

    public void MoveToLocation(Vector3 destination, float surfaceRadius)
    {
        agent.stoppingDistance = surfaceRadius;
        agent.SetDestination(destination);
        wasMoving = true;
    }

    /// <summary>
    /// Enables or disables the Water area mask on the NavMeshAgent based on boat ownership.
    /// </summary>
    public void UpdateWaterTraversalPermission(bool canUseWater)
    {
        if (agent == null) return;

        int waterAreaIndex = NavMesh.GetAreaFromName("Water");
        if (waterAreaIndex < 0) return;

        int waterMask = 1 << waterAreaIndex;

        if (canUseWater)
        {
            agent.areaMask |= waterMask; // Enable Water area
        }
        else
        {
            agent.areaMask &= ~waterMask; // Disable Water area
        }
    }

    private void CheckWaterSurfaceStatus()
    {
        if (!agent.isOnNavMesh) return;

        NavMeshHit hit;
        if (agent.SamplePathPosition(NavMesh.AllAreas, 0.5f, out hit))
        {
            int waterAreaIndex = NavMesh.GetAreaFromName("Water");
            bool currentlyOnWater = (hit.mask & (1 << waterAreaIndex)) != 0;

            if (currentlyOnWater != isInWater)
            {
                isInWater = currentlyOnWater;
                SetModelVisualState(isInWater);
            }
        }
    }

    private void SetModelVisualState(bool onWater)
    {
        if (characterModel != null) characterModel.SetActive(!onWater);
        if (boatModel != null) boatModel.SetActive(onWater);
    }
}