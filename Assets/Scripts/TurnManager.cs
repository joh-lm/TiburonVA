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
    [SerializeField] private TextMeshProUGUI moneyDisplayText;
    [SerializeField] private TextMeshProUGUI turnDisplayText;
    [SerializeField] private TextMeshProUGUI hoverCostText;
    [SerializeField] private TextMeshProUGUI energyDisplayText;

    [Header("Win Conditions")]
    [SerializeField] private int winMoneyAmount = 100;

    private int currentUnitIndex = 0;
    private NodePlatform[] allNodes;
    private bool wasMoving = false;
    public PathClickMovement CurrentUnit => units.Count > 0 ? units[currentUnitIndex] : null;
    public List<PathClickMovement> AllUnits => units;
    
    public event Action<PathClickMovement> OnTurnStarted;
    public event Action<PathClickMovement> OnPlayerWon;

    public int CurrentEnergy
    {
        get
        {
            if (CurrentUnit == null) return 0;
            PlayerInventory inventory = CurrentUnit.GetComponent<PlayerInventory>();
            return inventory != null ? inventory.CurrentEnergy : 0;
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
        HideHoverCost();
        InitializeTurnSystem();
    }

    private void Update()
    {
        // Monitor movement completion to refresh the range visualizer once unit arrives
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
        
        // Grant turn energy directly to the unit's inventory component
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
            // Pass CurrentUnit so GetReachableNodes evaluates HasBoat permissions!
            HashSet<NodePlatform> reachableNodes = NodePlatform.GetReachableNodes(currentNode, CurrentEnergy, CurrentUnit);
            
            foreach (NodePlatform node in reachableNodes)
            {
                node.SetReachableState(true);
            }
        }
    }

    public void ClearReachableHighlights()
    {
        if (allNodes == null) allNodes = FindObjectsOfType<NodePlatform>();
        foreach (NodePlatform node in allNodes)
        {
            node.SetReachableState(false);
        }
    }

    public void UpdateTurnUI()
    {
        if (turnDisplayText != null && CurrentUnit != null)
        {
            turnDisplayText.text = $"Turn: <b>{CurrentUnit.gameObject.name}</b>";
        }

        if (energyDisplayText != null)
        {
            energyDisplayText.text = $"Energy: <b>{CurrentEnergy}</b>";
        }

        if (moneyDisplayText != null)
        {
            moneyDisplayText.text = $"Money: <b>${CurrentMoney} / ${winMoneyAmount}</b>";
        }
    }

    public void ShowHoverCost(int cost, string targetName)
    {
        if (hoverCostText != null)
        {
            hoverCostText.gameObject.SetActive(true);
            string costColor = CanAfford(cost) ? "green" : "red";
            hoverCostText.text = $"Destination: {targetName} | Cost: <color={costColor}>{cost} Energy</color>";
        }
    }

    public void HideHoverCost()
    {
        if (hoverCostText != null)
        {
            hoverCostText.gameObject.SetActive(false);
        }
    }

    public void CheckWinCondition(PathClickMovement unit)
    {
        if (unit == null) return;

        PlayerInventory inventory = unit.GetComponent<PlayerInventory>();
        if (inventory != null && inventory.CurrentMoney >= winMoneyAmount)
        {
            Debug.Log($"{unit.gameObject.name} has reached ${winMoneyAmount} and won the game!");
            OnPlayerWon?.Invoke(unit);
            // Trigger Victory UI or pause game loop
        }
    }

}