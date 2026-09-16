using System;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public Dictionary<string, ResepData> resepDict { get; private set; }
    public Dictionary<string, BahanMakananData> bahanDict { get; private set; }
    public Dictionary<string, KebutuhanData> kebutuhanDict { get; private set; }
    public Dictionary<string, TujuanFinansialData> tujuanFinansialDict { get; private set; }
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
        QuestSessionContext.ApplyTo(this);
    }

    // Setiap file data dibaca terpisah: satu file yang hilang tidak boleh membatalkan pemuatan file lain
    // maupun penerapan paket narasi yang dijalankan setelah InitializeData.
    private void InitializeData()
    {
        resepDict = new Dictionary<string, ResepData>();
        ResepDatabase resepDatabase = LoadDatabase<ResepDatabase>("Data/resep");
        if (resepDatabase?.resep != null)
        {
            foreach (ResepData resep in resepDatabase.resep)
            {
                if (resep != null && !string.IsNullOrWhiteSpace(resep.nama))
                {
                    resepDict[resep.nama] = resep;
                }
            }
        }

        bahanDict = new Dictionary<string, BahanMakananData>();
        BahanMakananDatabase bahanDatabase = LoadDatabase<BahanMakananDatabase>("Data/bahan");
        if (bahanDatabase?.bahan != null)
        {
            foreach (BahanMakananData bahan in bahanDatabase.bahan)
            {
                if (bahan != null && !string.IsNullOrWhiteSpace(bahan.nama))
                {
                    bahanDict[bahan.nama] = bahan;
                }
            }
        }

        kebutuhanDict = new Dictionary<string, KebutuhanData>();
        KebutuhanDatabase kebutuhanDatabase = LoadDatabase<KebutuhanDatabase>("Data/kebutuhan");
        if (kebutuhanDatabase?.kebutuhan != null)
        {
            foreach (KebutuhanData kebutuhan in kebutuhanDatabase.kebutuhan)
            {
                if (kebutuhan != null && !string.IsNullOrWhiteSpace(kebutuhan.nama))
                {
                    kebutuhanDict[kebutuhan.nama] = kebutuhan;
                }
            }
        }

        tujuanFinansialDict = new Dictionary<string, TujuanFinansialData>();
        TujuanFinansialDatabase tujuanFinansialDatabase = LoadDatabase<TujuanFinansialDatabase>("Data/tujuanFinansial");
        if (tujuanFinansialDatabase?.tujuanFinansial != null)
        {
            foreach (TujuanFinansialData tujuan in tujuanFinansialDatabase.tujuanFinansial)
            {
                if (tujuan != null && !string.IsNullOrWhiteSpace(tujuan.nama))
                {
                    tujuanFinansialDict[tujuan.nama] = tujuan;
                }
            }
        }

        LoadDialogKarakter();

        questDict = new Dictionary<string, QuestData>();
        QuestDatabase questDatabase = LoadDatabase<QuestDatabase>("Data/quest");
        if (questDatabase?.quest != null)
        {
            foreach (QuestData quest in questDatabase.quest)
            {
                if (quest != null && !string.IsNullOrWhiteSpace(quest.id))
                {
                    questDict[quest.id] = quest;
                }
            }
        }

        targetKebutuhanDict = new Dictionary<string, TargetKebutuhanData>();
        targetKebutuhanList = new List<TargetKebutuhanData>();
        TargetKebutuhanDatabase targetKebutuhanDatabase = LoadDatabase<TargetKebutuhanDatabase>("Data/targetKebutuhan");
        if (targetKebutuhanDatabase?.targetKebutuhan != null)
        {
            foreach (TargetKebutuhanData targetKebutuhan in targetKebutuhanDatabase.targetKebutuhan)
            {
                if (targetKebutuhan != null && !string.IsNullOrWhiteSpace(targetKebutuhan.id))
                {
                    targetKebutuhanDict[targetKebutuhan.id] = targetKebutuhan;
                    targetKebutuhanList.Add(targetKebutuhan);
                }
            }
        }
    }

    private static T LoadDatabase<T>(string resourcePath) where T : class
    {
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
        {
            Debug.LogWarning("File data tidak ditemukan: Resources/" + resourcePath);
            return null;
        }

        try
        {
            return JsonUtility.FromJson<T>(asset.text);
        }
        catch (Exception ex)
        {
            Debug.LogError("Gagal membaca " + resourcePath + ": " + ex.Message);
            return null;
        }
    }

    private void LoadDialogKarakter()
    {
        DialogKarakterDatabase database = LoadDatabase<DialogKarakterDatabase>("Data/dialogKarakter");
        if (database == null)
        {
            dialogKarakterDict = new Dictionary<string, DialogKarakterData>();
            IsDialogKarakterLoaded = false;
            DialogKarakterLoadStatus = "WARN: dialogKarakter.json tidak ditemukan atau tidak valid.";
            Debug.LogWarning(DialogKarakterLoadStatus);
            return;
        }

        ApplyDialogKarakterDatabase(database, "dialogKarakter.json");
    }

    // Paket quest yang dipilih di Home menggantikan isi quest bawaan.
    public void OverrideQuest(QuestDatabase database, string sourceName)
    {
        questDict = new Dictionary<string, QuestData>();
        if (database?.quest == null)
        {
            Debug.LogWarning("WARN: Data quest " + sourceName + " tidak valid.");
            return;
        }

        foreach (QuestData quest in database.quest)
        {
            if (quest == null || string.IsNullOrWhiteSpace(quest.id))
            {
                continue;
            }

            if (questDict.ContainsKey(quest.id))
            {
                Debug.LogWarning("Quest dengan id ganda pada " + sourceName + ": " + quest.id);
            }

            questDict[quest.id] = quest;
        }

        Debug.Log("OK: Quest " + sourceName + " dimuat (" + questDict.Count + " quest)");
    }

    public void OverrideDialogKarakter(DialogKarakterDatabase database, string sourceName)
    {
        if (database == null)
        {
            dialogKarakterDict = new Dictionary<string, DialogKarakterData>();
            IsDialogKarakterLoaded = false;
            DialogKarakterLoadStatus = "WARN: Data narasi " + sourceName + " tidak valid.";
            Debug.LogWarning(DialogKarakterLoadStatus);
            return;
        }

        ApplyDialogKarakterDatabase(database, "Narasi " + sourceName);
    }

    private void ApplyDialogKarakterDatabase(DialogKarakterDatabase database, string sourceName)
    {
        dialogKarakterDict = new Dictionary<string, DialogKarakterData>();
        IsDialogKarakterLoaded = false;
        DialogKarakterLoadStatus = string.Empty;

        if (database.dialogKarakter == null)
        {
            DialogKarakterLoadStatus = "WARN: Format " + sourceName + " tidak valid.";
            Debug.LogWarning(DialogKarakterLoadStatus);
            return;
        }

        foreach (DialogKarakterData dialog in database.dialogKarakter)
        {
            if (dialog == null || string.IsNullOrWhiteSpace(dialog.id))
            {
                continue;
            }

            // id ganda membuat penanda "sudah diputar" bertabrakan, jadi yang terakhir dipakai dan sisanya dilaporkan.
            if (dialogKarakterDict.ContainsKey(dialog.id))
            {
                Debug.LogWarning("Dialog dengan id ganda pada " + sourceName + ": " + dialog.id);
            }

            dialogKarakterDict[dialog.id] = dialog;
        }

        IsDialogKarakterLoaded = true;
        DialogKarakterLoadStatus = "OK: " + sourceName + " dimuat (" + dialogKarakterDict.Count + " dialog)";
        Debug.Log(DialogKarakterLoadStatus);
    }
}
