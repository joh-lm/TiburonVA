using TMPro;
using UnityEngine;

public class LocationHoverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI infoText;

    private void Awake()
    {
        if (infoText == null)
        {
            infoText = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    private void LateUpdate()
    {
        if (Camera.main == null) return;
        // Face camera flat towards screen just like RoadblockUI
        transform.rotation = Camera.main.transform.rotation;
    }

    public void Setup(string locationName, int cost, bool canAfford, int questCount)
    {
        if (infoText == null) return;

        string costColorHex = canAfford ? "#00FF00" : "#FF0000";
        string questInfo = questCount > 0 ? $"<color=#00FFFF>Quests Available: {questCount}</color>" : "<i>No Quests</i>";

        infoText.text = $"<b>{locationName}</b>\n" +
                        $"Cost: <color={costColorHex}>{cost} Energy</color>\n" +
                        $"{questInfo}";
    }
}