using System;
using System.Collections.Generic;

[Serializable]
public class QuestData
{
    public string id;
    public string title;
    public string description;
    public string aksi;
    public int target;
    public int rewardCoins;
    public int rewardHappiness;
}

[Serializable]
public class QuestDatabase
{
    public List<QuestData> quest;
}
