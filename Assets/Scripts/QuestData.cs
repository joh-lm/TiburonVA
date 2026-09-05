using UnityEngine;

public enum QuestGoalType
{
    VisitLocation,  // Go to a specific NodePlatform
    VisitPlayer     // Reach another player's location
}

[CreateAssetMenu(fileName = "NewQuest", menuName = "Tactical Game/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("Basic Info")]
    public string questTitle = "New Quest";
    [TextArea(2, 4)] public string description;

    [Header("Fulfillment Requirements")]
    [Tooltip("Energy required to fulfill/complete the quest once at the destination.")]
    public int energyCostToFulfill = 1;

    [Header("Goal Setup")]
    public QuestGoalType goalType = QuestGoalType.VisitLocation;
    [Tooltip("Target location for VisitLocation goals.")]
    public string targetLocationName; 
    [Tooltip("Target unit name for VisitPlayer goals.")]
    public string targetUnitName;     

    [Header("Rewards")]
    public int rewardMoney = 50;
    public int rewardEnergy = 1;
    public string rewardItemName = ""; // Prepared for future items/vehicles
}