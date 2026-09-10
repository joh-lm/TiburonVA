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

    [Header("Hover UI & Visual Filter")]
    [Tooltip("Prefab of the World Space UI Canvas displaying costs and routes.")]
    [SerializeField] private RoadblockUI uiPrefab;

    [Tooltip("Height offset for the hover UI above this roadblock model.")]
    [SerializeField] private float uiHeightOffset = 1.5f;

    [Header("Highlight Filter Tints")]
    [SerializeField] private Color affordableTint = new Color(0f, 1f, 0.2f, 0.4f); // Green Filter
    [SerializeField] private Color unaffordableTint = new Color(1f, 0f, 0f, 0.4f); // Red Filter

    private List<NodeConnection> blockedConnections = new List<NodeConnection>();
    private RoadblockUI activeUIInstance;
    private Renderer[] modelRenderers;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        modelRenderers = GetComponentsInChildren<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        if (targetJunction != null)
        {
            if (targetJunction.Type == NodeType.Location)
            {
                Debug.LogError($"[Roadblock] Cannot place roadblock on Location '{targetJunction.name}'!");
                Destroy(gameObject);
                return;
            }
            blockedConnections.AddRange(targetJunction.Connections);
        }
        else if (targetConnection != null)
        {
            blockedConnections.Add(targetConnection);
        }

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

    private void OnMouseEnter()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (TurnManager.Instance == null) return;

        PathClickMovement activeUnit = TurnManager.Instance.CurrentUnit;
        if (activeUnit == null) return;
        PlayerInventory unitInventory = activeUnit.GetComponent<PlayerInventory>();

        NodePlatform currentUnitNode = NodePlatform.GetNodeAtPosition(activeUnit.transform.position);
        if (currentUnitNode == null) return;

        bool isConnected = IsPlayerConnectedToRoadblock(currentUnitNode);
        bool hasEnergy = TurnManager.Instance.CanAfford(energyToClear);
        int unitMoney = unitInventory.CurrentMoney;
        bool hasMoney = unitMoney >= moneyToClear;

        bool canRemove = isConnected && hasEnergy && hasMoney;

        // 1. Apply Color Filter Tint (Green if clearable, Red if not)
        ApplyHighlightFilter(canRemove ? affordableTint : unaffordableTint);

        // 2. Spawn and setup Hover UI
        ShowHoverUI(canRemove);
    }

    private void OnMouseExit()
    {
        ClearHighlightFilter();
        HideHoverUI();
    }

    private void ApplyHighlightFilter(Color tintColor)
    {
        foreach (Renderer rend in modelRenderers)
        {
            if (rend == null) continue;

            rend.GetPropertyBlock(propertyBlock);

            // Sets color properties for Built-in, URP, and custom shader pipelines
            propertyBlock.SetColor("_Color", tintColor);
            propertyBlock.SetColor("_BaseColor", tintColor);
            
            // Emissive properties for glow effects
            propertyBlock.SetColor("_EmissionColor", tintColor * 0.8f);

            rend.SetPropertyBlock(propertyBlock);

            // Ensures emission is enabled on the material keywords
            foreach (Material mat in rend.materials)
            {
                mat.EnableKeyword("_EMISSION");
            }
        }
    }   

    private void ClearHighlightFilter()
    {
        foreach (Renderer rend in modelRenderers)
        {
            if (rend == null) continue;
            
            rend.SetPropertyBlock(null);

            // Reset emission keywords back to default
            foreach (Material mat in rend.materials)
            {
                mat.DisableKeyword("_EMISSION");
            }
        }
    }

    private void ShowHoverUI(bool canAfford)
    {
        if (uiPrefab == null) return;

        if (activeUIInstance == null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * uiHeightOffset;
            
            // Instantiate without setting transform as parent to avoid inheriting parent scale shearing
            activeUIInstance = Instantiate(uiPrefab, spawnPos, Quaternion.identity);
        }

        // Keep position attached to roadblock in case roadblock moves
        activeUIInstance.transform.position = transform.position + Vector3.up * uiHeightOffset;

        string routeDescription = FormatBlockedRoutes();
        activeUIInstance.Setup(routeDescription, energyToClear, moneyToClear, canAfford);
        activeUIInstance.gameObject.SetActive(true);
    }

    private void HideHoverUI()
    {
        if (activeUIInstance != null)
        {
            activeUIInstance.gameObject.SetActive(false);
        }
    }

    private string FormatBlockedRoutes()
    {
        if (targetJunction != null)
        {
            return $"Junction: {targetJunction.name} (All Paths)";
        }
        else if (targetConnection != null && targetConnection.NodeA != null && targetConnection.NodeB != null)
        {
            return $"{targetConnection.NodeA.name} ↔ {targetConnection.NodeB.name}";
        }
        return "Unknown Route";
    }

    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (TurnManager.Instance == null || TurnManager.Instance.IsUnitBusy()) return;

        PathClickMovement activeUnit = TurnManager.Instance.CurrentUnit;
        if (activeUnit == null) return;

        NodePlatform currentUnitNode = NodePlatform.GetNodeAtPosition(activeUnit.transform.position);
        if (currentUnitNode == null) return;

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

            if (connection.NodeA == playerNode || connection.NodeB == playerNode)
                return true;

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
        PlayerInventory inventory = unit.GetComponent<PlayerInventory>();

        bool hasEnoughEnergy = TurnManager.Instance != null && TurnManager.Instance.CanAfford(energyToClear);
        int unitMoney = inventory.CurrentMoney;
        bool hasEnoughMoney = unitMoney >= moneyToClear;

        if (hasEnoughEnergy && hasEnoughMoney)
        {
            inventory.TryDeductEnergy(energyToClear);
            if (moneyToClear > 0)
            {
                inventory.TryDeductMoney(moneyToClear);
            }

            ApplyBlockState(false);
            HideHoverUI();

            Debug.Log($"<color=green>[Roadblock Removed]</color> Cleared roadblock for {energyToClear} Energy and ${moneyToClear}!");
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (activeUIInstance != null)
        {
            Destroy(activeUIInstance.gameObject);
        }
    }
}