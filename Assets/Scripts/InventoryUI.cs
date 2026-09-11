using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Slot Container")]
    [SerializeField] private Transform slotContainerParent;
    [SerializeField] private InventorySlotUI slotPrefab;

    private List<InventorySlotUI> slotPool = new List<InventorySlotUI>();

    private void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted += HandleTurnStarted;
        }

        RefreshInventoryUI();
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted -= HandleTurnStarted;
        }

        UnbindCurrentInventory();
    }

    private void HandleTurnStarted(PathClickMovement unit)
    {
        RefreshInventoryUI();
    }

    public void RefreshInventoryUI()
    {
        UnbindCurrentInventory();

        PathClickMovement currentUnit = TurnManager.Instance != null ? TurnManager.Instance.CurrentUnit : null;
        if (currentUnit == null) return;

        PlayerInventory inventory = currentUnit.GetComponent<PlayerInventory>();
        if (inventory == null) return;

        inventory.OnInventoryChanged += RefreshInventoryUI;

        int totalSlots = inventory.MaxInventorySlots;
        List<InventoryItem> items = inventory.Items;

        // Ensure UI pool has enough slot instances for max capacity
        while (slotPool.Count < totalSlots)
        {
            InventorySlotUI newSlot = Instantiate(slotPrefab, slotContainerParent);
            slotPool.Add(newSlot);
        }

        // Loop through ALL max slots to render populated or empty UI boxes
        for (int i = 0; i < slotPool.Count; i++)
        {
            if (i < totalSlots)
            {
                slotPool[i].gameObject.SetActive(true);
                InventoryItem item = (i < items.Count) ? items[i] : null;
                
                // Pass item (or null for empty) to slot setup
                slotPool[i].SetupSlot(i, item, inventory); 
            }
            else
            {
                // Disable extra slots if character capacity decreases
                slotPool[i].gameObject.SetActive(false); 
            }
        }
    }

    private void UnbindCurrentInventory()
    {
        PathClickMovement currentUnit = TurnManager.Instance != null ? TurnManager.Instance.CurrentUnit : null;
        if (currentUnit != null)
        {
            PlayerInventory inventory = currentUnit.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.OnInventoryChanged -= RefreshInventoryUI;
            }
        }
    }
}