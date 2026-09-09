using UnityEngine;

public class NodeConnection : MonoBehaviour
{
    [Header("Connected Nodes")]
    [Tooltip("First platform or junction node connected by this road segment.")]
    [SerializeField] private NodePlatform nodeA;
    
    [Tooltip("Second platform or junction node connected by this road segment.")]
    [SerializeField] private NodePlatform nodeB;

    [Header("Connection Properties")]
    [SerializeField] private bool isWaterRoute = false;
    public bool IsWaterRoute => isWaterRoute;

    [Header("Connection State")]
    [SerializeField] private bool isBlocked = false;

    [Header("Optional Roadblock Reference")]
    [Tooltip("Child barrier object or placed Roadblock component.")]
    [SerializeField] private Roadblock activeRoadblock;

    public bool IsBlocked => isBlocked;
    public NodePlatform NodeA => nodeA;
    public NodePlatform NodeB => nodeB;

    private void Start()
    {
        if (activeRoadblock != null)
        {
            SetBlocked(true, activeRoadblock);
        }
    }

    /// <summary>
    /// Gets the node on the opposite end of this connection.
    /// </summary>
    public NodePlatform GetOtherNode(NodePlatform source)
    {
        if (source == nodeA) return nodeB;
        if (source == nodeB) return nodeA;
        return null;
    }

    public void SetBlocked(bool blocked, Roadblock roadblockInstance = null)
    {
        isBlocked = blocked;
        activeRoadblock = blocked ? roadblockInstance : null;

        // Refresh range visualizer during the active turn
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.HighlightReachableNodes();
        }
    }

    private void OnDrawGizmos()
    {
        if (nodeA != null && nodeB != null)
        {
            Gizmos.color = isBlocked ? Color.red : Color.green;
            Gizmos.DrawLine(nodeA.transform.position + Vector3.up * 0.5f, nodeB.transform.position + Vector3.up * 0.5f);
        }
    }
}