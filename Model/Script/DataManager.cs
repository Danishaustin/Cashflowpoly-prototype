using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public Dictionary<string, ResepData> resepDict {get; private set;}
    public Dictionary<string, BahanMakananData> bahanDict {get; private set;}
    public Dictionary<string, KebutuhanData> kebutuhanDict {get; private set;}
    public Dictionary<string, TujuanFinansialData> tujuanFinansialDict {get; private set;}
    public Dictionary<string, NarasiData> narasiDict {get; private set;}
    public Dictionary<string, DialogKarakterData> dialogKarakterDict {get; private set;}
    public Dictionary<string, QuestData> questDict {get; private set;}
    public Dictionary<string, TargetKebutuhanData> targetKebutuhanDict {get; private set;}
    public List<TargetKebutuhanData> targetKebutuhanList {get; private set;}

    public bool IsDialogKarakterLoaded { get; private set; }
    public string DialogKarakterLoadStatus { get; private set; }

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

        LoadExternalDialogKarakter();

        TextAsset jsonQuest = Resources.Load<TextAsset>("Data/quest");
        var questDatabase = JsonUtility.FromJson<QuestDatabase>(jsonQuest.text);

        questDict = new();

        foreach (var q in questDatabase.quest)
            questDict[q.id] = q;

        TextAsset jsonTargetKebutuhan = Resources.Load<TextAsset>("Data/targetKebutuhan");
        var targetKebutuhanDatabase = JsonUtility.FromJson<TargetKebutuhanDatabase>(jsonTargetKebutuhan.text);

        targetKebutuhanDict = new();
        targetKebutuhanList = new();

        foreach (var tk in targetKebutuhanDatabase.targetKebutuhan)
        {
            targetKebutuhanDict[tk.id] = tk;
            targetKebutuhanList.Add(tk);
        }

    }

    private void LoadExternalDialogKarakter()
    {
        dialogKarakterDict = new();
        IsDialogKarakterLoaded = false;
        DialogKarakterLoadStatus = string.Empty;

        string externalPath = GetExternalDialogKarakterPath();
        
        if (!File.Exists(externalPath))
        {
            DialogKarakterLoadStatus = "⚠ DialogKarakter.json tidak ditemukan: " + externalPath;
            Debug.LogWarning(DialogKarakterLoadStatus);
            return;
        }

        try
        {
            string jsonText = File.ReadAllText(externalPath);
            var dialogKarakterDatabase = JsonUtility.FromJson<DialogKarakterDatabase>(jsonText);

            if (dialogKarakterDatabase == null || dialogKarakterDatabase.dialogKarakter == null)
            {
                DialogKarakterLoadStatus = "⚠ Format dialogKarakter.json tidak valid";
                Debug.LogWarning(DialogKarakterLoadStatus + ": " + externalPath);
                return;
            }

            foreach (var d in dialogKarakterDatabase.dialogKarakter)
            {
                if (d != null && !string.IsNullOrWhiteSpace(d.id))
                {
                    dialogKarakterDict[d.id] = d;
                }
            }

            IsDialogKarakterLoaded = true;
            DialogKarakterLoadStatus = "✓ DialogKarakter berhasil dimuat (" + dialogKarakterDict.Count + " dialog)";
            Debug.Log(DialogKarakterLoadStatus + " dari: " + externalPath);
        }
        catch (System.Exception ex)
        {
            DialogKarakterLoadStatus = "✗ Error: " + ex.Message;
            Debug.LogError(DialogKarakterLoadStatus + " dari: " + externalPath);
        }
    }

    private string GetExternalDialogKarakterPath()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
            // Android: Baca dari /data/data/com.DefaultCompany.Cashflowpoly/files/dialogKarakter.json
            string path = Path.Combine(Application.persistentDataPath, "dialogKarakter.json");
            Debug.Log("Android path: " + path);
            return path;
        #else
            // PC/Editor: Baca dari folder files/ di sebelah Assets
            string appPath = Application.dataPath;
            string appDirectory = Path.HasExtension(appPath) 
                ? Path.GetDirectoryName(appPath) 
                : appPath;
            
            string path = Path.Combine(appDirectory, "files", "dialogKarakter.json");
            Debug.Log("PC path: " + path);
            return path;
        #endif
    }
}
