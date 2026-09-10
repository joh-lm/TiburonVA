using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Win Condition")]
    [SerializeField] private int winMoneyAmount = 200;


    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI activeQuestText;
    [SerializeField] private TextMeshProUGUI queuedQuestText;
    [SerializeField] private TextMeshProUGUI moneyDisplayText;

    [Header("Quest Completed UI Panel References")]
    [SerializeField] private GameObject questCompletedPanel;
    [SerializeField] private TextMeshProUGUI completedTitleText;
    [SerializeField] private TextMeshProUGUI completedDescriptionText;
    [SerializeField] private TextMeshProUGUI completedRewardsText;
    [SerializeField] private Button dismissCompletedButton;

    [Header("Quest Offer UI Prompt")]
    [SerializeField] private GameObject questPromptPanel;
    [SerializeField] private TextMeshProUGUI promptTitleText;
    [SerializeField] private TextMeshProUGUI promptDescriptionText;
    [SerializeField] private TextMeshProUGUI promptRewardText;

    private Dictionary<PathClickMovement, int> playerMoney = new Dictionary<PathClickMovement, int>();
    private Dictionary<PathClickMovement, QuestData> activeQuests = new Dictionary<PathClickMovement, QuestData>();
    private Dictionary<PathClickMovement, QuestData> queuedQuests = new Dictionary<PathClickMovement, QuestData>();

    private QuestData pendingQuest;
    private PathClickMovement pendingUnit;
    private LocationQuestDeck pendingSourceDeck;

    public event Action<PathClickMovement, int> OnMoneyChanged;
    public event Action<PathClickMovement> OnGameWon;
    private Action onDismissCallback;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (dismissCompletedButton != null)
        {
            // Bind button click listener
            dismissCompletedButton.onClick.AddListener(OnDismissCompletedPanelClicked);
        }

        if (questCompletedPanel != null)
        {
            questCompletedPanel.SetActive(false);
        }
    }

    private void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted += HandleTurnStarted;
        }
        if (questPromptPanel != null) questPromptPanel.SetActive(false);
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted -= HandleTurnStarted;
        }
    }

    private void HandleTurnStarted(PathClickMovement currentUnit)
    {
        if (!playerMoney.ContainsKey(currentUnit))
        {
            playerMoney[currentUnit] = 0;
        }

        // Check if a queued quest can now be activated with new turn energy
        TryAutoPromoteQueuedQuest(currentUnit);

        UpdateUI();
    }

    public bool HasFulfilledQuest(PathClickMovement unit, NodePlatform currentLocation)
    {
        if (unit == null || !activeQuests.ContainsKey(unit) || activeQuests[unit] == null) return false;

        QuestData currentQuest = activeQuests[unit];
        bool goalReached = false;

        if (currentQuest.goalType == QuestGoalType.VisitLocation)
        {
            if (currentLocation != null)
            {
                goalReached = string.Equals(currentLocation.name, currentQuest.targetLocationName, StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(currentLocation.gameObject.name, currentQuest.targetLocationName, StringComparison.OrdinalIgnoreCase);
            }
        }
        else if (currentQuest.goalType == QuestGoalType.VisitPlayer)
        {
            if (TurnManager.Instance != null)
            {
                foreach (PathClickMovement playerUnit in TurnManager.Instance.AllUnits)
                {
                    if (playerUnit != null && playerUnit != unit && string.Equals(playerUnit.gameObject.name, currentQuest.targetUnitName, StringComparison.OrdinalIgnoreCase))
                    {
                        NodePlatform targetUnitNode = NodePlatform.GetNodeAtPosition(playerUnit.transform.position);
                        if (targetUnitNode == currentLocation)
                        {
                            goalReached = true;
                            break;
                        }
                    }
                }
            }
        }
        return goalReached;
    }

    public void PresentQuestCompletedUI(PathClickMovement unit, NodePlatform location, Action onDismissed)
    {
        if (unit == null || !activeQuests.ContainsKey(unit)) return;

        QuestData completedQuest = activeQuests[unit];

        // 1. Populate Title and Description
        if (completedTitleText != null) 
            completedTitleText.text = $"Quest Completed: {completedQuest.questTitle}!";
        
        if (completedDescriptionText != null) 
            completedDescriptionText.text = completedQuest.description;

        // 2. Build Dynamic Reward String from Scriptable Object
        if (completedRewardsText != null)
        {
            string formattedRewards = BuildRewardString(completedQuest);
            completedRewardsText.text = formattedRewards;
        }

        // 3. Store callback for panel dismissal
        onDismissCallback = onDismissed;

        // 4. Clear active quest from tracking dictionary
        activeQuests.Remove(unit);

        // 5. Display Panel
        if (questCompletedPanel != null)
        {
            questCompletedPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Builds a comma-separated reward string ignoring 0 money/energy or empty item names.
    /// </summary>
    private string BuildRewardString(QuestData quest)
    {
        if (quest == null) return "Rewards: None";

        List<string> rewardsList = new List<string>();

        // Check reward money
        if (quest.rewardMoney > 0)
        {
            rewardsList.Add($"+${quest.rewardMoney}");
        }

        // Check reward energy
        if (quest.rewardEnergy > 0)
        {
            rewardsList.Add($"+{quest.rewardEnergy} Energy");
        }

        // Check reward item name
        if (!string.IsNullOrEmpty(quest.rewardItemName))
        {
            rewardsList.Add(quest.rewardItemName);
        }

        if (rewardsList.Count == 0)
        {
            return "Rewards: None";
        }

        return "Rewards: " + string.Join(", ", rewardsList);
    }

    private void OnDismissCompletedPanelClicked()
    {
        // Hide the Quest Completed UI panel
        if (questCompletedPanel != null)
        {
            questCompletedPanel.SetActive(false);
        }

        // Execute the delayed callback to prompt the new Quest Offer UI
        Action callbackToExecute = onDismissCallback;
        onDismissCallback = null; // Clear reference
        
        callbackToExecute?.Invoke();
    }

    public bool CanDrawQuest(PathClickMovement unit)
    {
        bool hasActive = activeQuests.ContainsKey(unit) && activeQuests[unit] != null;
        bool hasQueued = queuedQuests.ContainsKey(unit) && queuedQuests[unit] != null;

        return !hasActive || !hasQueued;
    }

    public void PresentQuestOffer(PathClickMovement unit, QuestData quest, LocationQuestDeck sourceDeck = null)
    {
        pendingQuest = quest;
        pendingUnit = unit;
        pendingSourceDeck = sourceDeck;

        if (questPromptPanel != null)
        {
            questPromptPanel.SetActive(true);

            if (promptTitleText != null) promptTitleText.text = quest.questTitle;
            if (promptDescriptionText != null) promptDescriptionText.text = quest.description;
            if (promptRewardText != null) 
                promptRewardText.text = $"Reward: ${quest.rewardMoney} | Activation Cost: {quest.energyCostToFulfill} Energy";
        }
        else
        {
            Debug.LogWarning("[QuestManager] No Quest Prompt Panel assigned! Auto-accepting quest.");
            AcceptPendingQuest();
        }
    }

    public void AcceptPendingQuest()
    {
        if (pendingQuest != null && pendingUnit != null)
        {
            AssignQuestWithEnergyCheck(pendingUnit, pendingQuest);
        }
        CloseQuestPrompt();
    }

    public void DeclinePendingQuest()
    {
        // Return quest back to the source location's deck pool
        if (pendingQuest != null && pendingSourceDeck != null)
        {
            pendingSourceDeck.ReturnQuestToDeck(pendingQuest);
        }
        else
        {
            Debug.LogWarning("[QuestManager] Could not return quest to deck because pendingSourceDeck or pendingQuest is null.");
        }
        CloseQuestPrompt();
    }

    private void CloseQuestPrompt()
    {
        pendingQuest = null;
        pendingUnit = null;
        pendingSourceDeck = null;
        if (questPromptPanel != null) questPromptPanel.SetActive(false);
    }

    /// <summary>
    /// Assigns quest based on energy balance. Deducts energy immediately if activated.
    /// </summary>
    public void AssignQuestWithEnergyCheck(PathClickMovement unit, QuestData quest)
    {
        if (quest == null) return;

        bool canAffordEnergy = TurnManager.Instance.CanAfford(quest.energyCostToFulfill);
        bool hasActive = activeQuests.ContainsKey(unit) && activeQuests[unit] != null;

        // Condition A: If no active quest AND unit has energy to ACTIVATE -> Deduct energy and make ACTIVE
        if (!hasActive && canAffordEnergy)
        {
            TurnManager.Instance.DeductEnergy(quest.energyCostToFulfill);
            activeQuests[unit] = quest;
            Debug.Log($"[Quest Activated] {quest.questTitle} (Deducted {quest.energyCostToFulfill} Energy)");
        }
        // Condition B: Otherwise (insufficient energy or active slot occupied) -> Send to QUEUE
        else if (!queuedQuests.ContainsKey(unit) || queuedQuests[unit] == null)
        {
            queuedQuests[unit] = quest;
            Debug.Log($"[Quest Queued] {quest.questTitle} (Waiting for energy or open slot)");
        }

        UpdateUI();
    }

    /// <summary>
    /// Promotes a queued quest to active once energy is gained.
    /// </summary>
    public void TryAutoPromoteQueuedQuest(PathClickMovement unit)
    {
        bool hasActive = activeQuests.ContainsKey(unit) && activeQuests[unit] != null;
        bool hasQueued = queuedQuests.ContainsKey(unit) && queuedQuests[unit] != null;

        if (!hasActive && hasQueued)
        {
            QuestData queued = queuedQuests[unit];
            
            // Check if player can afford to activate it now
            if (TurnManager.Instance.CanAfford(queued.energyCostToFulfill))
            {
                TurnManager.Instance.DeductEnergy(queued.energyCostToFulfill);
                activeQuests[unit] = queued;
                queuedQuests[unit] = null;
                Debug.Log($"[Quest Auto-Activated] {queued.questTitle} promoted from queue! (Deducted {queued.energyCostToFulfill} Energy)");
            }
        }
    }

    /// <summary>
    /// Fulfills objective upon reaching target location or player unit.
    /// </summary>
    public void CheckAndFulfillQuest(PathClickMovement unit, NodePlatform currentLocation)
    {
        if (!activeQuests.ContainsKey(unit) || activeQuests[unit] == null) return;

        QuestData currentQuest = activeQuests[unit];
        bool goalReached = false;

        if (currentQuest.goalType == QuestGoalType.VisitLocation)
        {
            if (currentLocation != null)
            {
                goalReached = string.Equals(currentLocation.name, currentQuest.targetLocationName, StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(currentLocation.gameObject.name, currentQuest.targetLocationName, StringComparison.OrdinalIgnoreCase);
            }
        }
        else if (currentQuest.goalType == QuestGoalType.VisitPlayer)
        {
            if (TurnManager.Instance != null)
            {
                foreach (PathClickMovement playerUnit in TurnManager.Instance.AllUnits)
                {
                    if (playerUnit != null && playerUnit != unit && string.Equals(playerUnit.gameObject.name, currentQuest.targetUnitName, StringComparison.OrdinalIgnoreCase))
                    {
                        NodePlatform targetUnitNode = NodePlatform.GetNodeAtPosition(playerUnit.transform.position);
                        if (targetUnitNode == currentLocation)
                        {
                            goalReached = true;
                            break;
                        }
                    }
                }
            }
        }

        if (goalReached)
        {
            // Award rewards
            AddMoney(unit, currentQuest.rewardMoney);

            if (currentQuest.rewardEnergy > 0)
            {
                TurnManager.Instance.AddEnergy(currentQuest.rewardEnergy);
            }

            Debug.Log($"[Quest Completed] {currentQuest.questTitle}! Granted ${currentQuest.rewardMoney}.");

            activeQuests[unit] = null;

            // Immediately check if a queued quest can activate
            TryAutoPromoteQueuedQuest(unit);
            UpdateUI();
        }
    }

    public void AddMoney(PathClickMovement unit, int amount)
    {
        if (!playerMoney.ContainsKey(unit)) playerMoney[unit] = 0;
        
        playerMoney[unit] += amount;
        OnMoneyChanged?.Invoke(unit, playerMoney[unit]);
        UpdateUI();

        if (playerMoney[unit] >= winMoneyAmount)
        {
            Debug.Log($"<color=gold>PLAYER WIN!</color> {unit.gameObject.name} reached ${playerMoney[unit]}!");
            OnGameWon?.Invoke(unit);
        }
    }

    public int GetMoney(PathClickMovement unit)
    {
        return playerMoney.ContainsKey(unit) ? playerMoney[unit] : 0;
    }

    /// <summary>
    /// Deducts a specified amount of money from the unit's balance.
    /// </summary>
    public void DeductMoney(PathClickMovement unit, int amount)
    {
        if (unit == null || amount <= 0) return;

        if (playerMoney.ContainsKey(unit))
        {
            playerMoney[unit] = Mathf.Max(0, playerMoney[unit] - amount);
            Debug.Log($"<color=yellow>[Economy]</color> Deducted ${amount} from {unit.name}. Remaining balance: ${playerMoney[unit]}");
            UpdateUI();
        }
    }

    public void UpdateUI()
    {
        PathClickMovement currentUnit = TurnManager.Instance != null ? TurnManager.Instance.CurrentUnit : null;
        if (currentUnit == null) return;

        if (moneyDisplayText != null)
            moneyDisplayText.text = $"Money: <b>${GetMoney(currentUnit)} / ${winMoneyAmount}</b>";

        if (activeQuestText != null)
        {
            QuestData active = activeQuests.ContainsKey(currentUnit) ? activeQuests[currentUnit] : null;
            activeQuestText.text = active != null ? $"Active Quest: <b>{active.questTitle}</b>" : "Active Quest: <i>None</i>";
        }

        if (queuedQuestText != null)
        {
            QuestData queued = queuedQuests.ContainsKey(currentUnit) ? queuedQuests[currentUnit] : null;
            queuedQuestText.text = queued != null ? $"Queued Quest: <b>{queued.questTitle}</b>" : "Queued Quest: <i>None</i>";
        }
    }
}