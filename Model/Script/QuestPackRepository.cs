using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

// Paket quest disimpan terpisah dari paket narasi, dengan alur yang sama: UGS lebih dulu, lalu file lokal,
// lalu bawaan Resources.
public static class QuestPackRepository
{
    private const string ResourceDirectory = "Data/Quest";
    private const string ManifestResourcePath = ResourceDirectory + "/manifest";
    private const string QuestFallbackResourcePath = "Data/quest";
    private const string CloudKeyPrefix = "quest_";
    private const string ManifestCloudKey = CloudKeyPrefix + "manifest.json";

    public static string LastCloudWarningMessage { get; private set; }

    public static void ClearLastCloudWarning()
    {
        LastCloudWarningMessage = string.Empty;
    }

    public static QuestManifestData LoadManifest()
    {
        string manifestPath = GetManifestFilePath();
        if (File.Exists(manifestPath))
        {
            return NormalizeManifest(JsonUtility.FromJson<QuestManifestData>(File.ReadAllText(manifestPath)));
        }

        TextAsset manifestAsset = Resources.Load<TextAsset>(ManifestResourcePath);
        if (manifestAsset != null)
        {
            return NormalizeManifest(JsonUtility.FromJson<QuestManifestData>(manifestAsset.text));
        }

        return new QuestManifestData { questPacks = new List<QuestPackData>() };
    }

    public static async Task<QuestManifestData> LoadManifestAsync()
    {
        string cloudJson = await TryLoadCloudTextAsync(ManifestCloudKey);
        if (!string.IsNullOrWhiteSpace(cloudJson))
        {
            QuestManifestData cloudManifest = NormalizeManifest(JsonUtility.FromJson<QuestManifestData>(cloudJson));
            SaveManifest(cloudManifest);
            return cloudManifest;
        }

        return LoadManifest();
    }

    public static QuestDatabase LoadQuestDatabase(QuestPackData pack)
    {
        if (pack == null || string.IsNullOrWhiteSpace(pack.file))
        {
            return LoadFallbackQuestDatabase();
        }

        string packPath = GetPackFilePath(pack);
        if (File.Exists(packPath))
        {
            return NormalizeQuestDatabase(JsonUtility.FromJson<QuestDatabase>(File.ReadAllText(packPath)));
        }

        TextAsset packAsset = Resources.Load<TextAsset>(ResourceDirectory + "/" + Path.GetFileNameWithoutExtension(pack.file));
        if (packAsset != null)
        {
            return NormalizeQuestDatabase(JsonUtility.FromJson<QuestDatabase>(packAsset.text));
        }

        Debug.LogWarning("File paket quest tidak ditemukan: " + pack.file);
        return new QuestDatabase { quest = new List<QuestData>() };
    }

    public static async Task<QuestDatabase> LoadQuestDatabaseAsync(QuestPackData pack)
    {
        if (pack == null || string.IsNullOrWhiteSpace(pack.file))
        {
            return LoadFallbackQuestDatabase();
        }

        string cloudJson = await TryLoadCloudTextAsync(GetPackCloudKey(pack));
        if (!string.IsNullOrWhiteSpace(cloudJson))
        {
            QuestDatabase cloudDatabase = NormalizeQuestDatabase(JsonUtility.FromJson<QuestDatabase>(cloudJson));
            SaveQuestDatabase(pack, cloudDatabase);
            return cloudDatabase;
        }

        return LoadQuestDatabase(pack);
    }

    public static QuestDatabase LoadFallbackQuestDatabase()
    {
        string fallbackPath = GetFallbackQuestFilePath();
        if (File.Exists(fallbackPath))
        {
            return NormalizeQuestDatabase(JsonUtility.FromJson<QuestDatabase>(File.ReadAllText(fallbackPath)));
        }

        TextAsset questAsset = Resources.Load<TextAsset>(QuestFallbackResourcePath);
        if (questAsset == null)
        {
            return new QuestDatabase { quest = new List<QuestData>() };
        }

        return NormalizeQuestDatabase(JsonUtility.FromJson<QuestDatabase>(questAsset.text));
    }

