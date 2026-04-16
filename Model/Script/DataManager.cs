using UnityEngine;
using System.Collections.Generic;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public Dictionary<string, ResepData> resepDict {get; private set;}
    public Dictionary<string, BahanMakananData> bahanDict {get; private set;}
    public Dictionary<string, KebutuhanData> kebutuhanDict {get; private set;}
    public Dictionary<string, TujuanFinansialData> tujuanFinansialDict {get; private set;}

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

    }
}