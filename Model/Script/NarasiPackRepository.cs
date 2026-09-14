using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

public static class NarasiPackRepository
{
    private const string ResourceDirectory = "Data/Narasi";
    private const string ManifestResourcePath = ResourceDirectory + "/manifest";
    private const string DialogFallbackResourcePath = "Data/dialogKarakter";
    private const string CloudKeyPrefix = "narasi_";
    private const string ManifestCloudKey = CloudKeyPrefix + "manifest.json";

    public static string LastCloudWarningMessage { get; private set; }

    public static void ClearLastCloudWarning()
    {
        LastCloudWarningMessage = string.Empty;
    }

    public static NarasiManifestData LoadManifest()
    {
        string manifestPath = GetManifestFilePath();
        if (File.Exists(manifestPath))
        {
            return NormalizeManifest(JsonUtility.FromJson<NarasiManifestData>(File.ReadAllText(manifestPath)));
        }

        TextAsset manifestAsset = Resources.Load<TextAsset>(ManifestResourcePath);
        if (manifestAsset != null)
        {
            return NormalizeManifest(JsonUtility.FromJson<NarasiManifestData>(manifestAsset.text));
        }

        return new NarasiManifestData { narasiPacks = new List<NarasiPackData>() };
    }

    public static async Task<NarasiManifestData> LoadManifestAsync()
    {
        string cloudJson = await TryLoadCloudTextAsync(ManifestCloudKey);
        if (!string.IsNullOrWhiteSpace(cloudJson))
        {
            Debug.Log("Manifest narasi dimuat dari UGS Cloud Save.");
            NarasiManifestData cloudManifest = NormalizeManifest(JsonUtility.FromJson<NarasiManifestData>(cloudJson));
            SaveManifest(cloudManifest);
            return cloudManifest;
        }

        Debug.Log("Manifest narasi memakai fallback lokal.");
        return LoadManifest();
    }

    public static DialogKarakterDatabase LoadDialogDatabase(NarasiPackData pack)
    {
        if (pack == null || string.IsNullOrWhiteSpace(pack.file))
        {
            return LoadFallbackDialogDatabase();
        }

        string packPath = GetPackFilePath(pack);
        if (File.Exists(packPath))
        {
            return NormalizeDialogDatabase(JsonUtility.FromJson<DialogKarakterDatabase>(File.ReadAllText(packPath)));
        }

        TextAsset packAsset = Resources.Load<TextAsset>(ResourceDirectory + "/" + Path.GetFileNameWithoutExtension(pack.file));
        if (packAsset != null)
        {
            return NormalizeDialogDatabase(JsonUtility.FromJson<DialogKarakterDatabase>(packAsset.text));
        }

        Debug.LogWarning("File paket narasi tidak ditemukan: " + pack.file);
        return new DialogKarakterDatabase { dialogKarakter = new List<DialogKarakterData>() };
    }

    public static async Task<DialogKarakterDatabase> LoadDialogDatabaseAsync(NarasiPackData pack)
    {
        if (pack == null || string.IsNullOrWhiteSpace(pack.file))
        {
            return LoadFallbackDialogDatabase();
        }

        string cloudJson = await TryLoadCloudTextAsync(GetPackCloudKey(pack));
        if (!string.IsNullOrWhiteSpace(cloudJson))
        {
            Debug.Log("Paket narasi dimuat dari UGS Cloud Save: " + pack.file);
            DialogKarakterDatabase cloudDatabase = NormalizeDialogDatabase(JsonUtility.FromJson<DialogKarakterDatabase>(cloudJson));
            SaveDialogDatabase(pack, cloudDatabase);
            return cloudDatabase;
        }

        Debug.Log("Paket narasi memakai fallback lokal: " + pack.file);
        return LoadDialogDatabase(pack);
    }

    public static DialogKarakterDatabase LoadFallbackDialogDatabase()
    {
        string fallbackPath = GetFallbackDialogFilePath();
        if (File.Exists(fallbackPath))
        {
            return NormalizeDialogDatabase(JsonUtility.FromJson<DialogKarakterDatabase>(File.ReadAllText(fallbackPath)));
        }

        TextAsset dialogAsset = Resources.Load<TextAsset>(DialogFallbackResourcePath);
        if (dialogAsset == null)
        {
            Debug.LogWarning("dialogKarakter.json tidak ditemukan di Resources/Data.");
            return new DialogKarakterDatabase { dialogKarakter = new List<DialogKarakterData>() };
        }

        return NormalizeDialogDatabase(JsonUtility.FromJson<DialogKarakterDatabase>(dialogAsset.text));
    }

