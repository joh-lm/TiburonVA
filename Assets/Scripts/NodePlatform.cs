using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum NodeType
{
    Location,
    Junction
}

[RequireComponent(typeof(Collider))]
public class NodePlatform : MonoBehaviour
{
    [Header("Node Configuration")]
    [SerializeField] private NodeType nodeType = NodeType.Location;

    [Header("Graph Connections")]
    [Tooltip("List of NodeConnections attached to this node platform or junction.")]
    [SerializeField] private List<NodeConnection> connections = new List<NodeConnection>();

    [Header("Movement Cost")]
    [SerializeField] private int baseMoveCost = 1;

    [Header("Highlight Settings")]
    [SerializeField] private Color defaultColor = Color.gray;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color unreachableHoverColor = Color.red;

    [Header("Glow Overlay Visuals")]
    [SerializeField] private GameObject glowOverlay;
    [SerializeField] private Color glowColor = new Color(0.2f, 0.8f, 1f, 0.5f);

    [Header("Hover UI Settings")]
    [SerializeField] private LocationHoverUI uiPrefab;
    [SerializeField] private float uiHeightOffset = 1.5f;

    [Header("Edge Safety Padding")]
    [SerializeField] private float edgePadding = 0.2f;

    private Renderer platformRenderer;
    private Collider platformCollider;
    private Renderer overlayRenderer;
    private bool isReachable = false;
    private LocationHoverUI activeUIInstance;

    public NodeType Type => nodeType;
    public List<NodeConnection> Connections => connections;
    public int BaseMoveCost => baseMoveCost;

    protected virtual void Awake()
    {
        platformRenderer = GetComponent<Renderer>();
        platformCollider = GetComponent<Collider>();
        SetupGlowOverlay();
    }

    private void SetupGlowOverlay()
    {
        if (glowOverlay == null)
        {
            Debug.Log("Glow Overlay required!");
            return;
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

    public void SetReachableState(bool reachable)
    {
        if (nodeType == NodeType.Junction) return;

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

    protected virtual void OnMouseEnter()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (nodeType == NodeType.Junction) return;

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
                int cost = currentUnitNode.CalculateGraphMoveCost(this, activeUnit);
                bool canAfford = TurnManager.Instance.CanAfford(cost);

                if (platformRenderer != null)
                {
                    platformRenderer.material.color = canAfford ? hoverColor : unreachableHoverColor;
                }

                ShowHoverUI(cost, canAfford);
            }
        }
    }

    protected virtual void OnMouseExit()
    {
        if (platformRenderer != null)
        {
            platformRenderer.material.color = defaultColor;
        }

        HideHoverUI();
    }

    private void ShowHoverUI(int cost, bool canAfford)
{
    if (uiPrefab == null) return;

    if (activeUIInstance == null)
    {
        Vector3 spawnPos = transform.position + Vector3.up * uiHeightOffset;
        activeUIInstance = Instantiate(uiPrefab, spawnPos, Quaternion.identity);
    }

    activeUIInstance.transform.position = transform.position + Vector3.up * uiHeightOffset;

    // Fetch exact remaining quest count from LocationQuestDeck component
    LocationQuestDeck questDeck = GetComponent<LocationQuestDeck>();
    int questCount = questDeck != null ? questDeck.AvailableQuestCount : 0;

    activeUIInstance.Setup(gameObject.name, cost, canAfford, questCount);
    activeUIInstance.gameObject.SetActive(true);
}

    protected void HideHoverUI()
    {
        if (activeUIInstance != null)
        {
            activeUIInstance.gameObject.SetActive(false);
        }
    }

    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (nodeType == NodeType.Junction) return;
        if (TurnManager.Instance == null || TurnManager.Instance.IsUnitBusy()) return;

        PathClickMovement activeCharacter = TurnManager.Instance.CurrentUnit;
        if (activeCharacter == null) return;
        PlayerInventory activeInventory = activeCharacter.GetComponent<PlayerInventory>();

        NodePlatform currentUnitNode = GetNodeAtPosition(activeCharacter.transform.position);

        if (this == currentUnitNode)
        {
            ProcessNodeInteractions(activeCharacter);
            return;
        }

