using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BusinessHoverUI : MonoBehaviour
{
    [Header("UI Element References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text ownerText;
    [SerializeField] private TMP_Text buyPriceText;
    [SerializeField] private TMP_Text incomeText;
    [SerializeField] private TMP_Text turnsUnvisitedText;
    [SerializeField] private Button buyButton;

    private BusinessNode targetBusiness;

    public void Setup(BusinessNode business)
    {
        targetBusiness = business;
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyButtonClicked);
        }
    }

    public void ShowUI()
    {
        gameObject.SetActive(true);
        UpdateDisplay();
    }

    public void HideUI()
    {
        gameObject.SetActive(false);
    }

    public void UpdateDisplay()
    {
        if (targetBusiness == null) return;

        // Title
        if (titleText != null) titleText.text = targetBusiness.BusinessTitle;

        // Owner Name ("Bank" if unowned)
        if (ownerText != null)
        {
            string ownerName = targetBusiness.CurrentOwner != null ? targetBusiness.CurrentOwner.name : "Bank";
            ownerText.text = $"Owner: {ownerName}";
        }

        // Price, Income & Unvisited Turns
        if (buyPriceText != null) buyPriceText.text = $"Price: ${targetBusiness.CurrentValue:F2}";
        if (incomeText != null) incomeText.text = $"Income/Turn: ${targetBusiness.TurnIncome:F2}";
        if (turnsUnvisitedText != null) turnsUnvisitedText.text = $"Unvisited Turns: {targetBusiness.TurnsSinceLastVisit}";

        // Buy Button Interactability
        if (buyButton != null)
        {
            PathClickMovement currentPlayer = TurnManager.Instance != null ? TurnManager.Instance.CurrentUnit : null;
            bool isPlayerStandingOnNode = currentPlayer != null && currentPlayer.CurrentNode == targetBusiness;
            bool isAlreadyOwner = currentPlayer != null && targetBusiness.CurrentOwner == currentPlayer;

            PlayerInventory inventory = currentPlayer != null ? currentPlayer.GetComponent<PlayerInventory>() : null;
            bool canAfford = inventory != null && inventory.CurrentMoney >= targetBusiness.CurrentValue;

            buyButton.interactable = isPlayerStandingOnNode && !isAlreadyOwner && canAfford;
        }
    }

    private void OnBuyButtonClicked()
    {
        if (targetBusiness == null || TurnManager.Instance == null) return;

        PathClickMovement currentPlayer = TurnManager.Instance.CurrentUnit;
        if (currentPlayer != null)
        {
            targetBusiness.TryBuyBusiness(currentPlayer);
        }
    }
}