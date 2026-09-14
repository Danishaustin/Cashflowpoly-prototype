using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public Dictionary<string, ResepData> resepDict { get; private set; }
    public Dictionary<string, BahanMakananData> bahanDict { get; private set; }
    public Dictionary<string, KebutuhanData> kebutuhanDict { get; private set; }
    public Dictionary<string, TujuanFinansialData> tujuanFinansialDict { get; private set; }
    public Dictionary<string, NarasiData> narasiDict { get; private set; }
    public Dictionary<string, DialogKarakterData> dialogKarakterDict { get; private set; }
    public Dictionary<string, QuestData> questDict { get; private set; }
    public Dictionary<string, TargetKebutuhanData> targetKebutuhanDict { get; private set; }
    public List<TargetKebutuhanData> targetKebutuhanList { get; private set; }

    public bool IsDialogKarakterLoaded { get; private set; }
    public string DialogKarakterLoadStatus { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        InitializeData();
        NarasiSessionContext.ApplyTo(this);
    }

    private void InitializeData()
    {
        TextAsset jsonResep = Resources.Load<TextAsset>("Data/resep");
        var resepDatabase = JsonUtility.FromJson<ResepDatabase>(jsonResep.text);

        resepDict = new Dictionary<string, ResepData>();
        foreach (var resep in resepDatabase.resep)
        {
            resepDict[resep.nama] = resep;
        }

        TextAsset jsonBahan = Resources.Load<TextAsset>("Data/bahan");
        var bahanDatabase = JsonUtility.FromJson<BahanMakananDatabase>(jsonBahan.text);

        bahanDict = new Dictionary<string, BahanMakananData>();
        foreach (var bahan in bahanDatabase.bahan)
        {
            bahanDict[bahan.nama] = bahan;
        }

        TextAsset jsonKebutuhan = Resources.Load<TextAsset>("Data/kebutuhan");
        var kebutuhanDatabase = JsonUtility.FromJson<KebutuhanDatabase>(jsonKebutuhan.text);

        kebutuhanDict = new Dictionary<string, KebutuhanData>();
        foreach (var kebutuhan in kebutuhanDatabase.kebutuhan)
        {
            kebutuhanDict[kebutuhan.nama] = kebutuhan;
        }

        TextAsset jsonTujuanFinansial = Resources.Load<TextAsset>("Data/tujuanFinansial");
        var tujuanFinansialDatabase = JsonUtility.FromJson<TujuanFinansialDatabase>(jsonTujuanFinansial.text);

        tujuanFinansialDict = new Dictionary<string, TujuanFinansialData>();
        foreach (var tujuan in tujuanFinansialDatabase.tujuanFinansial)
        {
            tujuanFinansialDict[tujuan.nama] = tujuan;
        }

        TextAsset jsonNarasi = Resources.Load<TextAsset>("Data/narasi");
        var narasiDatabase = JsonUtility.FromJson<NarasiDatabase>(jsonNarasi.text);

        narasiDict = new Dictionary<string, NarasiData>();
        foreach (var narasi in narasiDatabase.narasi)
        {
            narasiDict[narasi.nama] = narasi;
        }

        LoadDialogKarakter();

        TextAsset jsonQuest = Resources.Load<TextAsset>("Data/quest");
        var questDatabase = JsonUtility.FromJson<QuestDatabase>(jsonQuest.text);

        questDict = new Dictionary<string, QuestData>();
        foreach (var quest in questDatabase.quest)
        {
            questDict[quest.id] = quest;
        }

        TextAsset jsonTargetKebutuhan = Resources.Load<TextAsset>("Data/targetKebutuhan");
        var targetKebutuhanDatabase = JsonUtility.FromJson<TargetKebutuhanDatabase>(jsonTargetKebutuhan.text);

        targetKebutuhanDict = new Dictionary<string, TargetKebutuhanData>();
        targetKebutuhanList = new List<TargetKebutuhanData>();

        foreach (var targetKebutuhan in targetKebutuhanDatabase.targetKebutuhan)
        {
            targetKebutuhanDict[targetKebutuhan.id] = targetKebutuhan;
            targetKebutuhanList.Add(targetKebutuhan);
        }
    }

    private void LoadDialogKarakter()
    {
        dialogKarakterDict = new Dictionary<string, DialogKarakterData>();
        IsDialogKarakterLoaded = false;
        DialogKarakterLoadStatus = string.Empty;

        TextAsset jsonDialogKarakter = Resources.Load<TextAsset>("Data/dialogKarakter");
        if (jsonDialogKarakter == null)
        {
            DialogKarakterLoadStatus = "WARN: dialogKarakter.json tidak ditemukan di Resources/Data.";
            Debug.LogWarning(DialogKarakterLoadStatus);
            return;
        }

        try
        {
            var database = JsonUtility.FromJson<DialogKarakterDatabase>(jsonDialogKarakter.text);
            if (database == null || database.dialogKarakter == null)
            {
                DialogKarakterLoadStatus = "WARN: Format dialogKarakter.json tidak valid";
                Debug.LogWarning(DialogKarakterLoadStatus);
                return;
            }

            foreach (var dialog in database.dialogKarakter)
            {
                if (dialog != null && !string.IsNullOrWhiteSpace(dialog.id))
                {
                    dialogKarakterDict[dialog.id] = dialog;
                }
            }

            IsDialogKarakterLoaded = true;
            DialogKarakterLoadStatus = "OK: DialogKarakter berhasil dimuat (" + dialogKarakterDict.Count + " dialog)";
            Debug.Log(DialogKarakterLoadStatus);
        }
        catch (System.Exception ex)
        {
            DialogKarakterLoadStatus = "ERROR: " + ex.Message;
            Debug.LogError(DialogKarakterLoadStatus);
        }
    }

    public void OverrideDialogKarakter(DialogKarakterDatabase database, string sourceName)
    {
        dialogKarakterDict = new Dictionary<string, DialogKarakterData>();
        IsDialogKarakterLoaded = false;
        DialogKarakterLoadStatus = string.Empty;

        if (database == null || database.dialogKarakter == null)
        {
            DialogKarakterLoadStatus = "WARN: Data narasi " + sourceName + " tidak valid.";
            Debug.LogWarning(DialogKarakterLoadStatus);
            return;
        }

        foreach (DialogKarakterData dialog in database.dialogKarakter)
        {
            if (dialog != null && !string.IsNullOrWhiteSpace(dialog.id))
            {
                dialogKarakterDict[dialog.id] = dialog;
            }
        }

        IsDialogKarakterLoaded = true;
        DialogKarakterLoadStatus = "OK: Narasi " + sourceName + " dimuat (" + dialogKarakterDict.Count + " dialog)";
        Debug.Log(DialogKarakterLoadStatus);
    }
}

