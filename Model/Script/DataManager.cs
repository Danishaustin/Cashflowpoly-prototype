using UnityEngine;
using System.Collections.Generic;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public Dictionary<string, ResepData> resepDict {get; private set;}
    public Dictionary<string, BahanMakananData> bahanDict {get; private set;}
    public Dictionary<string, KebutuhanData> kebutuhanDict {get; private set;}
    public Dictionary<string, TujuanFinansialData> tujuanFinansialDict {get; private set;}
    public Dictionary<string, NarasiData> narasiDict {get; private set;}
    public Dictionary<string, QuestData> questDict {get; private set;}
    public Dictionary<string, TargetKebutuhanData> targetKebutuhanDict {get; private set;}

    void Awake()
    {
        if (Instance == null)
            Instance = this;

        InitializeData();
    }

    private void InitializeData()
    {
        TextAsset jsonResep = Resources.Load<TextAsset>("Data/resep");
        var resepDatabase = JsonUtility.FromJson<ResepDatabase>(jsonResep.text);

        resepDict = new();
        foreach (var r in resepDatabase.resep)
            resepDict[r.nama] = r;

        TextAsset jsonBahan = Resources.Load<TextAsset>("Data/bahan");
        var bahanDatabase = JsonUtility.FromJson<BahanMakananDatabase>(jsonBahan.text);

        bahanDict = new();
        foreach (var b in bahanDatabase.bahan)
            bahanDict[b.nama] = b;

        TextAsset jsonKebutuhan = Resources.Load<TextAsset>("Data/kebutuhan");
        var kebutuhanDatabase = JsonUtility.FromJson<KebutuhanDatabase>(jsonKebutuhan.text);

        kebutuhanDict = new();
        foreach (var k in kebutuhanDatabase.kebutuhan)
            kebutuhanDict[k.nama] = k;

        TextAsset jsonTujuanFinansial = Resources.Load<TextAsset>("Data/tujuanFinansial");
        var tujuanFinansialDatabase = JsonUtility.FromJson<TujuanFinansialDatabase>(jsonTujuanFinansial.text);
        
        tujuanFinansialDict = new();

        foreach (var t in tujuanFinansialDatabase.tujuanFinansial)
            tujuanFinansialDict[t.nama] = t;

        TextAsset jsonNarasi = Resources.Load<TextAsset>("Data/narasi");
        var narasiDatabase = JsonUtility.FromJson<NarasiDatabase>(jsonNarasi.text);

        narasiDict = new();

        foreach (var n in narasiDatabase.narasi)
            narasiDict[n.nama] = n;

        TextAsset jsonQuest = Resources.Load<TextAsset>("Data/quest");
        var questDatabase = JsonUtility.FromJson<QuestDatabase>(jsonQuest.text);

        questDict = new();

        foreach (var q in questDatabase.quest)
            questDict[q.id] = q;

        TextAsset jsonTargetKebutuhan = Resources.Load<TextAsset>("Data/targetKebutuhan");
        var targetKebutuhanDatabase = JsonUtility.FromJson<TargetKebutuhanDatabase>(jsonTargetKebutuhan.text);

        targetKebutuhanDict = new();

        foreach (var tk in targetKebutuhanDatabase.targetKebutuhan)
            targetKebutuhanDict[tk.id] = tk;

    }
}