    public static void SaveDialogDatabase(NarasiPackData pack, DialogKarakterDatabase database)
    {
        string path = pack == null ? GetFallbackDialogFilePath() : GetPackFilePath(pack);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(NormalizeDialogDatabase(database), true));
    }

    public static async Task SaveDialogDatabaseAsync(NarasiPackData pack, DialogKarakterDatabase database, bool requireCloudSave = false)
    {
        DialogKarakterDatabase normalizedDatabase = NormalizeDialogDatabase(database);
        SaveDialogDatabase(pack, normalizedDatabase);

        if (pack == null)
        {
            return;
        }

        string json = JsonUtility.ToJson(normalizedDatabase, true);
        if (requireCloudSave)
        {
            await SaveCloudTextStrictAsync(GetPackCloudKey(pack), json, "file paket narasi");
        }
        else
        {
            await SaveCloudTextOrLogAsync(GetPackCloudKey(pack), json);
        }

        await SaveManifestAsync(LoadManifest(), requireCloudSave);
    }

    public static bool CreateNewPack(out NarasiPackData newPack, out string errorMessage)
    {
        newPack = null;
        errorMessage = string.Empty;

        NarasiManifestData manifest = LoadManifest();
        int nextNumber = 1;
        string id;
        string name;
        string file;

        do
        {
            name = "Narasi " + nextNumber.ToString("000");
            file = "narasi_" + nextNumber.ToString("000");
            id = file;
            nextNumber++;
        }
        while (PackIdExists(manifest, id) || PackFileExists(manifest, file) || File.Exists(GetPackFilePath(file)));

        newPack = new NarasiPackData
        {
            id = id,
            name = name,
            file = file
        };

        manifest.narasiPacks.Add(newPack);

        try
        {
            SaveManifest(manifest);
            SaveDialogDatabase(newPack, new DialogKarakterDatabase { dialogKarakter = new List<DialogKarakterData>() });
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = "Gagal membuat paket narasi baru: " + ex.Message;
            return false;
        }
    }

    public static async Task<NarasiPackCreateResult> CreateNewPackAsync()
    {
        NarasiPackCreateResult result = new NarasiPackCreateResult();
        NarasiManifestData manifest = await LoadManifestAsync();
        int nextNumber = 1;
        string id;
        string name;
        string file;

        do
        {
            name = "Narasi " + nextNumber.ToString("000");
            file = "narasi_" + nextNumber.ToString("000");
            id = file;
            nextNumber++;
        }
        while (PackIdExists(manifest, id) || PackFileExists(manifest, file) || File.Exists(GetPackFilePath(file)));

        NarasiPackData newPack = new NarasiPackData
        {
            id = id,
            name = name,
            file = file
        };

        manifest.narasiPacks.Add(newPack);

        try
        {
            await SaveManifestAsync(manifest, true);
            await SaveDialogDatabaseAsync(newPack, new DialogKarakterDatabase { dialogKarakter = new List<DialogKarakterData>() }, true);
            await SaveManifestAsync(manifest, true);
            result.Success = true;
            result.Pack = newPack;
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = "Gagal membuat paket narasi baru: " + ex.Message;
            return result;
        }
    }

    public static bool UpdatePackName(string packId, string newName, out string errorMessage)
    {
        errorMessage = string.Empty;
        string cleanName = (newName ?? string.Empty).Trim();
        if (!IsValidPackName(cleanName))
        {
            errorMessage = "Nama narasi hanya boleh berisi huruf, angka, spasi, garis bawah, dan strip.";
            return false;
        }

        NarasiManifestData manifest = LoadManifest();
        NarasiPackData pack = FindPackById(manifest, packId);
        if (pack == null)
        {
            errorMessage = "Paket narasi aktif tidak ditemukan di manifest.";
            return false;
        }

        pack.name = cleanName;
        SaveManifest(manifest);
        return true;
    }

    public static async Task<NarasiPackOperationResult> UpdatePackNameAsync(string packId, string newName)
    {
        NarasiPackOperationResult result = new NarasiPackOperationResult();
        string cleanName = (newName ?? string.Empty).Trim();
        if (!IsValidPackName(cleanName))
        {
            result.Success = false;
            result.ErrorMessage = "Nama narasi hanya boleh berisi huruf, angka, spasi, garis bawah, dan strip.";
            return result;
        }

        NarasiManifestData manifest = await LoadManifestAsync();
        NarasiPackData pack = FindPackById(manifest, packId);
        if (pack == null)
        {
            result.Success = false;
            result.ErrorMessage = "Paket narasi aktif tidak ditemukan di manifest.";
            return result;
        }

        pack.name = cleanName;
        await SaveManifestAsync(manifest);
        result.Success = true;
        return result;
    }

    public static async Task<NarasiPackOperationResult> DeletePackAsync(string packId)
    {
        NarasiPackOperationResult result = new NarasiPackOperationResult();
        if (string.IsNullOrWhiteSpace(packId))
        {
            result.Success = false;
            result.ErrorMessage = "Paket narasi belum dipilih.";
            return result;
        }

        NarasiManifestData manifest = await LoadManifestAsync();
        NarasiPackData pack = FindPackById(manifest, packId);
        if (pack == null)
        {
            result.Success = false;
            result.ErrorMessage = "Paket narasi tidak ditemukan di manifest.";
            return result;
        }

        manifest.narasiPacks.Remove(pack);

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
            result.ErrorMessage = "Gagal menghapus paket narasi: " + ex.Message;
            return result;
        }
    }

    public static string GetManifestFilePath()
    {
        return Path.Combine(Application.persistentDataPath, "Data", "Narasi", "manifest.json");
    }

    public static string GetPackFilePath(NarasiPackData pack)
    {
        string fileName = Path.GetFileNameWithoutExtension(pack?.file ?? string.Empty);
        return GetPackFilePath(fileName);
    }

    private static string GetPackFilePath(string fileName)
    {
        string cleanFileName = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        return Path.Combine(Application.persistentDataPath, "Data", "Narasi", cleanFileName + ".json");
    }

    public static string ToFileNameSlug(string value)
    {
        string source = (value ?? string.Empty).Trim().ToLowerInvariant();
        StringBuilder builder = new StringBuilder();
        bool previousWasSeparator = false;

        foreach (char c in source)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                previousWasSeparator = false;
            }
            else if ((char.IsWhiteSpace(c) || c == '_' || c == '-') && !previousWasSeparator)
            {
                builder.Append('_');
                previousWasSeparator = true;
            }
        }

        string slug = builder.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(slug) ? "narasi" : slug;
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

    private static void SaveManifest(NarasiManifestData manifest)
    {
        string manifestPath = GetManifestFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath));
        File.WriteAllText(manifestPath, JsonUtility.ToJson(NormalizeManifest(manifest), true));
    }

    private static async Task SaveManifestAsync(NarasiManifestData manifest, bool requireCloudSave = false)
    {
        NarasiManifestData normalizedManifest = NormalizeManifest(manifest);
        string json = JsonUtility.ToJson(normalizedManifest, true);

        if (requireCloudSave)
        {
            await SaveCloudTextStrictAsync(ManifestCloudKey, json, "manifest narasi");
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
            byte[] bytes = await InvokeCloudSaveLoadBytesAsync(key);
            return bytes == null || bytes.Length == 0 ? string.Empty : Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            if (IsCloudNotFoundError(ex))
            {
                Debug.Log("Narasi belum ada di UGS Cloud Save. Fallback lokal digunakan. Key: " + key);
                return string.Empty;
            }

            LastCloudWarningMessage = "Gagal memuat narasi dari UGS. Data lokal akan digunakan. Detail: " + ex.Message;
            Debug.LogWarning("Load narasi dari UGS gagal. Key: " + key + ", error: " + ex.Message);
            return string.Empty;
        }
    }

    private static async Task SaveCloudTextOrLogAsync(string key, string json)
    {
        if (!CanUseCloudSave())
        {
            Debug.Log("Save narasi ke UGS dilewati. Cloud Save belum aktif atau dimatikan. Key: " + key);
            return;
        }

        try
        {
            await InvokeCloudSaveSaveAsync(key, Encoding.UTF8.GetBytes(json ?? string.Empty));
            Debug.Log("Narasi tersimpan ke UGS Cloud Save. Key: " + key);
        }
        catch (Exception ex)
        {
            LastCloudWarningMessage = "Narasi tersimpan lokal, tetapi gagal disimpan ke UGS. Detail: " + ex.Message;
            Debug.LogWarning("Save narasi ke UGS gagal, data lokal tetap tersimpan. Key: " + key + ", error: " + ex.Message);
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
            await InvokeCloudSaveSaveAsync(key, Encoding.UTF8.GetBytes(json ?? string.Empty));
            Debug.Log(label + " tersimpan ke UGS Cloud Save. Key: " + key);
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
            Debug.Log("Delete narasi di UGS dilewati. Cloud Save belum aktif atau dimatikan. Key: " + key);
            return;
        }

        try
        {
            await CloudSaveService.Instance.Files.Player.DeleteAsync(key);
            Debug.Log("Narasi dihapus dari UGS Cloud Save. Key: " + key);
        }
        catch (Exception ex)
        {
            if (IsCloudNotFoundError(ex))
            {
                Debug.Log("File narasi UGS sudah tidak ada. Key: " + key);
                return;
            }

            LastCloudWarningMessage = "Paket narasi dihapus lokal, tetapi gagal menghapus file di UGS. Detail: " + ex.Message;
            Debug.LogWarning("Delete narasi dari UGS gagal. Key: " + key + ", error: " + ex.Message);
        }
    }

    private static bool CanUseCloudSave()
    {
        return NarafinRuntimeConfig.UseUnityGameServicesAuth &&
               NarafinRuntimeConfig.UseUgsNarasiCloudSave &&
               UnityServices.State == ServicesInitializationState.Initialized &&
               AuthenticationService.Instance.IsSignedIn;
    }

    private static async Task<byte[]> InvokeCloudSaveLoadBytesAsync(string key)
    {
        return await CloudSaveService.Instance.Files.Player.LoadBytesAsync(key);
    }

    private static async Task InvokeCloudSaveSaveAsync(string key, byte[] bytes)
    {
        await CloudSaveService.Instance.Files.Player.SaveAsync(key, bytes);
    }

    private static NarasiManifestData NormalizeManifest(NarasiManifestData manifest)
    {
        if (manifest == null)
        {
            manifest = new NarasiManifestData();
        }

        if (manifest.narasiPacks == null)
        {
            manifest.narasiPacks = new List<NarasiPackData>();
        }

        return manifest;
    }

    private static DialogKarakterDatabase NormalizeDialogDatabase(DialogKarakterDatabase database)
    {
        if (database == null)
        {
            database = new DialogKarakterDatabase();
        }

        if (database.dialogKarakter == null)
        {
            database.dialogKarakter = new List<DialogKarakterData>();
        }

        return database;
    }

    private static NarasiPackData FindPackById(NarasiManifestData manifest, string packId)
    {
        if (manifest?.narasiPacks == null || string.IsNullOrWhiteSpace(packId))
        {
            return null;
        }

        foreach (NarasiPackData pack in manifest.narasiPacks)
        {
            if (pack != null && pack.id == packId)
            {
                return pack;
            }
        }

        return null;
    }

    private static bool PackIdExists(NarasiManifestData manifest, string id)
    {
        if (manifest?.narasiPacks == null)
        {
            return false;
        }

        foreach (NarasiPackData pack in manifest.narasiPacks)
        {
            if (pack != null && pack.id == id)
            {
                return true;
            }
        }

        return false;
    }

    private static bool PackFileExists(NarasiManifestData manifest, string file)
    {
        if (manifest?.narasiPacks == null)
        {
            return false;
        }

        foreach (NarasiPackData pack in manifest.narasiPacks)
        {
            if (pack != null && Path.GetFileNameWithoutExtension(pack.file) == file)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetFallbackDialogFilePath()
    {
        return Path.Combine(Application.persistentDataPath, "Data", "dialogKarakter.json");
    }

    private static void DeleteLocalPackFile(NarasiPackData pack)
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

    private static string GetPackCloudKey(NarasiPackData pack)
    {
        string fileName = Path.GetFileNameWithoutExtension(pack?.file ?? string.Empty);
        return CloudKeyPrefix + fileName + ".json";
    }
}

public static class NarasiSessionContext
{
    public const string DefaultOptionName = "Default";

    public static string ActivePackId { get; private set; } = string.Empty;
    public static string ActivePackName { get; private set; } = DefaultOptionName;
    public static DialogKarakterDatabase ActiveDatabase { get; private set; }

    public static async Task<NarafinSessionOperationResult> ApplyAsync(NarasiPackData selectedPack)
    {
        try
        {
            DialogKarakterDatabase database;
            if (selectedPack == null)
            {
                database = NarasiPackRepository.LoadFallbackDialogDatabase();
                ActivePackId = string.Empty;
                ActivePackName = DefaultOptionName;
            }
            else
            {
                database = await NarasiPackRepository.LoadDialogDatabaseAsync(selectedPack);
                ActivePackId = selectedPack.id ?? string.Empty;
                ActivePackName = string.IsNullOrWhiteSpace(selectedPack.name) ? selectedPack.file : selectedPack.name;
            }

            if (database == null || database.dialogKarakter == null)
            {
                return CreateFailure("NARASI_LOAD_FAILED", "Data narasi yang dipilih tidak valid.");
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
            Debug.LogWarning("Gagal memuat narasi untuk session: " + ex.Message);
            return CreateFailure("NARASI_LOAD_FAILED", "Gagal memuat narasi yang dipilih.");
        }
    }

    public static void ApplyTo(DataManager dataManager)
    {
        if (dataManager == null || ActiveDatabase == null)
        {
            return;
        }

        dataManager.OverrideDialogKarakter(ActiveDatabase, ActivePackName);
    }

    private static NarafinSessionOperationResult CreateFailure(string errorCode, string errorMessage)
    {
        return new NarafinSessionOperationResult
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}

public class NarasiPackOperationResult
{
    public bool Success;
    public string ErrorMessage;
}

public class NarasiPackCreateResult : NarasiPackOperationResult
{
    public NarasiPackData Pack;
}
