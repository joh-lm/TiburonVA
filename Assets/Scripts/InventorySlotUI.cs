using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Slot UI Components")]
    [SerializeField] private Image itemIconImage;
    [SerializeField] private TextMeshProUGUI itemTitleText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private GameObject expandedContentPanel;
    [SerializeField] private Image slotBackgroundBorder;

    [Header("Visual Colors")]
    [SerializeField] private Color defaultBorderColor = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Semi-transparent empty slot
    [SerializeField] private Color hoverBorderColor = Color.white;
    [SerializeField] private Color selectedBorderColor = Color.yellow;

    private int slotIndex;
    private InventoryItem currentItem;
    private PlayerInventory ownerInventory;
    private bool isHovered = false;

    public void SetupSlot(int index, InventoryItem item, PlayerInventory inventory)
    {
        slotIndex = index;
        currentItem = item;
        ownerInventory = inventory;

        // Ensure slot box GameObject remains ACTIVE even if item is null
        gameObject.SetActive(true);

        if (item != null)
        {
            // Populate item details
            if (itemIconImage != null)
            {
                itemIconImage.sprite = item.itemIcon;
                itemIconImage.gameObject.SetActive(item.itemIcon != null);
            }

            if (itemTitleText != null) itemTitleText.text = item.itemTitle;
            if (itemDescriptionText != null) itemDescriptionText.text = item.description;
        }
        else
        {
            // Handle Empty Slot Visuals
            if (itemIconImage != null)
            {
                itemIconImage.gameObject.SetActive(false); // Hide icon
            }

            if (itemTitleText != null) itemTitleText.text = "Empty Slot";
            if (itemDescriptionText != null) itemDescriptionText.text = "No item stored.";
        }

        RefreshStateVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        RefreshStateVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        RefreshStateVisuals();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Only allow clicking/selecting if slot contains a consumable item
        if (ownerInventory != null && currentItem != null && currentItem.isConsumable)
        {
            ownerInventory.ToggleSlotSelection(slotIndex);
        }
    }

    private void RefreshStateVisuals()
    {
        bool isSelected = ownerInventory != null && ownerInventory.SelectedSlotIndex == slotIndex;
        bool shouldExpand = isHovered || isSelected;

        // Show hover expansion even for empty slots
        if (expandedContentPanel != null)
        {
            expandedContentPanel.SetActive(shouldExpand);
        }

        if (slotBackgroundBorder != null)
        {
            if (isSelected)
            {
                slotBackgroundBorder.color = selectedBorderColor;
            }
            else if (isHovered)
            {
                slotBackgroundBorder.color = hoverBorderColor;
            }
            else
            {
                slotBackgroundBorder.color = defaultBorderColor;
            }
        }
    }
}