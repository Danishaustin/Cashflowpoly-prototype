using System.Collections.Generic;

public partial class GameState
{
    // State quest per pemain, hidup selama satu sesi permainan saja dan hanya diubah lewat narasi.
    private readonly Dictionary<int, Dictionary<string, string>> playerQuestStates = new Dictionary<int, Dictionary<string, string>>();

    public List<QuestData> GetQuestList()
    {
        List<QuestData> quests = new List<QuestData>();
        if (DataManager.Instance?.questDict == null)
        {
            return quests;
        }

        foreach (QuestData quest in DataManager.Instance.questDict.Values)
        {
            if (quest != null && !string.IsNullOrWhiteSpace(quest.id))
            {
                quests.Add(quest);
            }
        }

        return quests;
    }

    public string GetQuestState(int player, string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
        {
            return QuestState.BelumAktif;
        }

        if (playerQuestStates.TryGetValue(player, out Dictionary<string, string> questStates)
            && questStates.TryGetValue(questId, out string state))
        {
            return QuestState.Normalize(state);
        }

        return QuestState.BelumAktif;
    }

    public void SetQuestState(int player, string questId, string state)
    {
        if (string.IsNullOrWhiteSpace(questId) || player < 1)
        {
            return;
        }

        if (!playerQuestStates.TryGetValue(player, out Dictionary<string, string> questStates))
        {
            questStates = new Dictionary<string, string>();
            playerQuestStates[player] = questStates;
        }

        questStates[questId] = QuestState.Normalize(state);
    }

    public bool IsQuestInState(int player, string questId, string state)
    {
        return GetQuestState(player, questId) == QuestState.Normalize(state);
    }

    public string GetQuestDisplayName(string questId)
    {
        if (DataManager.Instance?.questDict != null
            && !string.IsNullOrWhiteSpace(questId)
            && DataManager.Instance.questDict.TryGetValue(questId, out QuestData quest)
            && quest != null
            && !string.IsNullOrWhiteSpace(quest.nama))
        {
            return quest.nama;
        }

        return questId ?? string.Empty;
    }
}
