using TMPro;
using UnityEngine;

public class RoadblockUI : MonoBehaviour
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
        // Matches the camera's exact view rotation so the canvas faces flat towards the screen
        transform.rotation = Camera.main.transform.rotation;
    }

    public void Setup(string locationText, int energyCost, int moneyCost, bool canAfford)
    {
        if (infoText == null) return;

        string colorHex = canAfford ? "#00FF00" : "#FF0000";
        infoText.text = $"<b>Blocked Route:</b>\n{locationText}\n\n" +
                        $"<b>Cost to Clear:</b>\n" +
                        $"<color={colorHex}>{energyCost} Energy | ${moneyCost}</color>";
    }
}