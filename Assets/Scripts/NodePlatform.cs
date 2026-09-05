using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class NodePlatform : MonoBehaviour
{
    [Header("Graph Connections")]
    [Tooltip("Drag all directly adjacent NodePlatforms that can be reached from this location.")]
    [SerializeField] private List<NodePlatform> neighbors = new List<NodePlatform>();

    [Header("Movement Cost")]
    [SerializeField] private int baseMoveCost = 1;

    [Header("Highlight Settings")]
    [SerializeField] private Color defaultColor = Color.gray;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color unreachableHoverColor = Color.red;

    [Header("Glow Overlay Visuals")]
    [Tooltip("Child GameObject representing the glow effect/ring. If unassigned, one will be created automatically.")]
    [SerializeField] private GameObject glowOverlay;
    [SerializeField] private Color glowColor = new Color(0.2f, 0.8f, 1f, 0.5f); // Light cyan glow

    [Header("Edge Safety Padding")]
    [SerializeField] private float edgePadding = 0.2f;

    private Renderer platformRenderer;
    private Collider platformCollider;
    private Renderer overlayRenderer;
    private bool isReachable = false;

    public List<NodePlatform> Neighbors => neighbors;
    public int BaseMoveCost => baseMoveCost;

    private void Awake()
    {
        platformRenderer = GetComponent<Renderer>();
        platformCollider = GetComponent<Collider>();

        SetupGlowOverlay();
    }

    private void Start()
    {
        // Note: ResetVisuals() is excluded here to avoid race-condition bugs 
        // with TurnManager frame-1 highlights.
    }

    /// <summary>
    /// Creates a subtle overlay disc/quad on top of the node if none was assigned in the Inspector.
    /// </summary>
    private void SetupGlowOverlay()
    {
        if (glowOverlay == null)
        {
            glowOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glowOverlay.name = "GlowOverlay";
            glowOverlay.transform.SetParent(transform);
            
            float surfaceY = platformCollider != null ? platformCollider.bounds.extents.y + 0.02f : 0.52f;
            glowOverlay.transform.localPosition = new Vector3(0f, surfaceY, 0f);
            glowOverlay.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Rotate flat facing UP
            glowOverlay.transform.localScale = Vector3.one * (GetTopSurfaceRadius() * 1.8f);

            Collider col = glowOverlay.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        overlayRenderer = glowOverlay.GetComponent<Renderer>();
        if (overlayRenderer != null)
        {
            Material glowMat = new Material(Shader.Find("Sprites/Default"));
            glowMat.color = glowColor;
            overlayRenderer.material = glowMat;
        }

        glowOverlay.SetActive(false);
    }

    /// <summary>
    /// Highlights or hides the subtle overlay depending on reachability.
    /// </summary>
    public void SetReachableState(bool reachable)
    {
        isReachable = reachable;
        if (glowOverlay != null)
        {
            glowOverlay.SetActive(reachable);
        }

        if (!reachable && platformRenderer != null)
        {
            platformRenderer.material.color = defaultColor;
        }
    }

    public void ResetVisuals()
    {
        isReachable = false;
        if (glowOverlay != null)
        {
            glowOverlay.SetActive(false);
        }

        if (platformRenderer != null)
        {
            platformRenderer.material.color = defaultColor;
        }
    }

    private void OnMouseEnter()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        PathClickMovement activeUnit = TurnManager.Instance != null ? TurnManager.Instance.CurrentUnit : null;
        if (activeUnit == null) return;

        NodePlatform currentUnitNode = GetNodeAtPosition(activeUnit.transform.position);
        if (currentUnitNode != null)
        {
            int cost = currentUnitNode.CalculateGraphMoveCost(this);
            bool canAfford = TurnManager.Instance.CanAfford(cost);

            if (platformRenderer != null)
            {
                platformRenderer.material.color = canAfford ? hoverColor : unreachableHoverColor;
            }

            TurnManager.Instance.ShowHoverCost(cost, gameObject.name);
        }
    }

    private void OnMouseExit()
    {
        if (platformRenderer != null)
        {
            platformRenderer.material.color = defaultColor;
        }

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.HideHoverCost();
        }
    }

    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (TurnManager.Instance == null || TurnManager.Instance.IsUnitBusy()) return;

        PathClickMovement activeCharacter = TurnManager.Instance.CurrentUnit;
        if (activeCharacter == null) return;

        NodePlatform currentUnitNode = GetNodeAtPosition(activeCharacter.transform.position);

        // 1. If clicking the node the unit is ALREADY standing on:
        if (this == currentUnitNode)
        {
            ProcessNodeInteractions(activeCharacter);
            return;
        }

        // 2. If clicking a destination node to move:
        if (currentUnitNode != null)
        {
            int moveCost = currentUnitNode.CalculateGraphMoveCost(this);

            if (TurnManager.Instance.CanAfford(moveCost))
            {
                TurnManager.Instance.DeductEnergy(moveCost);
                float autoRadius = GetTopSurfaceRadius();

                // Subscribe once to handle quest interaction automatically upon arrival
                Action arrivalHandler = null;
                arrivalHandler = () =>
                {
                    activeCharacter.OnDestinationReached -= arrivalHandler;
                    ProcessNodeInteractions(activeCharacter);
                };
                activeCharacter.OnDestinationReached += arrivalHandler;

                activeCharacter.MoveToLocation(transform.position, autoRadius);
                TurnManager.Instance.HideHoverCost();
            }
            else
            {
                Debug.Log("Not enough Energy!");
            }
        }
    }

    /// <summary>
    /// Checks for LocationQuestDeck offers and active Quest fulfillment upon reaching/clicking this node.
    /// </summary>
    public void ProcessNodeInteractions(PathClickMovement unit)
    {
        if (unit == null) return;

        // Step A: Check for Quest Deck Draws
        LocationQuestDeck questDeck = GetComponent<LocationQuestDeck>();
        if (questDeck != null && QuestManager.Instance != null && QuestManager.Instance.CanDrawQuest(unit))
        {
            QuestData drawnQuest = questDeck.DrawQuest();
            if (drawnQuest != null)
            {
                // Pass 'questDeck' so it can be returned if declined
                QuestManager.Instance.PresentQuestOffer(unit, drawnQuest, questDeck);
                return;
            }
        }

        // Step B: Check for Quest Fulfillment
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.CheckAndFulfillQuest(unit, this);
        }
    }

    /// <summary>
    /// Gets all nodes reachable from startNode within the specified max energy limit.
    /// </summary>
    public static HashSet<NodePlatform> GetReachableNodes(NodePlatform startNode, int maxEnergy)
    {
        HashSet<NodePlatform> reachable = new HashSet<NodePlatform>();
        if (startNode == null || maxEnergy <= 0) return reachable;

        Queue<(NodePlatform node, int costSoFar)> queue = new Queue<(NodePlatform, int)>();
        Dictionary<NodePlatform, int> bestCost = new Dictionary<NodePlatform, int>();

        queue.Enqueue((startNode, 0));
        bestCost[startNode] = 0;

        while (queue.Count > 0)
        {
            var (currentNode, currentCost) = queue.Dequeue();

            foreach (NodePlatform neighbor in currentNode.Neighbors)
            {
                if (neighbor == null) continue;

                int newCost = currentCost + neighbor.BaseMoveCost;
                if (newCost <= maxEnergy)
                {
                    if (!bestCost.ContainsKey(neighbor) || newCost < bestCost[neighbor])
                    {
                        bestCost[neighbor] = newCost;
                        reachable.Add(neighbor);
                        queue.Enqueue((neighbor, newCost));
                    }
                }
            }
        }

        return reachable;
    }

    /// <summary>
    /// Calculates step distance using Breadth-First Search (BFS) along neighbor connections.
    /// </summary>
    public int CalculateGraphMoveCost(NodePlatform targetNode)
    {
        if (targetNode == null || targetNode == this) return 0;

        Queue<(NodePlatform node, int depth)> queue = new Queue<(NodePlatform, int)>();
        HashSet<NodePlatform> visited = new HashSet<NodePlatform>();

        queue.Enqueue((this, 0));
        visited.Add(this);

        while (queue.Count > 0)
        {
            var (currentNode, currentDepth) = queue.Dequeue();

            if (currentNode == targetNode)
            {
                return currentDepth;
            }

            foreach (NodePlatform neighbor in currentNode.Neighbors)
            {
                if (neighbor != null && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue((neighbor, currentDepth + 1));
                }
            }
        }

        return 1;
    }

    /// <summary>
    /// Finds which NodePlatform a character is standing on based on collider overlap.
    /// </summary>
    public static NodePlatform GetNodeAtPosition(Vector3 position)
    {
        Collider[] hitColliders = Physics.OverlapSphere(position, 1.0f);
        foreach (var col in hitColliders)
        {
            NodePlatform node = col.GetComponent<NodePlatform>();
            if (node != null)
            {
                return node;
            }
        }
        return null;
    }

    public float GetTopSurfaceRadius()
    {
        if (platformCollider != null)
        {
            Vector3 extents = platformCollider.bounds.extents;
            float smallestHorizontalExtent = Mathf.Min(extents.x, extents.z);
            return Mathf.Max(0.1f, smallestHorizontalExtent - edgePadding);
        }
        return 0.5f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (NodePlatform neighbor in neighbors)
        {
            if (neighbor != null)
            {
                Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, neighbor.transform.position + Vector3.up * 0.5f);
            }
        }
    }
}