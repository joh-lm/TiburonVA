using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Registered Units")]
    [SerializeField] private List<PathClickMovement> units = new List<PathClickMovement>();

    [Header("Turn Rules")]
    [SerializeField] private int energyGainPerTurn = 1;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI turnDisplayText;
    [SerializeField] private TextMeshProUGUI statsDisplayText;

    [Header("Win Conditions (All 3 Required)")]
    [SerializeField] private int winMoneyAmount = 100;
    [SerializeField] private int winQuestCount = 3;
    [SerializeField] private int winBusinessCount = 1;

    private int currentUnitIndex = 0;
    private NodePlatform[] allNodes;
    private bool wasMoving = false;

    public PathClickMovement CurrentUnit => units.Count > 0 ? units[currentUnitIndex] : null;
    public List<PathClickMovement> AllUnits => units;
    
    public event Action<PathClickMovement> OnTurnStarted;
    public event Action<PathClickMovement, string> OnPlayerWon;

    public int CurrentEnergy
    {
        get
        {
            if (CurrentUnit == null) return 0;
            PlayerInventory inventory = CurrentUnit.GetComponent<PlayerInventory>();
            return inventory != null ? inventory.CurrentEnergy : 0;
        }
    }

    public int MaxEnergy
    {
        get
        {
            if (CurrentUnit == null) return 10;
            PlayerInventory inventory = CurrentUnit.GetComponent<PlayerInventory>();
            return inventory != null ? inventory.MaxEnergy : 10;
        }
    }

    public int CurrentMoney
    {
        get
        {
            if (CurrentUnit == null) return 0;
            PlayerInventory inventory = CurrentUnit.GetComponent<PlayerInventory>();
            return inventory != null ? inventory.CurrentMoney : 0;
        }
    }

    public int CurrentCompletedQuests
    {
        get
        {
            if (CurrentUnit == null) return 0;
            PlayerInventory inventory = CurrentUnit.GetComponent<PlayerInventory>();
            return inventory != null ? inventory.CompletedQuestCount : 0;
        }
    }

    public int CurrentBusinessesOwned
    {
        get
        {
            if (CurrentUnit == null) return 0;
            PlayerInventory inventory = CurrentUnit.GetComponent<PlayerInventory>();
            return inventory != null ? inventory.BusinessesOwned : 0;
        }
    }

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
        allNodes = FindObjectsOfType<NodePlatform>();
        InitializeTurnSystem();
    }

    private void Update()
    {
        if (wasMoving && !IsUnitBusy())
        {
            wasMoving = false;
            UpdateReachableHighlights();
        }
    }

    public void InitializeTurnSystem()
    {
        if (units.Count == 0)
        {
            units.AddRange(FindObjectsOfType<PathClickMovement>());
        }

        if (units.Count > 0)
        {
            StartTurn(0);
        }
    }

    public void StartTurn(int index)
    {
        currentUnitIndex = index % units.Count;
        
        if (CurrentUnit != null)
        {
            PlayerInventory inventory = CurrentUnit.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.AddEnergy(energyGainPerTurn);
            }
        }

        UpdateTurnUI();
        UpdateReachableHighlights();
        OnTurnStarted?.Invoke(CurrentUnit);
    }

    public void EndTurn()
    {
        if (IsUnitBusy())
        {
            Debug.Log("Cannot end turn while unit is moving!");
            return;
        }

        int nextIndex = (currentUnitIndex + 1) % units.Count;
        StartTurn(nextIndex);
    }

    public bool IsUnitBusy()
    {
        return CurrentUnit != null && CurrentUnit.IsMoving;
    }

    public bool CanAfford(int cost)
    {
        if (CurrentUnit == null) return false;
        PlayerInventory inventory = CurrentUnit.GetComponent<PlayerInventory>();
        return inventory != null && inventory.CanAfford(cost);
    }

    public void UpdateReachableHighlights()
    {
        ClearReachableHighlights();

        if (CurrentUnit == null) return;

        NodePlatform currentNode = NodePlatform.GetNodeAtPosition(CurrentUnit.transform.position);
        if (currentNode != null)
        {
            HashSet<NodePlatform> reachableNodes = NodePlatform.GetReachableNodes(currentNode, CurrentEnergy, CurrentUnit);
            
            foreach (NodePlatform node in reachableNodes)
            {
                if (node != null) node.SetReachableState(true);
            }
        }
    }

    public void ClearReachableHighlights()
    {
        NodePlatform[] nodesInScene = FindObjectsOfType<NodePlatform>();
        foreach (NodePlatform node in nodesInScene)
        {
            if (node != null) node.SetReachableState(false);
        }
    }

    public void UpdateTurnUI()
    {
        if (turnDisplayText != null && CurrentUnit != null)
        {
            turnDisplayText.text = $"Turn: <b>{CurrentUnit.gameObject.name}</b>";
        }

        if (statsDisplayText != null)
        {
            statsDisplayText.text = $"Energy: <b>{CurrentEnergy}/{MaxEnergy}</b> | Balance: <b>${CurrentMoney}/${winMoneyAmount}</b> | Quests: <b>{CurrentCompletedQuests}/{winQuestCount}</b> | Businesses: <b>{CurrentBusinessesOwned}/{winBusinessCount}</b>";
        }
    }

    public void CheckWinCondition(PathClickMovement unit)
    {
        if (unit == null) return;

        PlayerInventory inventory = unit.GetComponent<PlayerInventory>();
        if (inventory == null) return;

        bool hasEnoughMoney = inventory.CurrentMoney >= winMoneyAmount;
        bool hasEnoughQuests = inventory.CompletedQuestCount >= winQuestCount;
        bool hasEnoughBusinesses = inventory.BusinessesOwned >= winBusinessCount;

        // All three conditions must be satisfied to trigger victory
        if (hasEnoughMoney && hasEnoughQuests && hasEnoughBusinesses)
        {
            string reason = $"reached ${winMoneyAmount}, completed {winQuestCount} quests, and owned {winBusinessCount} business(es)";
            Debug.Log($"<color=gold>[VICTORY]</color> {unit.gameObject.name} won by achieving all 3 win conditions!");
            OnPlayerWon?.Invoke(unit, reason);
        }
    }
}