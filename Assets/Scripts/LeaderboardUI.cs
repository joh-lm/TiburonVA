using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI Instance { get; private set; }

    [Header("Expand / Collapse Controls")]
    [SerializeField] private Button titleButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private GameObject contentPanel;

    [Header("Row Spawning")]
    [SerializeField] private GameObject rowPrefab;
    [SerializeField] private Transform rowsContainer;

    private bool isExpanded = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (titleButton != null)
        {
            titleButton.onClick.AddListener(ToggleExpand);
        }

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted += HandleTurnStarted;
        }

        RefreshLeaderboard();
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted -= HandleTurnStarted;
        }
    }

    public void ToggleExpand()
    {
        isExpanded = !isExpanded;

        if (contentPanel != null)
        {
            contentPanel.SetActive(isExpanded);
        }

        if (titleText != null)
        {
            titleText.text = isExpanded ? "Leaderboard ▲" : "Leaderboard ▼";
        }
    }

    public void RefreshLeaderboard()
    {
        if (TurnManager.Instance == null || rowsContainer == null || rowPrefab == null) return;

        // Clear existing dynamic rows
        foreach (Transform child in rowsContainer)
        {
            Destroy(child.gameObject);
        }

        // Generate a row for every unit registered in TurnManager
        foreach (PathClickMovement unit in TurnManager.Instance.AllUnits)
        {
            if (unit == null) continue;

            PlayerInventory inventory = unit.GetComponent<PlayerInventory>();
            if (inventory == null) continue;

            GameObject rowObj = Instantiate(rowPrefab, rowsContainer);
            LeaderboardRowUI rowUI = rowObj.GetComponent<LeaderboardRowUI>();

            if (rowUI != null)
            {
                rowUI.SetData(
                    unit.gameObject.name,
                    inventory.CurrentMoney,
                    inventory.CompletedQuestCount,
                    inventory.BusinessesOwned
                );
            }
        }
    }

    private void HandleTurnStarted(PathClickMovement activeUnit)
    {
        RefreshLeaderboard();
    }
}