        if (currentUnitNode != null)
        {
            int moveCost = currentUnitNode.CalculateGraphMoveCost(this, activeCharacter);

            if (activeInventory.TryDeductEnergy(moveCost))
            {
                float autoRadius = GetTopSurfaceRadius();

                Action arrivalHandler = null;
                arrivalHandler = () =>
                {
                    activeCharacter.OnDestinationReached -= arrivalHandler;
                    ProcessNodeInteractions(activeCharacter);
                };
                activeCharacter.OnDestinationReached += arrivalHandler;

                activeCharacter.MoveToLocation(transform.position, autoRadius, this);
                HideHoverUI();
            }
            else
            {
                Debug.Log("Not enough Energy!");
            }
        }
    }

    public virtual void ProcessNodeInteractions(PathClickMovement unit)
    {
        if (unit == null || nodeType == NodeType.Junction) return;
        
        TurnManager.Instance.UpdateReachableHighlights();
        LocationQuestDeck questDeck = GetComponent<LocationQuestDeck>();

        if (QuestManager.Instance != null && QuestManager.Instance.HasFulfilledQuest(unit, this))
        {
            QuestManager.Instance.PresentQuestCompletedUI(unit, this, () =>
            {
                TryPresentNewQuestOffer(unit, questDeck);
            });
        }
        else
        {
            TryPresentNewQuestOffer(unit, questDeck);
        }
    }

    private void TryPresentNewQuestOffer(PathClickMovement unit, LocationQuestDeck questDeck)
    {
        if (questDeck != null && QuestManager.Instance != null && QuestManager.Instance.CanDrawQuest(unit))
        {
            QuestData drawnQuest = questDeck.DrawQuest();
            if (drawnQuest != null)
            {
                QuestManager.Instance.PresentQuestOffer(unit, drawnQuest, questDeck);
            }
        }
    }

    public List<NodePlatform> GetUnblockedNeighbors(PathClickMovement unit = null)
    {
        List<NodePlatform> validNeighbors = new List<NodePlatform>();

        PlayerInventory inv = unit != null ? unit.GetComponent<PlayerInventory>() : null;
        bool unitHasBoat = inv != null && inv.HasBoat;

        foreach (NodeConnection conn in connections)
        {
            if (conn != null && !conn.IsBlocked)
            {
                if (conn.IsWaterRoute && !unitHasBoat) continue;

                NodePlatform neighbor = conn.GetOtherNode(this);
                if (neighbor != null && !validNeighbors.Contains(neighbor))
                {
                    validNeighbors.Add(neighbor);
                }
            }
        }

        return validNeighbors;
    }

    public int CalculateGraphMoveCost(NodePlatform targetNode, PathClickMovement unit = null)
    {
        if (targetNode == null || targetNode == this) return 0;

        Dictionary<NodePlatform, int> minCostToNode = new Dictionary<NodePlatform, int>();
        List<(NodePlatform node, int costSoFar)> openSet = new List<(NodePlatform, int)>();

        openSet.Add((this, 0));
        minCostToNode[this] = 0;

        while (openSet.Count > 0)
        {
            openSet.Sort((a, b) => a.costSoFar.CompareTo(b.costSoFar));
            var (currentNode, currentCost) = openSet[0];
            openSet.RemoveAt(0);

            if (currentNode == targetNode)
            {
                return currentCost;
            }

            if (currentCost > minCostToNode[currentNode]) continue;

            foreach (NodePlatform neighbor in currentNode.GetUnblockedNeighbors(unit))
            {
                if (neighbor == null) continue;

                int edgeCost = neighbor.BaseMoveCost;
                int newCost = currentCost + edgeCost;

                if (!minCostToNode.ContainsKey(neighbor) || newCost < minCostToNode[neighbor])
                {
                    minCostToNode[neighbor] = newCost;
                    openSet.Add((neighbor, newCost));
                }
            }
        }

        return 999;
    }

    public static HashSet<NodePlatform> GetReachableNodes(NodePlatform startNode, int maxEnergy, PathClickMovement unit = null)
    {
        HashSet<NodePlatform> reachable = new HashSet<NodePlatform>();
        if (startNode == null || maxEnergy <= 0) return reachable;

        Dictionary<NodePlatform, int> minCostToNode = new Dictionary<NodePlatform, int>();
        List<(NodePlatform node, int costSoFar)> openSet = new List<(NodePlatform, int)>();

        openSet.Add((startNode, 0));
        minCostToNode[startNode] = 0;

        while (openSet.Count > 0)
        {
            openSet.Sort((a, b) => a.costSoFar.CompareTo(b.costSoFar));
            var (currentNode, currentCost) = openSet[0];
            openSet.RemoveAt(0);

            if (currentCost > minCostToNode[currentNode]) continue;

            foreach (NodePlatform neighbor in currentNode.GetUnblockedNeighbors(unit))
            {
                if (neighbor == null) continue;

                int edgeCost = neighbor.BaseMoveCost;
                int newCost = currentCost + edgeCost;

                if (newCost <= maxEnergy)
                {
                    if (!minCostToNode.ContainsKey(neighbor) || newCost < minCostToNode[neighbor])
                    {
                        minCostToNode[neighbor] = newCost;

                        if (neighbor.Type == NodeType.Location)
                        {
                            reachable.Add(neighbor);
                        }

                        openSet.Add((neighbor, newCost));
                    }
                }
            }
        }

        return reachable;
    }

    public static NodePlatform GetNodeAtPosition(Vector3 position)
    {
        Collider[] hitColliders = Physics.OverlapSphere(position, 1.2f);
        foreach (var col in hitColliders)
        {
            NodePlatform node = col.GetComponent<NodePlatform>();
            if (node != null) return node;
        }

        if (Physics.Raycast(position + Vector3.up * 2.0f, Vector3.down, out RaycastHit hit, 10.0f))
        {
            NodePlatform node = hit.collider.GetComponent<NodePlatform>();
            if (node != null) return node;
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

    private void OnDestroy()
    {
        if (activeUIInstance != null)
        {
            Destroy(activeUIInstance.gameObject);
        }
    }
}