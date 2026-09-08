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

    [Header("Removal Costs")]
    [SerializeField] private int energyToClear = 2;
    [SerializeField] private int moneyToClear = 50;

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
            TryClearRoadblock(activeUnit);
        }
        else
        {
            Debug.Log("[Roadblock] You must be standing at a connected location to clear this roadblock!");
        }
    }

    private bool IsPlayerConnectedToRoadblock(NodePlatform playerNode)
    {
        foreach (NodeConnection connection in blockedConnections)
        {
            if (connection == null) continue;

            // Direct adjacency: Player is standing on NodeA or NodeB of this exact connection
            if (connection.NodeA == playerNode || connection.NodeB == playerNode)
                return true;

            // Traversal check: Search backwards from NodeA and NodeB using ONLY Junctions
            if (IsNodeConnectedToPlayerViaJunctions(connection.NodeA, playerNode) ||
                IsNodeConnectedToPlayerViaJunctions(connection.NodeB, playerNode))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsNodeConnectedToPlayerViaJunctions(NodePlatform endpoint, NodePlatform targetPlayerNode)
    {
        if (endpoint == null || targetPlayerNode == null) return false;

        // Stop if the endpoint itself is a Location and not the player's current node
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

            if (current.Type == NodeType.Junction)
            {
                foreach (NodeConnection conn in current.Connections)
                {
                    if (conn != null && !conn.IsBlocked && !blockedConnections.Contains(conn))
                    {
                        NodePlatform neighbor = conn.GetOtherNode(current);
                        if (neighbor != null && !visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);

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

    public void TryClearRoadblock(PathClickMovement unit)
    {
        bool hasEnoughEnergy = TurnManager.Instance != null && TurnManager.Instance.CanAfford(energyToClear);
        
        // Retrieve unit's current money balance from QuestManager
        int unitMoney = QuestManager.Instance != null ? QuestManager.Instance.GetMoney(unit) : 0;
        bool hasEnoughMoney = unitMoney >= moneyToClear;

        if (hasEnoughEnergy && hasEnoughMoney)
        {
            // Deduct Energy & Money
            TurnManager.Instance.DeductEnergy(energyToClear);
            if (QuestManager.Instance != null && moneyToClear > 0)
            {
                QuestManager.Instance.DeductMoney(unit, moneyToClear);
            }

            ApplyBlockState(false);

            Debug.Log($"<color=green>[Roadblock Removed]</color> Cleared roadblock for {energyToClear} Energy and ${moneyToClear}!");
            Destroy(gameObject);
        }
        else
        {
            if (!hasEnoughEnergy)
            {
                Debug.Log($"[Roadblock] Not enough Energy! Requires {energyToClear} Energy.");
            }
            if (!hasEnoughMoney)
            {
                Debug.Log($"[Roadblock] Not enough Money! Requires ${moneyToClear} (You have ${unitMoney}).");
            }
        }
    }
}