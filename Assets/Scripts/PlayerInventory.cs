using UnityEngine;
using System;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Items")]
    [SerializeField] private bool hasBoat = false;

    [Header("Quest Stats")]
    [SerializeField] private int completedQuestCount = 0;

    [Header("Energy Stats")]
    [SerializeField] private int currentEnergy = 0;
    [SerializeField] private int maxEnergy = 10;

    [Header("Currency Stats")]
    [SerializeField] private int currentMoney = 0;

    public bool HasBoat => hasBoat;
    public int CompletedQuestCount => completedQuestCount;
    public int CurrentEnergy => currentEnergy;
    public int CurrentMoney => currentMoney;
    
    public event Action OnInventoryChanged;
    public event Action OnEnergyChanged;
    public event Action OnMoneyChanged;

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
    public void IncrementCompletedQuests()
    {
        completedQuestCount++;
    }

    /// <summary>
    /// Grants energy at the start of the unit's turn.
    /// </summary>
    public void AddEnergy(int amount)
    {
        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
        TurnManager.Instance.UpdateTurnUI();
        OnEnergyChanged?.Invoke();
    }

    /// <summary>
    /// Deducts energy when performing actions or moving.
    /// </summary>
    public bool TryDeductEnergy(int amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            TurnManager.Instance.UpdateTurnUI();
            OnEnergyChanged?.Invoke();
            return true;
        }
        return false;
    }

    public bool CanAfford(int cost)
    {
        return currentEnergy >= cost;
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        currentMoney += amount;
        TurnManager.Instance.UpdateTurnUI();
        OnMoneyChanged?.Invoke();
    }

    public bool TryDeductMoney(int amount)
    {
        if(currentMoney >= amount)
        {
            currentMoney -= amount;
            TurnManager.Instance.UpdateTurnUI();
            OnMoneyChanged?.Invoke();
            return true;
        }
        return false;
    }
}