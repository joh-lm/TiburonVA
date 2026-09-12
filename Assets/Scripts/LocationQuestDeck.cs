using System.Collections.Generic;
using UnityEngine;

public class LocationQuestDeck : MonoBehaviour
{
    [Header("Quest Deck")]
    [Tooltip("Pool of quests available at this location. Duplicates of the same QuestData asset are allowed!")]
    [SerializeField] private List<QuestData> locationQuests = new List<QuestData>();

    private List<QuestData> availablePool = new List<QuestData>();

    public bool HasAvailableQuests => availablePool.Count > 0;
    public int AvailableQuestCount => availablePool.Count;

    private void Awake()
    {
        // Copy all entries from the inspector list (including duplicates)
        availablePool = new List<QuestData>(locationQuests);
    }

    /// <summary>
    /// Draws a random quest from this location's remaining pool.
    /// </summary>
    public QuestData DrawQuest()
    {
        if (availablePool.Count == 0)
        {
            Debug.Log($"[Quest Deck] No remaining quests at location: {gameObject.name}");
            return null;
        }

        int randomIndex = Random.Range(0, availablePool.Count);
        QuestData drawnQuest = availablePool[randomIndex];
        
        // Remove only the specific drawn instance from the pool
        availablePool.RemoveAt(randomIndex);

        return drawnQuest;
    }

    /// <summary>
    /// Returns a declined quest back into the available pool.
    /// </summary>
    public void ReturnQuestToDeck(QuestData quest)
    {
        if (quest != null)
        {
            // Simply re-add the instance to the pool (bypassing duplicate checks)
            availablePool.Add(quest);
            Debug.Log($"<color=cyan>[Quest Deck]</color> '{quest.questTitle}' returned to {gameObject.name}'s deck pool. Total remaining: {availablePool.Count}");
        }
    }

}