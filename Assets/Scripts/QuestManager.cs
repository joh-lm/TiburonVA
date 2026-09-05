using System;
using System.Collections.Generic;
using UnityEngine;
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

        // Check if a queued quest can now be activated with new energy
        TryAutoPromoteQueuedQuest(currentUnit);

        UpdateUI();
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
    /// Fulfills objective upon reaching target without secondary energy checks.
    /// </summary>
    public void CheckAndFulfillQuest(PathClickMovement unit, NodePlatform currentLocation)
    {
        if (!activeQuests.ContainsKey(unit) || activeQuests[unit] == null) return;

        QuestData currentQuest = activeQuests[unit];
        bool goalReached = false;

        if (currentQuest.goalType == QuestGoalType.VisitLocation)
        {
            if (currentLocation != null && currentLocation.gameObject.name == currentQuest.targetLocationName)
            {
                goalReached = true;
            }
        }
        else if (currentQuest.goalType == QuestGoalType.VisitPlayer)
        {
            Collider[] hits = Physics.OverlapSphere(currentLocation.transform.position, 1.0f);
            foreach (var col in hits)
            {
                PathClickMovement otherUnit = col.GetComponent<PathClickMovement>();
                if (otherUnit != null && otherUnit != unit && otherUnit.gameObject.name == currentQuest.targetUnitName)
                {
                    goalReached = true;
                    break;
                }
            }
        }

        if (goalReached)
        {
            // Award rewards
            AddMoney(unit, currentQuest.rewardMoney);

            if (currentQuest.rewardEnergy > 0)
            {
                TurnManager.Instance.DeductEnergy(-currentQuest.rewardEnergy);
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