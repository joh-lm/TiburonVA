using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Boat,
    Roadblock
}

[Serializable]
public class InventoryItem
{
    public string itemTitle;
    [TextArea(2, 4)] public string description;
    public Sprite itemIcon;
    public ItemType itemType;
    public bool isConsumable;

    public InventoryItem(string title, string desc, Sprite icon, ItemType type, bool consumable)
    {
        itemTitle = title;
        description = desc;
        itemIcon = icon;
        itemType = type;
        isConsumable = consumable;
    }
}

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Capacity")]
    [SerializeField] private int maxInventorySlots = 2;
    [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();

    [Header("Item Sprites")]
    [Tooltip("Assign the Boat icon sprite asset here.")]
    [SerializeField] private Sprite boatSprite;

    [Tooltip("Assign the Roadblock icon sprite asset here.")]
    [SerializeField] private Sprite roadblockSprite;

    [Header("Quest Stats")]
    [SerializeField] private int completedQuestCount = 0;

    [Header("Energy Stats")]
    [SerializeField] private int currentEnergy = 0;
    [SerializeField] private int maxEnergy = 10;

    [Header("Currency Stats")]
    [SerializeField] private int currentMoney = 0;

    private int selectedSlotIndex = -1; // -1 means no item selected

    public int MaxInventorySlots => maxInventorySlots;
    public List<InventoryItem> Items => items;
    public int SelectedSlotIndex => selectedSlotIndex;

    public bool HasBoat
    {
        get
        {
            foreach (var item in items)
            {
                if (item != null && item.itemType == ItemType.Boat) return true;
            }
            return false;
        }
    }

    public bool HasRoadblock
    {
        get
        {
            foreach (var item in items)
            {
                if (item != null && item.itemType == ItemType.Roadblock) return true;
            }
            return false;
        }
    }

    public int CompletedQuestCount => completedQuestCount;
    public int CurrentEnergy => currentEnergy;
    public int CurrentMoney => currentMoney;

    public event Action OnInventoryChanged;
    public event Action OnEnergyChanged;
    public event Action OnMoneyChanged;

    /// <summary>
    /// Attempts to add an item to the player's inventory by string name.
    /// Fails if inventory is at max capacity (maxInventorySlots).
    /// </summary>
    public bool TryAddItem(string rewardItemName)
    {
        if (string.IsNullOrEmpty(rewardItemName)) return false;

        if (items.Count >= maxInventorySlots)
        {
            Debug.LogWarning($"[Inventory] Cannot add '{rewardItemName}': Inventory full ({items.Count}/{maxInventorySlots})");
            return false;
        }

        InventoryItem newItem = null;

        // Resolve item type, sprite, title, description, and behaviors
        if (string.Equals(rewardItemName, "boat", StringComparison.OrdinalIgnoreCase))
        {
            newItem = new InventoryItem(
                "Boat",
                "Allows crossing water and river channels.",
                boatSprite, // Assigns the serialized boatSprite
                ItemType.Boat,
                false 
            );
        }
        else if (string.Equals(rewardItemName, "roadblock", StringComparison.OrdinalIgnoreCase))
        {
            newItem = new InventoryItem(
                "Roadblock",
                "Can be placed on path connections to block unit traversal.",
                roadblockSprite, // Assigns the serialized roadblockSprite
                ItemType.Roadblock,
                true 
            );
        }
        else
        {
            Debug.LogWarning($"[Inventory] Unknown item name: '{rewardItemName}'");
            return false;
        }

        items.Add(newItem);

        if (newItem.itemType == ItemType.Boat)
        {
            PathClickMovement movement = GetComponent<PathClickMovement>();
            if (movement != null)
            {
                movement.UpdateWaterTraversalPermission(true);
            }
        }

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.UpdateReachableHighlights();
        }

        OnInventoryChanged?.Invoke();
        Debug.Log($"<color=green>[Inventory]</color> Added '{newItem.itemTitle}' with icon to {gameObject.name}'s inventory.");
        return true;
    }

    public bool ToggleSlotSelection(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= items.Count) return false;

        InventoryItem item = items[slotIndex];
        
        if (!item.isConsumable) return false;

        if (selectedSlotIndex == slotIndex)
        {
            selectedSlotIndex = -1; // Deselect
        }
        else
        {
            selectedSlotIndex = slotIndex; // Select
        }

        OnInventoryChanged?.Invoke();
        return selectedSlotIndex == slotIndex;
    }

    public void DeselectItem()
    {
        if (selectedSlotIndex != -1)
        {
            selectedSlotIndex = -1;
            OnInventoryChanged?.Invoke();
        }
    }

    public InventoryItem GetSelectedItem()
    {
        if (selectedSlotIndex >= 0 && selectedSlotIndex < items.Count)
        {
            return items[selectedSlotIndex];
        }
        return null;
    }

    public bool RemoveSelectedItem()
    {
        if (selectedSlotIndex >= 0 && selectedSlotIndex < items.Count)
        {
            items.RemoveAt(selectedSlotIndex);
            selectedSlotIndex = -1;
            OnInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    public void IncrementCompletedQuests()
    {
        completedQuestCount++;
    }

    public void AddEnergy(int amount)
    {
        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
        if (TurnManager.Instance != null) TurnManager.Instance.UpdateTurnUI();
        OnEnergyChanged?.Invoke();
    }

    public bool TryDeductEnergy(int amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            if (TurnManager.Instance != null) TurnManager.Instance.UpdateTurnUI();
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
        if (TurnManager.Instance != null) TurnManager.Instance.UpdateTurnUI();
        OnMoneyChanged?.Invoke();
    }

    public bool TryDeductMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            if (TurnManager.Instance != null) TurnManager.Instance.UpdateTurnUI();
            OnMoneyChanged?.Invoke();
            return true;
        }
        return false;
    }
}