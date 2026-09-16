using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManager
{
    private const string EmptyDialogOption = "Tidak ada dialog";
    private const string EmptyNarasiPackOption = "Tidak ada paket narasi";
    private const string NewDialogOption = "Dialog baru (belum disimpan)";
    private static readonly List<string> EditNarasiDefaultActionTypes = new List<string>
    {
        "BahanMasakan",
        "Kebutuhan",
        "JualMasakan",
        "Menabung",
        "TujuanFinansial"
    };
    private static readonly List<string> EditNarasiDefaultNpcSprites = new List<string>
    {
        "penjual_bahan",
        "penjual_kebutuhan",
        "pembeli_masakan",
        "penjaga_bank"
    };

    private readonly Dictionary<string, DialogKarakterData> editNarasiDialogLookup = new Dictionary<string, DialogKarakterData>();
    private readonly Dictionary<string, NarasiPackData> editNarasiPackLookup = new Dictionary<string, NarasiPackData>();
    private NarasiPackData editNarasiActivePack;
    private string editNarasiSelectedDialogId;
    private bool editNarasiIsCreatingNew;
    private bool isEditNarasiLoading;

    private async Task SetupEditNarasiPackSelectionAsync()
    {
        editNarasiPackLookup.Clear();
        NarasiManifestData manifest = await NarasiPackRepository.LoadManifestAsync();
        List<string> packOptions = new List<string>();

        if (manifest?.narasiPacks != null)
        {
            foreach (NarasiPackData pack in manifest.narasiPacks)
            {
                if (pack == null || string.IsNullOrWhiteSpace(pack.id) || string.IsNullOrWhiteSpace(pack.file))
                {
                    continue;
                }

                string label = BuildNarasiPackOptionLabel(pack);
                string uniqueLabel = label;
                int duplicateCount = 2;
                while (editNarasiPackLookup.ContainsKey(uniqueLabel))
                {
                    uniqueLabel = label + " (" + duplicateCount + ")";
                    duplicateCount++;
                }

                editNarasiPackLookup[uniqueLabel] = pack;
                packOptions.Add(uniqueLabel);
            }
        }

        if (editNarasiPackDropdown == null)
        {
            return;
        }

        if (packOptions.Count == 0)
        {
            editNarasiPackDropdown.choices = new List<string> { EmptyNarasiPackOption };
            editNarasiPackDropdown.SetValueWithoutNotify(EmptyNarasiPackOption);
            return;
        }

        editNarasiPackDropdown.choices = packOptions;
        string selectedOption = FindActiveNarasiPackOption(packOptions) ?? packOptions[0];
        editNarasiPackDropdown.SetValueWithoutNotify(selectedOption);
    }

    private async Task<bool> SelectNarasiPackForEditingAsync()
    {
        string selectedOption = editNarasiPackDropdown?.value;
        if (string.IsNullOrWhiteSpace(selectedOption) || !editNarasiPackLookup.TryGetValue(selectedOption, out NarasiPackData selectedPack))
        {
            ShowErrorPopup("Pilih paket narasi terlebih dahulu.");
            return false;
        }

        editNarasiActivePack = selectedPack;
        SetTextWithoutNotify(editNarasiActivePackNameInput, selectedPack.name);
        await SetupEditNarasiDialogDropdownAsync();
        return true;
    }

    private async Task CreateNewNarasiPackAsync()
    {
        NarasiPackCreateResult result = await NarasiPackRepository.CreateNewPackAsync();
        if (!result.Success)
        {
            ShowErrorPopup(result.ErrorMessage);
            return;
        }

        editNarasiActivePack = result.Pack;
        await SetupEditNarasiPackSelectionAsync();
        ShowEditNarasiCloudWarningOrSuccess("Paket narasi baru berhasil dibuat. Kamu bisa ubah namanya setelah menekan Next.");
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    private async Task DeleteSelectedNarasiPackAsync()
    {
        string selectedOption = editNarasiPackDropdown?.value;
        if (string.IsNullOrWhiteSpace(selectedOption) || !editNarasiPackLookup.TryGetValue(selectedOption, out NarasiPackData selectedPack))
        {
            ShowErrorPopup("Pilih paket narasi yang ingin dihapus.");
            return;
        }

        NarasiPackOperationResult result = await NarasiPackRepository.DeletePackAsync(selectedPack.id);
        if (!result.Success)
        {
            ShowErrorPopup(result.ErrorMessage);
            return;
        }

        if (editNarasiActivePack != null && editNarasiActivePack.id == selectedPack.id)
        {
            editNarasiActivePack = null;
            editNarasiSelectedDialogId = null;
            editNarasiIsCreatingNew = false;
            ClearEditNarasiForm();
        }

        await SetupEditNarasiPackSelectionAsync();
        ShowEditNarasiCloudWarningOrSuccess("Paket narasi berhasil dihapus.");
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    private string BuildNarasiPackOptionLabel(NarasiPackData pack)
    {
        if (!string.IsNullOrWhiteSpace(pack.name))
        {
            return pack.name;
        }

        if (!string.IsNullOrWhiteSpace(pack.id))
        {
            return pack.id;
        }

        return pack.file;
    }

    private string FindActiveNarasiPackOption(List<string> options)
    {
        if (editNarasiActivePack == null || options == null)
        {
            return null;
        }

        foreach (string option in options)
        {
            if (editNarasiPackLookup.TryGetValue(option, out NarasiPackData pack) && pack.id == editNarasiActivePack.id)
            {
                return option;
            }
        }

        return null;
    }

    private async Task SetupEditNarasiDialogDropdownAsync()
    {
        SetupEditNarasiLineSpeakerDropdowns();
        await SetupEditNarasiBasicDropdownsAsync();

        if (editNarasiDialogDropdown == null)
        {
            return;
        }

        List<string> dialogIds = await GetDialogKarakterIdsAsync();
        if (dialogIds.Count == 0)
        {
            editNarasiDialogDropdown.choices = new List<string> { EmptyDialogOption };
            editNarasiDialogDropdown.SetValueWithoutNotify(EmptyDialogOption);
            editNarasiSelectedDialogId = null;
            editNarasiIsCreatingNew = false;
            ClearEditNarasiForm();
            Debug.LogWarning("Dropdown Edit Narasi kosong. Paket narasi aktif belum memiliki data dialogKarakter.");
            return;
        }

        editNarasiDialogDropdown.choices = dialogIds;

        string selectedDialogId = editNarasiDialogDropdown.value;
        if (string.IsNullOrWhiteSpace(selectedDialogId) || !editNarasiDialogLookup.ContainsKey(selectedDialogId))
        {
            selectedDialogId = dialogIds[0];
        }

        editNarasiDialogDropdown.SetValueWithoutNotify(selectedDialogId);
        PopulateEditNarasiForm(selectedDialogId);
    }

    private async Task SetupEditNarasiBasicDropdownsAsync()
    {
        DialogKarakterDatabase database = await LoadEditNarasiDatabaseAsync();
        List<string> actionChoices = BuildEditNarasiDropdownChoices(
            EditNarasiDefaultActionTypes,
            database?.dialogKarakter,
            dialog => dialog?.aksi);
        List<string> npcSpriteChoices = BuildEditNarasiDropdownChoices(
            EditNarasiDefaultNpcSprites,
            database?.dialogKarakter,
            dialog => dialog?.npcSprite);

        ConfigureDropdown(editNarasiActionTypeInput, actionChoices, actionChoices.Count > 0 ? actionChoices[0] : string.Empty);
        string defaultNpcSprite = npcSpriteChoices.Count > 0 ? npcSpriteChoices[0] : string.Empty;
        ConfigureDropdown(editNarasiNpcSpriteInput, npcSpriteChoices, defaultNpcSprite);
        UpdateEditNarasiNpcSpritePreview(editNarasiNpcSpriteInput?.value ?? defaultNpcSprite);
        await SetupEditNarasiQuestDropdownsAsync();
    }

    private List<string> BuildEditNarasiDropdownChoices(
        List<string> defaultChoices,
        List<DialogKarakterData> dialogs,
        Func<DialogKarakterData, string> selector)
    {
        List<string> choices = new List<string>();
        AddUniqueChoices(choices, defaultChoices);

        if (dialogs != null)
        {
            foreach (DialogKarakterData dialog in dialogs)
            {
                string value = selector(dialog);
                if (!string.IsNullOrWhiteSpace(value) && !choices.Contains(value))
                {
                    choices.Add(value);
                }
            }
        }

        return choices;
    }

    private void AddUniqueChoices(List<string> target, IEnumerable<string> values)
    {
        if (target == null || values == null)
        {
            return;
        }

        foreach (string value in values)
        {
            if (!string.IsNullOrWhiteSpace(value) && !target.Contains(value))
            {
                target.Add(value);
            }
        }
    }

    private void SetupEditNarasiLineSpeakerDropdowns()
    {
        EnsureEditNarasiLineInputs();
        for (int i = 0; i < editNarasiLineSpeakerInputs.Count; i++)
        {
            ConfigureDropdown(editNarasiLineSpeakerInputs[i], GetEditNarasiSpeakerChoices(), i % 2 == 0 ? "NPC" : "PLAYER");
        }
    }

    private List<string> GetEditNarasiSpeakerChoices()
    {
        return new List<string> { "NPC", "PLAYER" };
    }

    private void ConfigureDropdown(DropdownField dropdown, List<string> choices, string defaultValue)
    {
        if (dropdown == null)
        {
            return;
        }

        dropdown.choices = choices;
        dropdown.SetValueWithoutNotify(defaultValue);
    }

    private void InitializeEditNarasiLineInputs()
    {
        editNarasiLineRemoveButtons.Clear();
        editNarasiLineSpeakerInputs.Clear();
        editNarasiLineTextInputs.Clear();
        AddExistingEditNarasiLineInput(editNarasiLineRemove1, editNarasiLineSpeaker1, editNarasiLineText1);
        AddExistingEditNarasiLineInput(editNarasiLineRemove2, editNarasiLineSpeaker2, editNarasiLineText2);
        editNarasiLineInputsInitialized = true;
        RefreshEditNarasiLineNames();
    }

    private void AddExistingEditNarasiLineInput(Button removeButton, DropdownField speakerDropdown, TextField textField)
    {
        if (removeButton == null || speakerDropdown == null || textField == null)
        {
            return;
        }

        RegisterEditNarasiLineRemoveButton(removeButton);
        editNarasiLineRemoveButtons.Add(removeButton);
        editNarasiLineSpeakerInputs.Add(speakerDropdown);
        editNarasiLineTextInputs.Add(textField);
    }

    private void EnsureEditNarasiLineInputs()
    {
        if (!editNarasiLineInputsInitialized)
        {
            InitializeEditNarasiLineInputs();
        }
    }

    private void RegisterEditNarasiLineRemoveButton(Button removeButton)
    {
        if (removeButton == null || editNarasiLineRemoveButtonsRegistered.Contains(removeButton))
        {
            return;
        }

        removeButton.RegisterCallback<ClickEvent>(OnEditNarasiLineRemoveClicked);
        editNarasiLineRemoveButtonsRegistered.Add(removeButton);
    }

    private void OnEditNarasiLineRemoveClicked(ClickEvent evt)
    {
        RemoveEditNarasiLineInput(evt.currentTarget as Button);
    }

    private async Task<List<string>> GetDialogKarakterIdsAsync()
    {
        editNarasiDialogLookup.Clear();
        DialogKarakterDatabase database = await LoadEditNarasiDatabaseAsync();
        if (database?.dialogKarakter != null)
        {
            foreach (DialogKarakterData dialog in database.dialogKarakter)
            {
                AddEditNarasiDialog(dialog);
            }
        }

        if (editNarasiDialogLookup.Count == 0)
        {
            LoadEditNarasiDialogsFromDataManager();
        }

        List<string> dialogIds = new List<string>(editNarasiDialogLookup.Keys);
        dialogIds.Sort();
        return dialogIds;
    }

    private void LoadEditNarasiDialogsFromDataManager()
    {
        if (DataManager.Instance == null || DataManager.Instance.dialogKarakterDict == null)
        {
            return;
        }

        foreach (var pair in DataManager.Instance.dialogKarakterDict)
        {
            AddEditNarasiDialog(pair.Value);
        }
    }

    private DialogKarakterDatabase LoadEditNarasiDatabase()
    {
        return editNarasiActivePack == null
            ? NarasiPackRepository.LoadFallbackDialogDatabase()
            : NarasiPackRepository.LoadDialogDatabase(editNarasiActivePack);
    }

    private async Task<DialogKarakterDatabase> LoadEditNarasiDatabaseAsync()
    {
        return editNarasiActivePack == null
            ? NarasiPackRepository.LoadFallbackDialogDatabase()
            : await NarasiPackRepository.LoadDialogDatabaseAsync(editNarasiActivePack);
    }

    private void RefreshEditNarasiDialogLookupFromCurrentSource()
    {
        editNarasiDialogLookup.Clear();
        DialogKarakterDatabase database = LoadEditNarasiDatabase();
        if (database?.dialogKarakter == null)
        {
            return;
        }

        foreach (DialogKarakterData dialog in database.dialogKarakter)
        {
            AddEditNarasiDialog(dialog);
        }
    }

    private void AddEditNarasiDialog(DialogKarakterData dialogData)
    {
        if (dialogData == null || string.IsNullOrWhiteSpace(dialogData.id))
        {
            return;
        }

        editNarasiDialogLookup[dialogData.id] = dialogData;
    }

    private void OnEditNarasiDialogChanged(ChangeEvent<string> evt)
    {
        PopulateEditNarasiForm(evt.newValue);
    }

    private void BeginNewEditNarasiDialog()
    {
        RefreshEditNarasiDialogLookupFromCurrentSource();
        editNarasiSelectedDialogId = null;
        editNarasiIsCreatingNew = true;

        if (editNarasiDialogDropdown != null)
        {
            List<string> choices = new List<string>(editNarasiDialogDropdown.choices ?? new List<string>());
            if (!choices.Contains(NewDialogOption))
            {
                choices.Insert(0, NewDialogOption);
            }

            editNarasiDialogDropdown.choices = choices;
            editNarasiDialogDropdown.SetValueWithoutNotify(NewDialogOption);
        }

        ClearEditNarasiForm();
        SetTextWithoutNotify(editNarasiIdInput, GenerateNewDialogId());
        SetIntegerWithoutNotify(editNarasiAksiValueInput, 0);
        SetTextWithoutNotify(editNarasiNpcNameInput, string.Empty);
        SetDropdownWithoutNotify(editNarasiActionTypeInput, "BahanMasakan");
        SetDropdownWithoutNotify(editNarasiNpcSpriteInput, "penjual_bahan");
        UpdateEditNarasiNpcSpritePreview("penjual_bahan");
        ShowSuccessPopup("Form dialog baru siap diisi.");
    }

    private string GenerateNewDialogId()
    {
        const string prefix = "dialog_baru_";
        for (int i = 1; i < 1000; i++)
        {
            string candidate = prefix + i.ToString("000");
            if (!editNarasiDialogLookup.ContainsKey(candidate))
            {
                return candidate;
            }
        }

        return prefix + DateTime.Now.ToString("yyyyMMddHHmmss");
    }

    private void ResetEditNarasiForm()
    {
        if (editNarasiIsCreatingNew)
        {
            BeginNewEditNarasiDialog();
            return;
        }

        string selectedDialogId = editNarasiSelectedDialogId;
        if (string.IsNullOrWhiteSpace(selectedDialogId))
        {
            selectedDialogId = editNarasiDialogDropdown?.value;
        }

        PopulateEditNarasiForm(selectedDialogId);
        ShowSuccessPopup("Perubahan form dikembalikan ke data terakhir.");
    }

    private async Task<bool> SaveEditNarasiFormAsync()
    {
        if (!await SaveActiveNarasiPackNameAsync())
        {
            return false;
        }

        string dialogId = editNarasiIdInput?.value?.Trim();
        if (string.IsNullOrWhiteSpace(dialogId))
        {
            ShowErrorPopup("Dialog ID tidak boleh kosong.");
            return false;
        }

        // Dialog tanpa aksi pemicu atau tanpa baris berisi tidak akan pernah muncul saat bermain.
        if (string.IsNullOrWhiteSpace(editNarasiActionTypeInput?.value))
        {
            ShowErrorPopup("Aksi pemicu tidak boleh kosong.");
            return false;
        }

        DialogKarakterDatabase database = await LoadEditNarasiDatabaseAsync();
        if (database == null)
        {
            database = new DialogKarakterDatabase();
        }

        if (database.dialogKarakter == null)
        {
            database.dialogKarakter = new List<DialogKarakterData>();
        }

        int existingIndex = FindDialogIndex(database.dialogKarakter, editNarasiSelectedDialogId);
        int duplicateIndex = FindDialogIndex(database.dialogKarakter, dialogId);
        bool isChangingId = !string.IsNullOrWhiteSpace(editNarasiSelectedDialogId) && editNarasiSelectedDialogId != dialogId;

        if ((editNarasiIsCreatingNew || isChangingId) && duplicateIndex >= 0)
        {
            ShowErrorPopup("Dialog ID sudah digunakan. Gunakan ID lain.");
            return false;
        }

        DialogKarakterData previousData = existingIndex >= 0 ? database.dialogKarakter[existingIndex] : null;
        DialogKarakterData formData = BuildEditNarasiDialogFromForm(previousData);
        if (formData.lines == null || !formData.lines.Any(line => line != null && !string.IsNullOrWhiteSpace(line.text)))
        {
            ShowErrorPopup("Isi minimal satu baris dialog.");
            return false;
        }

        if (existingIndex >= 0)
        {
            database.dialogKarakter[existingIndex] = formData;
        }
        else
        {
            database.dialogKarakter.Add(formData);
        }

        try
        {
            await NarasiPackRepository.SaveDialogDatabaseAsync(editNarasiActivePack, database);
            RefreshEditNarasiRuntimeCache(database);
            editNarasiIsCreatingNew = false;
            editNarasiSelectedDialogId = dialogId;
            await SetupEditNarasiDialogDropdownAsync();
            editNarasiDialogDropdown?.SetValueWithoutNotify(dialogId);
            PopulateEditNarasiForm(dialogId);
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            ShowEditNarasiCloudWarningOrSuccess("Save berhasil. Narasi berhasil disimpan.");
            Debug.Log($"Edit Narasi saved: {dialogId}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Gagal menyimpan narasi: {ex.Message}");
            ShowErrorPopup("Gagal menyimpan narasi. Cek console untuk detail.");
            return false;
        }
    }

    private async Task<bool> DeleteSelectedEditNarasiDialogAsync()
    {
        string dialogId = editNarasiSelectedDialogId;
        if (string.IsNullOrWhiteSpace(dialogId))
        {
            dialogId = editNarasiDialogDropdown?.value;
        }

        if (string.IsNullOrWhiteSpace(dialogId) || dialogId == EmptyDialogOption || dialogId == NewDialogOption)
        {
            ShowErrorPopup("Pilih dialog yang ingin dihapus.");
            return false;
        }

        DialogKarakterDatabase database = await LoadEditNarasiDatabaseAsync();
        if (database?.dialogKarakter == null)
        {
            ShowErrorPopup("Data dialog tidak ditemukan.");
            return false;
        }

        int dialogIndex = FindDialogIndex(database.dialogKarakter, dialogId);
        if (dialogIndex < 0)
        {
            ShowErrorPopup("Dialog yang dipilih tidak ditemukan.");
            return false;
        }

        database.dialogKarakter.RemoveAt(dialogIndex);

        try
        {
            await NarasiPackRepository.SaveDialogDatabaseAsync(editNarasiActivePack, database);
            RefreshEditNarasiRuntimeCache(database);
            editNarasiSelectedDialogId = null;
            editNarasiIsCreatingNew = false;
            await SetupEditNarasiDialogDropdownAsync();
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            ShowEditNarasiCloudWarningOrSuccess("Dialog berhasil dihapus.");
            Debug.Log($"Edit Narasi deleted: {dialogId}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Gagal menghapus dialog narasi: {ex.Message}");
            ShowErrorPopup("Gagal menghapus dialog narasi. Detail: " + ex.Message);
            return false;
        }
    }

    private async Task<bool> SaveActiveNarasiPackNameAsync()
    {
        if (editNarasiActivePack == null)
        {
            return true;
        }

        string activePackName = editNarasiActivePackNameInput?.value?.Trim();
        if (string.IsNullOrWhiteSpace(activePackName))
        {
            ShowErrorPopup("Nama narasi aktif tidak boleh kosong.");
            return false;
        }

        if (activePackName == editNarasiActivePack.name)
        {
            return true;
        }

        NarasiPackOperationResult result = await NarasiPackRepository.UpdatePackNameAsync(editNarasiActivePack.id, activePackName);
        if (!result.Success)
        {
            ShowErrorPopup(result.ErrorMessage);
            return false;
        }

        editNarasiActivePack.name = activePackName;
        return true;
    }

    private void BeginEditNarasiLoading(string message)
    {
        if (isEditNarasiLoading)
        {
            return;
        }

        isEditNarasiLoading = true;
        SetEditNarasiButtonsEnabled(false);
        BeginPlayLoading(message);
    }

    private void EndEditNarasiLoading()
    {
        EndPlayLoading();
        SetEditNarasiButtonsEnabled(true);
        isEditNarasiLoading = false;
    }

    private void SetEditNarasiButtonsEnabled(bool enabled)
    {
        editButton?.SetEnabled(enabled);
        backEditButton?.SetEnabled(enabled);
        backEditPemilihanNarasiButton?.SetEnabled(enabled);
        backEditNarasiButton?.SetEnabled(enabled);
        editNarasiButton?.SetEnabled(enabled);
        editAssetButton?.SetEnabled(enabled);
        editNarasiPackNewButton?.SetEnabled(enabled);
        editNarasiPackDeleteButton?.SetEnabled(enabled);
        nextEditPemilihanNarasiButton?.SetEnabled(enabled);
        editNarasiNewButton?.SetEnabled(enabled);
        editNarasiDeleteButton?.SetEnabled(enabled);
        editNarasiAddLineButton?.SetEnabled(enabled);
        editNarasiResetButton?.SetEnabled(enabled);
        editNarasiSaveButton?.SetEnabled(enabled);
    }

    private void ShowEditNarasiCloudWarningIfNeeded()
    {
        string warningMessage = NarasiPackRepository.LastCloudWarningMessage;
        if (!string.IsNullOrWhiteSpace(warningMessage))
        {
            ShowErrorPopup(warningMessage);
        }
    }

    private void ShowEditNarasiCloudWarningOrSuccess(string successMessage)
    {
        string warningMessage = NarasiPackRepository.LastCloudWarningMessage;
        if (!string.IsNullOrWhiteSpace(warningMessage))
        {
            ShowErrorPopup(warningMessage);
            return;
        }

        ShowSuccessPopup(successMessage);
    }

    private int FindDialogIndex(List<DialogKarakterData> dialogs, string dialogId)
    {
        if (dialogs == null || string.IsNullOrWhiteSpace(dialogId))
        {
            return -1;
        }

        for (int i = 0; i < dialogs.Count; i++)
        {
            if (dialogs[i] != null && dialogs[i].id == dialogId)
            {
                return i;
            }
        }

        return -1;
    }

    private DialogKarakterData BuildEditNarasiDialogFromForm(DialogKarakterData previousData)
    {
        return new DialogKarakterData
        {
            id = editNarasiIdInput?.value?.Trim() ?? string.Empty,
            aksi = editNarasiActionTypeInput?.value?.Trim() ?? string.Empty,
            aksiValue = editNarasiAksiValueInput?.value ?? 0,
            npcName = editNarasiNpcNameInput?.value?.Trim() ?? string.Empty,
            npcSprite = editNarasiNpcSpriteInput?.value?.Trim() ?? string.Empty,
            questId = ResolveQuestIdFromOption(editNarasiQuestEffectDropdown?.value),
            questState = string.IsNullOrWhiteSpace(ResolveQuestIdFromOption(editNarasiQuestEffectDropdown?.value))
                ? string.Empty
                : QuestState.Normalize(editNarasiQuestEffectStateDropdown?.value),
            prerequisite = BuildEditNarasiPrerequisites(),
            lines = BuildEditNarasiLines()
        };
    }

    private List<DialogPrerequisiteData> BuildEditNarasiPrerequisites()
    {
        DialogPrerequisiteData prerequisite = new DialogPrerequisiteData
        {
            uang = editNarasiMinCoinInput?.value ?? 0,
            kebahagiaan = editNarasiMinHappinessInput?.value ?? 0,
            tabungan = editNarasiMinSavingInput?.value ?? 0,
            emas = editNarasiMinGoldInput?.value ?? 0,
            kartuPinjaman = editNarasiLoanCardInput?.value ?? 0,
            mingguKe = editNarasiWeekInput?.value ?? 0,
            hariKe = editNarasiDayInput?.value ?? 0,
            asuransiDimiliki = editNarasiHasInsuranceToggle?.value ?? false,
            bahanDimiliki = ParseCommaSeparatedList(editNarasiRequiredBahanInput?.value),
            kebutuhanDimiliki = ParseCommaSeparatedList(editNarasiRequiredKebutuhanInput?.value),
            tujuanFinansialDimiliki = ParseCommaSeparatedList(editNarasiRequiredTujuanFinansialInput?.value),
            masakanDijual = ParseCommaSeparatedList(editNarasiRequiredMasakanInput?.value),
            questId = ResolveQuestIdFromOption(editNarasiQuestPrereqDropdown?.value),
            questState = string.IsNullOrWhiteSpace(ResolveQuestIdFromOption(editNarasiQuestPrereqDropdown?.value))
                ? string.Empty
                : QuestState.Normalize(editNarasiQuestPrereqStateDropdown?.value)
        };

        return new List<DialogPrerequisiteData> { prerequisite };
    }

    private List<DialogKarakterLineData> BuildEditNarasiLines()
    {
        EnsureEditNarasiLineInputs();
        List<DialogKarakterLineData> lines = new List<DialogKarakterLineData>();
        for (int i = 0; i < editNarasiLineTextInputs.Count; i++)
        {
            DropdownField speakerDropdown = i < editNarasiLineSpeakerInputs.Count ? editNarasiLineSpeakerInputs[i] : null;
            AddLineFromForm(lines, speakerDropdown, editNarasiLineTextInputs[i]);
        }

        return lines;
    }

    private void AddLineFromForm(List<DialogKarakterLineData> lines, DropdownField speakerDropdown, TextField textField)
    {
        string speaker = speakerDropdown?.value;
        string text = textField?.value;
        if (string.IsNullOrWhiteSpace(speaker) && string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        lines.Add(new DialogKarakterLineData
        {
            speaker = string.IsNullOrWhiteSpace(speaker) ? "NPC" : speaker,
            text = text ?? string.Empty
        });
    }

    private void RefreshEditNarasiRuntimeCache(DialogKarakterDatabase database)
    {
        editNarasiDialogLookup.Clear();

        if (DataManager.Instance?.dialogKarakterDict != null)
        {
            DataManager.Instance.dialogKarakterDict.Clear();
        }

        if (database.dialogKarakter == null)
        {
            return;
        }

        foreach (DialogKarakterData dialog in database.dialogKarakter)
        {
            AddEditNarasiDialog(dialog);
            if (DataManager.Instance?.dialogKarakterDict != null && dialog != null && !string.IsNullOrWhiteSpace(dialog.id))
            {
                DataManager.Instance.dialogKarakterDict[dialog.id] = dialog;
            }
        }
    }

    private void PopulateEditNarasiForm(string dialogId)
    {
        if (string.IsNullOrWhiteSpace(dialogId) || dialogId == EmptyDialogOption || dialogId == NewDialogOption)
        {
            ClearEditNarasiForm();
            return;
        }

        if (!editNarasiDialogLookup.TryGetValue(dialogId, out DialogKarakterData dialogData))
        {
            RefreshEditNarasiDialogLookupFromCurrentSource();
        }

        if (!editNarasiDialogLookup.TryGetValue(dialogId, out dialogData))
        {
            Debug.LogWarning($"Dialog narasi tidak ditemukan: {dialogId}");
            ClearEditNarasiForm();
            return;
        }

        editNarasiSelectedDialogId = dialogId;
        editNarasiIsCreatingNew = false;
        SetTextWithoutNotify(editNarasiIdInput, dialogData.id);
        SetIntegerWithoutNotify(editNarasiAksiValueInput, dialogData.aksiValue);
        SetTextWithoutNotify(editNarasiNpcNameInput, dialogData.npcName);
        SetDropdownWithoutNotify(editNarasiActionTypeInput, dialogData.aksi);
        SetDropdownWithoutNotify(editNarasiNpcSpriteInput, dialogData.npcSprite);
        UpdateEditNarasiNpcSpritePreview(dialogData.npcSprite);
        SetQuestDropdownValue(editNarasiQuestEffectDropdown, dialogData.questId);
        SetDropdownWithoutNotify(editNarasiQuestEffectStateDropdown, QuestState.Normalize(dialogData.questState));
        PopulateEditNarasiPrerequisite(dialogData);
        PopulateEditNarasiLines(dialogData);
    }

    private void PopulateEditNarasiPrerequisite(DialogKarakterData dialogData)
    {
        DialogPrerequisiteData prerequisite = null;
        if (dialogData.prerequisite != null && dialogData.prerequisite.Count > 0)
        {
            prerequisite = dialogData.prerequisite[0];
        }

        SetIntegerWithoutNotify(editNarasiMinCoinInput, prerequisite?.uang ?? 0);
        SetIntegerWithoutNotify(editNarasiMinHappinessInput, prerequisite?.kebahagiaan ?? 0);
        SetIntegerWithoutNotify(editNarasiMinSavingInput, prerequisite?.tabungan ?? 0);
        SetIntegerWithoutNotify(editNarasiMinGoldInput, prerequisite?.emas ?? 0);
        SetIntegerWithoutNotify(editNarasiLoanCardInput, prerequisite?.kartuPinjaman ?? 0);
        SetIntegerWithoutNotify(editNarasiWeekInput, prerequisite?.mingguKe ?? 0);
        SetIntegerWithoutNotify(editNarasiDayInput, prerequisite?.hariKe ?? 0);
        SetToggleWithoutNotify(editNarasiHasInsuranceToggle, prerequisite?.asuransiDimiliki ?? false);
        SetTextWithoutNotify(editNarasiRequiredBahanInput, JoinPrerequisiteNames(prerequisite?.bahanDimiliki, NarafinEventCardIdResolver.ResolveBahanNameFromCardId));
        SetTextWithoutNotify(editNarasiRequiredKebutuhanInput, JoinPrerequisiteNames(prerequisite?.kebutuhanDimiliki, NarafinEventCardIdResolver.ResolveKebutuhanNameFromCardId));
        SetTextWithoutNotify(editNarasiRequiredTujuanFinansialInput, JoinPrerequisiteNames(prerequisite?.tujuanFinansialDimiliki, NarafinEventCardIdResolver.ResolveTujuanFinansialNameFromCardId));
        SetTextWithoutNotify(editNarasiRequiredMasakanInput, JoinPrerequisiteNames(prerequisite?.masakanDijual, NarafinEventCardIdResolver.ResolveResepNameFromCardId));
        SetQuestDropdownValue(editNarasiQuestPrereqDropdown, prerequisite?.questId);
        SetDropdownWithoutNotify(editNarasiQuestPrereqStateDropdown, QuestState.Normalize(prerequisite?.questState));
    }

    private void PopulateEditNarasiLines(DialogKarakterData dialogData)
    {
        int lineCount = Mathf.Max(2, dialogData?.lines?.Count ?? 0);
        EnsureEditNarasiLineCount(lineCount);

        for (int i = 0; i < editNarasiLineTextInputs.Count; i++)
        {
            PopulateEditNarasiLine(i, editNarasiLineSpeakerInputs[i], editNarasiLineTextInputs[i], dialogData);
        }
    }

    private void PopulateEditNarasiLine(int lineIndex, DropdownField speakerDropdown, TextField textField, DialogKarakterData dialogData)
    {
        if (dialogData?.lines == null || lineIndex < 0 || lineIndex >= dialogData.lines.Count)
        {
            speakerDropdown?.SetValueWithoutNotify(lineIndex % 2 == 0 ? "NPC" : "PLAYER");
            SetTextWithoutNotify(textField, string.Empty);
            return;
        }

        DialogKarakterLineData line = dialogData.lines[lineIndex];
        speakerDropdown?.SetValueWithoutNotify(string.IsNullOrWhiteSpace(line.speaker) ? "NPC" : line.speaker);
        SetTextWithoutNotify(textField, line.text);
    }

    private void AddEditNarasiLineInput(string speaker = "PLAYER", string text = "")
    {
        EnsureEditNarasiLineInputs();
        if (editNarasiLinesSection == null || editNarasiAddLineButton == null)
        {
            ShowErrorPopup("Area dialog line belum siap.");
            return;
        }

        int lineNumber = editNarasiLineTextInputs.Count + 1;
        VisualElement lineRow = new VisualElement { name = "EditNarasiLineRow" + lineNumber };
        lineRow.AddToClassList("edit-line-row");

        Button removeButton = new Button
        {
            name = "EditNarasiLineRemove" + lineNumber,
            text = "X"
        };
        removeButton.AddToClassList("edit-line-remove-button");
        RegisterEditNarasiLineRemoveButton(removeButton);

        DropdownField speakerDropdown = new DropdownField
        {
            name = "EditNarasiLineSpeaker" + lineNumber,
            choices = GetEditNarasiSpeakerChoices(),
            value = string.IsNullOrWhiteSpace(speaker) ? "PLAYER" : speaker
        };
        speakerDropdown.AddToClassList("edit-speaker-dropdown");

        TextField textField = new TextField
        {
            name = "EditNarasiLineText" + lineNumber,
            multiline = true,
            value = text ?? string.Empty
        };
        textField.AddToClassList("edit-line-text");

        lineRow.Add(removeButton);
        lineRow.Add(speakerDropdown);
        lineRow.Add(textField);

        int addButtonIndex = editNarasiLinesSection.IndexOf(editNarasiAddLineButton);
        if (addButtonIndex >= 0)
        {
            editNarasiLinesSection.Insert(addButtonIndex, lineRow);
        }
        else
        {
            editNarasiLinesSection.Add(lineRow);
        }

        editNarasiLineRemoveButtons.Add(removeButton);
        editNarasiLineSpeakerInputs.Add(speakerDropdown);
        editNarasiLineTextInputs.Add(textField);
        RefreshEditNarasiLineNames();
        textField.Focus();
    }

    private void EnsureEditNarasiLineCount(int targetCount)
    {
        EnsureEditNarasiLineInputs();
        targetCount = Mathf.Max(2, targetCount);

        while (editNarasiLineTextInputs.Count < targetCount)
        {
            AddEditNarasiLineInput(editNarasiLineTextInputs.Count % 2 == 0 ? "NPC" : "PLAYER");
        }

        while (editNarasiLineTextInputs.Count > targetCount)
        {
            RemoveLastEditNarasiLineInput();
        }
    }

    private void ResetEditNarasiLineInputsToBaseRows()
    {
        EnsureEditNarasiLineInputs();
        while (editNarasiLineTextInputs.Count > 0)
        {
            RemoveLastEditNarasiLineInput();
        }

        EnsureEditNarasiLineCount(2);
    }

    private void RemoveEditNarasiLineInput(Button removeButton)
    {
        if (removeButton == null)
        {
            return;
        }

        int index = editNarasiLineRemoveButtons.IndexOf(removeButton);
        if (index < 0)
        {
            return;
        }

        RemoveEditNarasiLineInputAt(index);
    }

    private void RemoveLastEditNarasiLineInput()
    {
        RemoveEditNarasiLineInputAt(editNarasiLineTextInputs.Count - 1);
    }

    private void RemoveEditNarasiLineInputAt(int index)
    {
        if (index < 0 || index >= editNarasiLineTextInputs.Count || index >= editNarasiLineSpeakerInputs.Count || index >= editNarasiLineRemoveButtons.Count)
        {
            return;
        }

        Button removeButton = editNarasiLineRemoveButtons[index];
        TextField textField = editNarasiLineTextInputs[index];
        DropdownField speakerDropdown = editNarasiLineSpeakerInputs[index];
        VisualElement row = removeButton?.parent ?? textField?.parent ?? speakerDropdown?.parent;
        row?.RemoveFromHierarchy();

        editNarasiLineRemoveButtons.RemoveAt(index);
        editNarasiLineTextInputs.RemoveAt(index);
        editNarasiLineSpeakerInputs.RemoveAt(index);
        RefreshEditNarasiLineNames();
    }

    private void RefreshEditNarasiLineNames()
    {
        int lineCount = Mathf.Min(editNarasiLineRemoveButtons.Count, Mathf.Min(editNarasiLineSpeakerInputs.Count, editNarasiLineTextInputs.Count));
        for (int i = 0; i < lineCount; i++)
        {
            int lineNumber = i + 1;
            if (editNarasiLineRemoveButtons[i] != null)
            {
                editNarasiLineRemoveButtons[i].name = "EditNarasiLineRemove" + lineNumber;
            }

            if (editNarasiLineSpeakerInputs[i] != null)
            {
                editNarasiLineSpeakerInputs[i].name = "EditNarasiLineSpeaker" + lineNumber;
            }

            if (editNarasiLineTextInputs[i] != null)
            {
                editNarasiLineTextInputs[i].name = "EditNarasiLineText" + lineNumber;
            }
        }
    }

    private void ClearEditNarasiForm()
    {
        SetTextWithoutNotify(editNarasiIdInput, string.Empty);
        SetIntegerWithoutNotify(editNarasiAksiValueInput, 0);
        SetTextWithoutNotify(editNarasiNpcNameInput, string.Empty);
        SetDropdownWithoutNotify(editNarasiActionTypeInput, string.Empty);
        SetDropdownWithoutNotify(editNarasiNpcSpriteInput, string.Empty);
        UpdateEditNarasiNpcSpritePreview(string.Empty);
        SetTextWithoutNotify(editNarasiRequiredBahanInput, string.Empty);
        SetTextWithoutNotify(editNarasiRequiredKebutuhanInput, string.Empty);
        SetTextWithoutNotify(editNarasiRequiredTujuanFinansialInput, string.Empty);
        SetTextWithoutNotify(editNarasiRequiredMasakanInput, string.Empty);
        ResetEditNarasiLineInputsToBaseRows();
        PopulateEditNarasiLines(null);

        SetIntegerWithoutNotify(editNarasiMinCoinInput, 0);
        SetIntegerWithoutNotify(editNarasiMinHappinessInput, 0);
        SetIntegerWithoutNotify(editNarasiMinSavingInput, 0);
        SetIntegerWithoutNotify(editNarasiMinGoldInput, 0);
        SetIntegerWithoutNotify(editNarasiLoanCardInput, 0);
        SetIntegerWithoutNotify(editNarasiWeekInput, 0);
        SetIntegerWithoutNotify(editNarasiDayInput, 0);
        SetToggleWithoutNotify(editNarasiHasInsuranceToggle, false);
    }


    private void UpdateEditNarasiNpcSpritePreview(string npcSpriteName)
    {
        if (editNarasiNpcSpritePreview == null)
        {
            return;
        }

        string safeSpriteName = Path.GetFileNameWithoutExtension((npcSpriteName ?? string.Empty).Trim());
        if (string.IsNullOrWhiteSpace(safeSpriteName))
        {
            editNarasiNpcSpritePreview.style.backgroundImage = StyleKeyword.None;
            editNarasiNpcSpritePreview.AddToClassList("edit-npc-sprite-preview-empty");
            return;
        }

        Sprite sprite = Resources.Load<Sprite>("Sprite/Character/NPC/" + safeSpriteName);
        if (sprite == null)
        {
            Debug.LogWarning("Preview sprite NPC tidak ditemukan di Resources: Sprite/Character/NPC/" + safeSpriteName);
            editNarasiNpcSpritePreview.style.backgroundImage = StyleKeyword.None;
            editNarasiNpcSpritePreview.AddToClassList("edit-npc-sprite-preview-empty");
            return;
        }

        editNarasiNpcSpritePreview.RemoveFromClassList("edit-npc-sprite-preview-empty");
        editNarasiNpcSpritePreview.style.backgroundImage = new StyleBackground(sprite);
    }

    private void OnEditNarasiNpcSpriteChanged(ChangeEvent<string> evt)
    {
        UpdateEditNarasiNpcSpritePreview(evt.newValue);
    }

    private List<string> ParseCommaSeparatedList(string value)
    {
        List<string> items = new List<string>();
        if (string.IsNullOrWhiteSpace(value))
        {
            return items;
        }

        string[] rawItems = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string rawItem in rawItems)
        {
            string item = rawItem.Trim();
            if (!string.IsNullOrWhiteSpace(item))
            {
                items.Add(item);
            }
        }

        return items;
    }

    private string JoinPrerequisiteNames(List<string> values, Func<string, string> resolveName)
    {
        if (values == null || values.Count == 0)
        {
            return string.Empty;
        }

        List<string> displayNames = new List<string>();
        foreach (string value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            string displayName = resolveName != null ? resolveName(value.Trim()) : value.Trim();
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                displayNames.Add(displayName);
            }
        }

        return string.Join(", ", displayNames);
    }

    private void SetIntegerWithoutNotify(IntegerField integerField, int value)
    {
        integerField?.SetValueWithoutNotify(value);
    }

    private void SetToggleWithoutNotify(Toggle toggle, bool value)
    {
        toggle?.SetValueWithoutNotify(value);
    }

    private void SetDropdownWithoutNotify(DropdownField dropdown, string value)
    {
        if (dropdown == null)
        {
            return;
        }

        string safeValue = value ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(safeValue))
        {
            List<string> choices = dropdown.choices ?? new List<string>();
            if (!choices.Contains(safeValue))
            {
                choices = new List<string>(choices) { safeValue };
                dropdown.choices = choices;
            }
        }

        dropdown.SetValueWithoutNotify(safeValue);
    }

    private void SetTextWithoutNotify(TextField textField, string value)
    {
        textField?.SetValueWithoutNotify(value ?? string.Empty);
    }
}
