using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Registered Units")]
    [SerializeField] private List<PathClickMovement> units = new List<PathClickMovement>();

    [Header("Energy Rules")]
    [SerializeField] private int startingEnergy = 0;
    [SerializeField] private int energyGainPerTurn = 1;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI turnDisplayText;
    [SerializeField] private TextMeshProUGUI hoverCostText;
    [SerializeField] private TextMeshProUGUI energyDisplayText;

    private List<int> playerEnergyPoints = new List<int>();
    private int currentUnitIndex = 0;

    public event Action<PathClickMovement> OnTurnStarted;
    public PathClickMovement CurrentUnit => units.Count > 0 ? units[currentUnitIndex] : null;
    public int CurrentEnergy => playerEnergyPoints.Count > currentUnitIndex ? playerEnergyPoints[currentUnitIndex] : 0;

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
        HideHoverCost();
        InitializeTurnSystem();
    }

    public void InitializeTurnSystem()
    {
        if (units.Count == 0)
        {
            units.AddRange(FindObjectsOfType<PathClickMovement>());
        }

        playerEnergyPoints.Clear();
        for (int i = 0; i < units.Count; i++)
        {
            playerEnergyPoints.Add(startingEnergy);
        }

        if (units.Count > 0)
        {
            StartTurn(0);
        }
    }

    public void StartTurn(int index)
    {
        currentUnitIndex = index % units.Count;
        
        // Grant +1 Energy at start of turn
        playerEnergyPoints[currentUnitIndex] += energyGainPerTurn;

        UpdateTurnUI();
        OnTurnStarted?.Invoke(CurrentUnit);
    }

    public void EndTurn()
    {
        // Block ending turn while active unit is moving
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
        return CurrentEnergy >= cost;
    }

    public void DeductEnergy(int cost)
    {
        if (CanAfford(cost))
        {
            playerEnergyPoints[currentUnitIndex] -= cost;
            UpdateTurnUI();
        }
    }

    private void UpdateTurnUI()
    {
        if (turnDisplayText != null && CurrentUnit != null)
        {
            turnDisplayText.text = $"Turn: <b>{CurrentUnit.gameObject.name}</b>";
        }

        if (energyDisplayText != null)
        {
            energyDisplayText.text = $"Energy: <b>{CurrentEnergy}</b>";
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

    public void RegisterUnit(PathClickMovement unit)
    {
        if (!units.Contains(unit))
        {
            units.Add(unit);
            playerEnergyPoints.Add(startingEnergy);
        }
    }
}