using UnityEngine;
using UnityEngine.EventSystems;

public class BusinessNode : NodePlatform
{
    [Header("Business Display")]
    [SerializeField] private string businessTitle = "Local Business";

    [Header("Serialized Balance Settings (\"For Now\" Parameters)")]
    [SerializeField] private float beginningPrice = 50f;
    [SerializeField] private float postPurchasePercentage = 0.75f;
    [SerializeField] private float turnIncomePercentage = 0.10f;
    [SerializeField] private float unvisitedValueDecreasePercentage = 0.05f;
    [SerializeField] private float visitedValueIncreasePercentage = 0.15f;

    [Header("Runtime State")]
    [SerializeField] private float currentValue;
    [SerializeField] private PathClickMovement currentOwner;
    [SerializeField] private int turnsSinceLastVisit = 0;

    [Header("UI Component (Screen-Space Panel in Bottom Right)")]
    [SerializeField] private BusinessHoverUI hoverUI;

    public string BusinessTitle => businessTitle;
    public float CurrentValue => currentValue;
    public PathClickMovement CurrentOwner => currentOwner;
    public int TurnsSinceLastVisit => turnsSinceLastVisit;
    public float TurnIncome => currentValue * turnIncomePercentage;
    public bool IsHoveredOrPlayerOnNode { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        currentValue = beginningPrice;
    }

    private void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted += HandleTurnStarted;
        }

        if (hoverUI != null)
        {
            hoverUI.Setup(this);
            hoverUI.HideUI();
        }
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted -= HandleTurnStarted;
        }
    }

    private void Update()
    {
        if (hoverUI != null && hoverUI.gameObject.activeSelf)
        {
            bool isStanding = IsCurrentTurnPlayerStandingHere();
            bool isMouseOver = IsMouseOverNode();

            // Hide business UI if player is not standing on this node AND mouse is not hovering over node
            if (!isStanding && !isMouseOver)
            {
                IsHoveredOrPlayerOnNode = false;
                HideUI();
            }
        }
    }

    #region Turn Logic
    private void HandleTurnStarted(PathClickMovement turnPlayer)
    {
        if (currentOwner != null && turnPlayer == currentOwner)
        {
            // 1. Generate income
            PlayerInventory ownerInventory = currentOwner.GetComponent<PlayerInventory>();
            if (ownerInventory != null)
            {
                ownerInventory.AddMoney(Mathf.RoundToInt(TurnIncome));
            }

            // 2. Increase/Decrease value based on owner presence
            bool isOwnerPresent = (currentOwner.CurrentNode == this);

            if (isOwnerPresent)
            {
                currentValue *= (1f + visitedValueIncreasePercentage);
                turnsSinceLastVisit = 0;
            }
            else
            {
                currentValue *= (1f - unvisitedValueDecreasePercentage);
                turnsSinceLastVisit++;
            }
        }

        // 3. Re-evaluate UI visibility
        CheckPlayerStanding(turnPlayer);
    }
    #endregion

    #region Purchase Logic
    public bool TryBuyBusiness(PathClickMovement buyer)
    {
        if (buyer == null || buyer == currentOwner) return false;

        PlayerInventory buyerInventory = buyer.GetComponent<PlayerInventory>();
        if (buyerInventory == null || buyerInventory.CurrentMoney < currentValue)
        {
            Debug.LogWarning($"{buyer.name} cannot afford {businessTitle} (${currentValue}).");
            return false;
        }

        buyerInventory.TryDeductMoney(Mathf.RoundToInt(currentValue));

        if (currentOwner != null)
        {
            PlayerInventory sellerInventory = currentOwner.GetComponent<PlayerInventory>();
            if (sellerInventory != null)
            {
                sellerInventory.AddMoney(Mathf.RoundToInt(currentValue));
                sellerInventory.BusinessesOwned = Mathf.Max(0, sellerInventory.BusinessesOwned - 1);
            }
        }

        currentOwner = buyer;
        buyerInventory.BusinessesOwned++;

        currentValue *= postPurchasePercentage;
        turnsSinceLastVisit = 0;

        RefreshUI();
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.UpdateTurnUI();
            TurnManager.Instance.CheckWinCondition(buyer);
        }
        return true;
    }
    #endregion

    #region Mouse & Hover Logic
    protected override void OnMouseEnter()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        base.OnMouseEnter(); // Shows normal LocationHoverUI popup above node

        IsHoveredOrPlayerOnNode = true;
        ShowUI();
    }

    protected override void OnMouseExit()
    {
        base.OnMouseExit(); // Hides normal LocationHoverUI popup

        if (!IsCurrentTurnPlayerStandingHere() && !IsMouseOverNode())
        {
            IsHoveredOrPlayerOnNode = false;
            HideUI();
        }
    }

    public override void ProcessNodeInteractions(PathClickMovement unit)
    {
        base.ProcessNodeInteractions(unit);
        CheckPlayerStanding(unit);
    }

    public void CheckPlayerStanding(PathClickMovement player)
    {
        if (player != null && player == TurnManager.Instance.CurrentUnit && IsCurrentTurnPlayerStandingHere())
        {
            IsHoveredOrPlayerOnNode = true;
            ShowUI();
        }
        else if (!IsMouseOverNode())
        {
            IsHoveredOrPlayerOnNode = false;
            HideUI();
        }
    }

    private bool IsCurrentTurnPlayerStandingHere()
    {
        if (TurnManager.Instance == null || TurnManager.Instance.CurrentUnit == null) return false;

        PathClickMovement currentPlayer = TurnManager.Instance.CurrentUnit;

        if (currentPlayer.IsMoving) return false;
        if (currentPlayer.CurrentNode == this) return true;

        Collider[] hitColliders = Physics.OverlapSphere(currentPlayer.transform.position, 0.8f);
        foreach (var col in hitColliders)
        {
            if (col.gameObject == gameObject) return true;
        }

        return false;
    }

    private bool IsMouseOverNode()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform;
    }

    private void ShowUI()
    {
        if (hoverUI != null)
        {
            hoverUI.ShowUI();
            RefreshUI();
        }
    }

    private void HideUI()
    {
        if (hoverUI != null)
        {
            hoverUI.HideUI();
        }
    }

    public void RefreshUI()
    {
        if (hoverUI != null && hoverUI.gameObject.activeSelf)
        {
            hoverUI.UpdateDisplay();
        }
    }
    #endregion
}