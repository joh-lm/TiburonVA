using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("UI Indicators")]
    [SerializeField] private GameObject boatIconImage;
    [SerializeField] private TextMeshProUGUI inventoryStatusText;

    private void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted += RefreshInventoryDisplay;
        }
        RefreshInventoryDisplay(TurnManager.Instance != null ? TurnManager.Instance.CurrentUnit : null);
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted -= RefreshInventoryDisplay;
        }
    }

    public void RefreshInventoryDisplay(PathClickMovement currentUnit)
    {
        if (currentUnit == null) return;

        PlayerInventory inv = currentUnit.GetComponent<PlayerInventory>();
        bool hasBoat = inv != null && inv.HasBoat;

        if (boatIconImage != null)
        {
            boatIconImage.SetActive(hasBoat);
        }

        if (inventoryStatusText != null)
        {
            inventoryStatusText.text = $"<b>Inventory:</b> {(hasBoat ? "Boat" : "Empty")}";
        }
    }
}