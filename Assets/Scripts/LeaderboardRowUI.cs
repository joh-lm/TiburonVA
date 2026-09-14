using UnityEngine;
using TMPro;

public class LeaderboardRowUI : MonoBehaviour
{
    [Header("Row Columns")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text questsText;
    [SerializeField] private TMP_Text businessesText;

    public void SetData(string name, int money, int quests, int businesses)
    {
        if (playerNameText != null) playerNameText.text = name;
        if (moneyText != null) moneyText.text = $"${money}";
        if (questsText != null) questsText.text = quests.ToString();
        if (businessesText != null) businessesText.text = businesses.ToString();
    }
}