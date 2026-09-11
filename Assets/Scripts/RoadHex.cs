using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class RoadHex : MonoBehaviour
{
    [Header("Correlated Edge/Junction")]
    [Tooltip("Assign the NodeConnection edge this road hex represents.")]
    [SerializeField] private NodeConnection targetConnection;

    [Tooltip("Assign IF this hex represents a Junction platform instead of an edge segment.")]
    [SerializeField] private NodePlatform targetJunction;

    [Header("Placement Prefabs")]
    [Tooltip("The Roadblock prefab asset instantiated upon placement.")]
    [SerializeField] private Roadblock roadblockPrefab;

    [Header("Hover Preview Visuals")]
    [SerializeField] private float previewOffsetY = 0.5f;

    private GameObject activePreviewInstance;
    private Renderer[] previewRenderers;

    public NodeConnection TargetConnection => targetConnection;
    public NodePlatform TargetJunction => targetJunction;

    private void OnMouseEnter()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (IsValidPlacementTarget(out PlayerInventory activeInventory))
        {
            ShowRoadblockPreview();
        }
    }

    private void OnMouseExit()
    {
        HideRoadblockPreview();
    }

    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (TurnManager.Instance == null || TurnManager.Instance.IsUnitBusy()) return;

        // 1. Verify placement validity and fetch active player inventory
        if (!IsValidPlacementTarget(out PlayerInventory activeInventory)) return;

        // 2. Instantiate permanent Roadblock on top/center of this hex
        Vector3 spawnPosition = transform.position + Vector3.up * previewOffsetY;
        Roadblock placedBlock = Instantiate(roadblockPrefab, spawnPosition, transform.rotation);

        // 3. Block the correlated edge connection or junction
        if (targetConnection != null)
        {
            targetConnection.SetBlocked(true, placedBlock);
        }
        else if (targetJunction != null)
        {
            foreach (NodeConnection conn in targetJunction.Connections)
            {
                if (conn != null) conn.SetBlocked(true, placedBlock);
            }
        }

        // 4. Consume the selected roadblock from active inventory & clean up preview
        activeInventory.RemoveSelectedItem();
        HideRoadblockPreview();

        Debug.Log($"<color=green>[Roadblock Placed]</color> Roadblock placed on {gameObject.name}");
    }

    /// <summary>
    /// Validates item selection, blocked state, and adjacency (direct or through Junctions) to player position.
    /// </summary>
    private bool IsValidPlacementTarget(out PlayerInventory inventory)
    {
        inventory = null;

        if (TurnManager.Instance == null) return false;

        PathClickMovement activeUnit = TurnManager.Instance.CurrentUnit;
        if (activeUnit == null) return false;

        inventory = activeUnit.GetComponent<PlayerInventory>();
        if (inventory == null) return false;

        // 1. Check if player has selected a consumable Roadblock item in inventory
        InventoryItem selectedItem = inventory.GetSelectedItem();
        if (selectedItem == null || selectedItem.itemType != ItemType.Roadblock) return false;

        // 2. Check if target edge or junction is already blocked
        if (targetConnection != null && targetConnection.IsBlocked) return false;

        // 3. Resolve active unit's current location platform
        NodePlatform playerNode = NodePlatform.GetNodeAtPosition(activeUnit.transform.position);
        if (playerNode == null) return false;

        // 4. Check connectivity (directly adjacent or connected through intermediate Junctions)
        return IsPlayerConnectedToHex(playerNode);
    }

    /// <summary>
    /// Checks if player's current node reaches this hex directly or via unblocked Junction chains.
    /// </summary>
    private bool IsPlayerConnectedToHex(NodePlatform playerNode)
    {
        if (targetConnection != null)
        {
            // Direct connection to current player node
            if (targetConnection.NodeA == playerNode || targetConnection.NodeB == playerNode)
                return true;

            // Traversable connection past intermediate Junctions
            return IsNodeConnectedToPlayerViaJunctions(targetConnection.NodeA, playerNode) ||
                   IsNodeConnectedToPlayerViaJunctions(targetConnection.NodeB, playerNode);
        }

        if (targetJunction != null)
        {
            if (targetJunction == playerNode) return true;
            return IsNodeConnectedToPlayerViaJunctions(targetJunction, playerNode);
        }

        return false;
    }

    /// <summary>
    /// BFS graph check matching Roadblock removal logic to evaluate path traversability through Junctions.
    /// </summary>
    private bool IsNodeConnectedToPlayerViaJunctions(NodePlatform endpoint, NodePlatform targetPlayerNode)
    {
        if (endpoint == null || targetPlayerNode == null) return false;
        
        // Stops traversal if endpoint is another stoppable Location node (not player's current spot)
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

            // Only propagate through passthrough Junction nodes
            if (current.Type == NodeType.Junction)
            {
                foreach (NodeConnection conn in current.Connections)
                {
                    if (conn != null && !conn.IsBlocked)
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

    private void ShowRoadblockPreview()
    {
        if (roadblockPrefab == null) return;

        if (activePreviewInstance == null)
        {
            Vector3 previewPos = transform.position + Vector3.up * previewOffsetY;
            activePreviewInstance = Instantiate(roadblockPrefab.gameObject, previewPos, transform.rotation);

            foreach (Collider col in activePreviewInstance.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            previewRenderers = activePreviewInstance.GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in previewRenderers)
            {
                foreach (Material mat in rend.materials)
                {
                    Color c = mat.color;
                    c.a = 0.5f;
                    mat.color = c;
                }
            }
        }

        activePreviewInstance.SetActive(true);
    }

    private void HideRoadblockPreview()
    {
        if (activePreviewInstance != null)
        {
            Destroy(activePreviewInstance);
            activePreviewInstance = null;
        }
    }
}