    public static void SaveQuestDatabase(QuestPackData pack, QuestDatabase database)
    {
        string path = pack == null ? GetFallbackQuestFilePath() : GetPackFilePath(pack);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(NormalizeQuestDatabase(database), true));
    }

    public static async Task SaveQuestDatabaseAsync(QuestPackData pack, QuestDatabase database, bool requireCloudSave = false)
    {
        QuestDatabase normalizedDatabase = NormalizeQuestDatabase(database);
        SaveQuestDatabase(pack, normalizedDatabase);

        if (pack == null)
        {
            return;
        }

        string json = JsonUtility.ToJson(normalizedDatabase, true);
        if (requireCloudSave)
        {
            await SaveCloudTextStrictAsync(GetPackCloudKey(pack), json, "file paket quest");
        }
        else
        {
            await SaveCloudTextOrLogAsync(GetPackCloudKey(pack), json);
        }

        await SaveManifestAsync(LoadManifest(), requireCloudSave);
    }

    public static async Task<QuestPackCreateResult> CreateNewPackAsync()
    {
        QuestPackCreateResult result = new QuestPackCreateResult();
        QuestManifestData manifest = await LoadManifestAsync();
        int nextNumber = 1;
        string id;
        string name;
        string file;

        do
        {
            name = "Quest " + nextNumber.ToString("000");
            file = "quest_" + nextNumber.ToString("000");
            id = file;
            nextNumber++;
        }
        while (PackIdExists(manifest, id) || PackFileExists(manifest, file) || File.Exists(GetPackFilePath(file)));

        QuestPackData newPack = new QuestPackData
        {
            id = id,
            name = name,
            file = file
        };

        manifest.questPacks.Add(newPack);

        try
        {
            await SaveManifestAsync(manifest, true);
            await SaveQuestDatabaseAsync(newPack, new QuestDatabase { quest = new List<QuestData>() }, true);
            await SaveManifestAsync(manifest, true);
            result.Success = true;
            result.Pack = newPack;
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = "Gagal membuat paket quest baru: " + ex.Message;
            return result;
        }
    }

    public static async Task<QuestPackOperationResult> UpdatePackNameAsync(string packId, string newName)
    {
        QuestPackOperationResult result = new QuestPackOperationResult();
        string cleanName = (newName ?? string.Empty).Trim();
        if (!IsValidPackName(cleanName))
        {
            result.Success = false;
            result.ErrorMessage = "Nama quest hanya boleh berisi huruf, angka, spasi, garis bawah, dan strip.";
            return result;
        }

        QuestManifestData manifest = await LoadManifestAsync();
        QuestPackData pack = FindPackById(manifest, packId);
        if (pack == null)
        {
            result.Success = false;
            result.ErrorMessage = "Paket quest aktif tidak ditemukan di manifest.";
            return result;
        }

        pack.name = cleanName;
        await SaveManifestAsync(manifest);
        result.Success = true;
        return result;
    }

    public static async Task<QuestPackOperationResult> DeletePackAsync(string packId)
    {
        QuestPackOperationResult result = new QuestPackOperationResult();
        if (string.IsNullOrWhiteSpace(packId))
        {
            result.Success = false;
            result.ErrorMessage = "Paket quest belum dipilih.";
            return result;
        }

        QuestManifestData manifest = await LoadManifestAsync();
        QuestPackData pack = FindPackById(manifest, packId);
        if (pack == null)
        {
            result.Success = false;
            result.ErrorMessage = "Paket quest tidak ditemukan di manifest.";
            return result;
        }

        manifest.questPacks.Remove(pack);

        try
        {
            DeleteLocalPackFile(pack);
            await SaveManifestAsync(manifest);
            await DeleteCloudFileOrLogAsync(GetPackCloudKey(pack));
            result.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = "Gagal menghapus paket quest: " + ex.Message;
            return result;
        }
    }

    public static string GetManifestFilePath()
    {
        return Path.Combine(Application.persistentDataPath, "Data", "Quest", "manifest.json");
    }

    public static string GetPackFilePath(QuestPackData pack)
    {
        return GetPackFilePath(Path.GetFileNameWithoutExtension(pack?.file ?? string.Empty));
    }

    private static string GetPackFilePath(string fileName)
    {
        string cleanFileName = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        return Path.Combine(Application.persistentDataPath, "Data", "Quest", cleanFileName + ".json");
    }

    public static bool IsValidPackName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (char c in value)
        {
            if (!char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c) && c != '_' && c != '-')
            {
                return false;
            }
        }

        return true;
    }

    private static void SaveManifest(QuestManifestData manifest)
    {
        string manifestPath = GetManifestFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath));
        File.WriteAllText(manifestPath, JsonUtility.ToJson(NormalizeManifest(manifest), true));
    }

    private static async Task SaveManifestAsync(QuestManifestData manifest, bool requireCloudSave = false)
    {
        QuestManifestData normalizedManifest = NormalizeManifest(manifest);
        string json = JsonUtility.ToJson(normalizedManifest, true);

        if (requireCloudSave)
        {
            await SaveCloudTextStrictAsync(ManifestCloudKey, json, "manifest quest");
        }
        else
        {
            await SaveCloudTextOrLogAsync(ManifestCloudKey, json);
        }

        SaveManifest(normalizedManifest);
    }

    private static async Task<string> TryLoadCloudTextAsync(string key)
    {
        if (!CanUseCloudSave())
        {
            return string.Empty;
        }

        try
        {
            byte[] bytes = await CloudSaveService.Instance.Files.Player.LoadBytesAsync(key);
            return bytes == null || bytes.Length == 0 ? string.Empty : Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            if (IsCloudNotFoundError(ex))
            {
                return string.Empty;
            }

            LastCloudWarningMessage = "Gagal memuat quest dari UGS. Data lokal akan digunakan. Detail: " + ex.Message;
            Debug.LogWarning("Load quest dari UGS gagal. Key: " + key + ", error: " + ex.Message);
            return string.Empty;
        }
    }

    private static async Task SaveCloudTextOrLogAsync(string key, string json)
    {
        if (!CanUseCloudSave())
        {
            return;
        }

        try
        {
            await CloudSaveService.Instance.Files.Player.SaveAsync(key, Encoding.UTF8.GetBytes(json ?? string.Empty));
        }
        catch (Exception ex)
        {
            LastCloudWarningMessage = "Quest tersimpan lokal, tetapi gagal disimpan ke UGS. Detail: " + ex.Message;
            Debug.LogWarning("Save quest ke UGS gagal, data lokal tetap tersimpan. Key: " + key + ", error: " + ex.Message);
        }
    }

    private static async Task SaveCloudTextStrictAsync(string key, string json, string label)
    {
        if (!CanUseCloudSave())
        {
            throw new InvalidOperationException("UGS Cloud Save belum aktif. " + label + " wajib disimpan ke UGS sebelum proses dilanjutkan.");
        }

        try
        {
            await CloudSaveService.Instance.Files.Player.SaveAsync(key, Encoding.UTF8.GetBytes(json ?? string.Empty));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Gagal menyimpan " + label + " ke UGS. Detail: " + ex.Message, ex);
        }
    }

    private static async Task DeleteCloudFileOrLogAsync(string key)
    {
        if (!CanUseCloudSave())
        {
            return;
        }

        try
        {
            await CloudSaveService.Instance.Files.Player.DeleteAsync(key);
        }
        catch (Exception ex)
        {
            if (IsCloudNotFoundError(ex))
            {
                return;
            }

            LastCloudWarningMessage = "Paket quest dihapus lokal, tetapi gagal menghapus file di UGS. Detail: " + ex.Message;
            Debug.LogWarning("Delete quest dari UGS gagal. Key: " + key + ", error: " + ex.Message);
        }
    }

    private static bool CanUseCloudSave()
    {
        return NarafinRuntimeConfig.UseUnityGameServicesAuth &&
               NarafinRuntimeConfig.UseUgsNarasiCloudSave &&
               UnityServices.State == ServicesInitializationState.Initialized &&
               AuthenticationService.Instance.IsSignedIn;
    }

    private static QuestManifestData NormalizeManifest(QuestManifestData manifest)
    {
        if (manifest == null)
        {
            manifest = new QuestManifestData();
        }

        if (manifest.questPacks == null)
        {
            manifest.questPacks = new List<QuestPackData>();
        }

        return manifest;
    }

    private static QuestDatabase NormalizeQuestDatabase(QuestDatabase database)
    {
        if (database == null)
        {
            database = new QuestDatabase();
        }

        if (database.quest == null)
        {
            database.quest = new List<QuestData>();
        }

        return database;
    }

    private static QuestPackData FindPackById(QuestManifestData manifest, string packId)
    {
        if (manifest?.questPacks == null || string.IsNullOrWhiteSpace(packId))
        {
            return null;
        }

        foreach (QuestPackData pack in manifest.questPacks)
        {
            if (pack != null && pack.id == packId)
            {
                return pack;
            }
        }

        return null;
    }

    private static bool PackIdExists(QuestManifestData manifest, string id)
    {
        return FindPackById(manifest, id) != null;
    }

    private static bool PackFileExists(QuestManifestData manifest, string file)
    {
        if (manifest?.questPacks == null)
        {
            return false;
        }

        foreach (QuestPackData pack in manifest.questPacks)
        {
            if (pack != null && Path.GetFileNameWithoutExtension(pack.file) == file)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetFallbackQuestFilePath()
    {
        return Path.Combine(Application.persistentDataPath, "Data", "quest.json");
    }

    private static void DeleteLocalPackFile(QuestPackData pack)
    {
        string packPath = GetPackFilePath(pack);
        if (File.Exists(packPath))
        {
            File.Delete(packPath);
        }

        string metaPath = packPath + ".meta";
        if (File.Exists(metaPath))
        {
            File.Delete(metaPath);
        }
    }

    private static bool IsCloudNotFoundError(Exception ex)
    {
        string message = ex?.Message ?? string.Empty;
        return message.IndexOf("404", StringComparison.OrdinalIgnoreCase) >= 0 ||
               message.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0 ||
               message.IndexOf("NotFound", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetPackCloudKey(QuestPackData pack)
    {
        return CloudKeyPrefix + Path.GetFileNameWithoutExtension(pack?.file ?? string.Empty) + ".json";
    }
}

public static class QuestSessionContext
{
    public const string DefaultOptionName = "Default";

    public static string ActivePackId { get; private set; } = string.Empty;
    public static string ActivePackName { get; private set; } = DefaultOptionName;
    public static QuestDatabase ActiveDatabase { get; private set; }

    public static async Task<NarafinSessionOperationResult> ApplyAsync(QuestPackData selectedPack)
    {
        try
        {
            QuestDatabase database;
            if (selectedPack == null)
            {
                database = QuestPackRepository.LoadFallbackQuestDatabase();
                ActivePackId = string.Empty;
                ActivePackName = DefaultOptionName;
            }
            else
            {
                database = await QuestPackRepository.LoadQuestDatabaseAsync(selectedPack);
                ActivePackId = selectedPack.id ?? string.Empty;
                ActivePackName = string.IsNullOrWhiteSpace(selectedPack.name) ? selectedPack.file : selectedPack.name;
            }

            if (database?.quest == null)
            {
                return new NarafinSessionOperationResult
                {
                    Success = false,
                    ErrorCode = "QUEST_LOAD_FAILED",
                    ErrorMessage = "Data quest yang dipilih tidak valid."
                };
            }

            ActiveDatabase = database;

            if (DataManager.Instance != null)
            {
                ApplyTo(DataManager.Instance);
            }

            return new NarafinSessionOperationResult { Success = true };
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal memuat quest untuk session: " + ex.Message);
            return new NarafinSessionOperationResult
            {
                Success = false,
                ErrorCode = "QUEST_LOAD_FAILED",
                ErrorMessage = "Gagal memuat quest yang dipilih."
            };
        }
    }

    public static void ApplyTo(DataManager dataManager)
    {
        if (dataManager == null || ActiveDatabase == null)
        {
            return;
        }

        dataManager.OverrideQuest(ActiveDatabase, ActivePackName);
    }
}

public class QuestPackOperationResult
{
    public bool Success;
    public string ErrorMessage;
}

public class QuestPackCreateResult : QuestPackOperationResult
{
    public QuestPackData Pack;
}
