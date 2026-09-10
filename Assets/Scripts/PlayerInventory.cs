using UnityEngine;
using System;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Items")]
    [SerializeField] private bool hasBoat = false;

    public bool HasBoat => hasBoat;

    public event Action OnInventoryChanged;

    /// <summary>
    /// Grants or removes the boat item from this player's inventory.
    /// Updates NavMesh river pathfinding accessibility.
    /// </summary>
    public void SetHasBoat(bool state)
    {
        hasBoat = state;

        // Update NavMeshAgent Area Mask permissions
        PathClickMovement movement = GetComponent<PathClickMovement>();
        if (movement != null)
        {
            movement.UpdateWaterTraversalPermission(hasBoat);
        }

        // Refresh glows on reachable water locations
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.UpdateReachableHighlights();
        }

        OnInventoryChanged?.Invoke();
        Debug.Log($"<color=cyan>[Inventory]</color> {gameObject.name} HasBoat set to: {hasBoat}");
    }
}