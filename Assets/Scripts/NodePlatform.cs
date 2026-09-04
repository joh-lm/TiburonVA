using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class NodePlatform : MonoBehaviour
{
    [Header("Graph Connections")]
    [Tooltip("Drag all directly adjacent NodePlatforms that can be reached from this location.")]
    [SerializeField] private List<NodePlatform> neighbors = new List<NodePlatform>();

    [Header("Highlight Settings")]
    [SerializeField] private Color defaultColor = Color.gray;
    [SerializeField] private Color hoverColor = Color.yellow;

    [Header("Edge Safety Padding")]
    [SerializeField] private float edgePadding = 0.2f;

    private Renderer platformRenderer;
    private Collider platformCollider;

    public List<NodePlatform> Neighbors => neighbors;

    private void Awake()
    {
        platformRenderer = GetComponent<Renderer>();
        platformCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        if (platformRenderer != null)
        {
            platformRenderer.material.color = defaultColor;
        }
    }

    private void OnMouseEnter()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (platformRenderer != null)
        {
            platformRenderer.material.color = hoverColor;
        }

        PathClickMovement activeUnit = TurnManager.Instance != null ? TurnManager.Instance.CurrentUnit : null;
        if (activeUnit != null)
        {
            NodePlatform currentUnitNode = GetNodeAtPosition(activeUnit.transform.position);
            
            if (currentUnitNode != null)
            {
                int cost = currentUnitNode.CalculateGraphMoveCost(this);
                TurnManager.Instance.ShowHoverCost(cost, gameObject.name);
            }
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
        if (activeCharacter != null)
        {
            NodePlatform currentUnitNode = GetNodeAtPosition(activeCharacter.transform.position);
            
            if (currentUnitNode != null)
            {
                int moveCost = currentUnitNode.CalculateGraphMoveCost(this);

                if (TurnManager.Instance.CanAfford(moveCost))
                {
                    TurnManager.Instance.DeductEnergy(moveCost);
                    float autoRadius = GetTopSurfaceRadius();
                    activeCharacter.MoveToLocation(transform.position, autoRadius);
                    TurnManager.Instance.HideHoverCost();
                }
                else
                {
                    Debug.Log("Not enough Energy!");
                }
            }
        }
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

        return 1; // Fallback default if unreachable
    }

    /// <summary>
    /// Finds which NodePlatform a character is standing on based on collider overlap.
    /// </summary>
    private NodePlatform GetNodeAtPosition(Vector3 position)
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
        return this;
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

    // Visual Gizmos to see neighbor connections directly in Scene view
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