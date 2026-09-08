using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class Roadblock : MonoBehaviour
{
    [Header("Block Target (Assign ONE of these)")]
    [Tooltip("Assign if blocking a single road connection edge.")]
    [SerializeField] private NodeConnection targetConnection;

    [Tooltip("Assign if blocking a Junction node to sever ALL of its attached connections.")]
    [SerializeField] private NodePlatform targetJunction;

    [Header("Removal Settings")]
    [SerializeField] private int energyToClear = 2;

    private List<NodeConnection> blockedConnections = new List<NodeConnection>();

    private void Start()
    {
        // 1. Validate Target Junction (Must be NodeType.Junction)
        if (targetJunction != null)
        {
            if (targetJunction.Type == NodeType.Location)
            {
                Debug.LogError($"[Roadblock] Cannot place roadblock on Location '{targetJunction.name}'! Roadblocks can only be placed on Junctions or Road Connections.");
                Destroy(gameObject);
                return;
            }

            blockedConnections.AddRange(targetJunction.Connections);
        }
        // 2. Validate Target Single Connection
        else if (targetConnection != null)
        {
            blockedConnections.Add(targetConnection);
        }
        else
        {
            Debug.LogWarning($"[Roadblock] '{gameObject.name}' has no assigned Connection or Junction target!");
        }

        // Apply blocked state across all targeted connections
        ApplyBlockState(true);
    }

    private void ApplyBlockState(bool isBlocked)
    {
        foreach (NodeConnection connection in blockedConnections)
        {
            if (connection != null)
            {
                connection.SetBlocked(isBlocked, isBlocked ? this : null);
            }
        }
    }

    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (TurnManager.Instance == null || TurnManager.Instance.IsUnitBusy()) return;

        PathClickMovement activeUnit = TurnManager.Instance.CurrentUnit;
        if (activeUnit == null) return;

        NodePlatform currentUnitNode = NodePlatform.GetNodeAtPosition(activeUnit.transform.position);
        if (currentUnitNode == null) return;

        // Check if player is directly adjacent or connected via unblocked Junctions
        if (IsPlayerConnectedToRoadblock(currentUnitNode))
        {
            TryClearRoadblock();
        }
        else
        {
            Debug.Log("[Roadblock] You must be standing at a connected location to clear this roadblock!");
        }
    }

    /// <summary>
    /// Verifies if the player's node can reach either endpoint of this roadblock 
    /// by moving ONLY through unblocked Junction nodes (never stepping through other Location nodes).
    /// </summary>
    private bool IsPlayerConnectedToRoadblock(NodePlatform playerNode)
    {
        foreach (NodeConnection connection in blockedConnections)
        {
            if (connection == null) continue;

            // 1. Direct adjacency: Player is standing on NodeA or NodeB of this exact connection
            if (connection.NodeA == playerNode || connection.NodeB == playerNode)
                return true;

            // 2. Traversal check: Search outward from NodeA and NodeB using ONLY Junctions
            if (IsNodeConnectedToPlayerViaJunctions(connection.NodeA, playerNode) ||
                IsNodeConnectedToPlayerViaJunctions(connection.NodeB, playerNode))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Search backwards from an endpoint of the roadblock to the player's node.
    /// MUST ONLY step through NodeType.Junction nodes. If it hits another Location, it halts.
    /// </summary>
    private bool IsNodeConnectedToPlayerViaJunctions(NodePlatform endpoint, NodePlatform targetPlayerNode)
    {
        if (endpoint == null || targetPlayerNode == null) return false;

        // If the endpoint itself is a Location and it isn't the player, we cannot reach through it!
        if (endpoint.Type == NodeType.Location && endpoint != targetPlayerNode)
        {
            return false;
        }

        Queue<NodePlatform> queue = new Queue<NodePlatform>();
        HashSet<NodePlatform> visited = new HashSet<NodePlatform>();

        queue.Enqueue(endpoint);
        visited.Add(endpoint);

        while (queue.Count > 0)
        {
            NodePlatform current = queue.Dequeue();

            if (current == targetPlayerNode) return true;

            // Strictly enforce: We can ONLY step outward from an endpoint if it is a Junction
            if (current.Type == NodeType.Junction)
            {
                foreach (NodeConnection conn in current.Connections)
                {
                    // Must be an unblocked connection and not part of the active roadblock
                    if (conn != null && !conn.IsBlocked && !blockedConnections.Contains(conn))
                    {
                        NodePlatform neighbor = conn.GetOtherNode(current);
                        if (neighbor != null && !visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);

                            // Only enqueue if the neighbor is the player's Location OR another pass-through Junction.
                            // Never enqueue other Locations!
                            if (neighbor == targetPlayerNode || neighbor.Type == NodeType.Junction)
                            {
                                queue.Enqueue(neighbor);
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    public void TryClearRoadblock()
    {
        if (TurnManager.Instance.CanAfford(energyToClear))
        {
            TurnManager.Instance.DeductEnergy(energyToClear);

            ApplyBlockState(false);

            Debug.Log($"<color=green>[Roadblock Removed]</color> Cleared roadblock for {energyToClear} Energy!");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"[Roadblock] Not enough Energy! Clearing requires {energyToClear} Energy.");
        }
    }
}