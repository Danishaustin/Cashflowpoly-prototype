using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManager
{
    private const string EmptyQuestOption = "Tidak ada quest";
    private const string EmptyQuestPackOption = "Tidak ada paket quest";
    private const string NewQuestOption = "Quest baru (belum disimpan)";
    private const string NoQuestOption = "(tidak ada)";
    private const string MissingQuestSuffix = " (tidak ada di paket quest)";
    private const string EditQuestPackIdPlayerPrefsKey = "Narafin.EditQuestPackId";

    private Button editQuestButton;
    private Button backEditPemilihanQuestButton;
    private Button editQuestPackNewButton;
    private Button editQuestPackDeleteButton;
    private Button nextEditPemilihanQuestButton;
    private Button backEditQuestButton;
    private Button editQuestNewButton;
    private Button editQuestDeleteButton;
    private Button editQuestResetButton;
    private Button editQuestSaveButton;
    private DropdownField editQuestPackDropdown;
    private DropdownField editQuestDropdown;
    private TextField editQuestActivePackNameInput;
    private TextField editQuestIdInput;
    private TextField editQuestNamaInput;
    private TextField editQuestPerintahInput;
    private VisualElement editPemilihanQuestContainer;
    private VisualElement editQuestContainer;

    private readonly Dictionary<string, QuestPackData> editQuestPackLookup = new Dictionary<string, QuestPackData>();
    private readonly Dictionary<string, QuestData> editQuestLookup = new Dictionary<string, QuestData>();
    private readonly Dictionary<string, string> editNarasiQuestOptionLookup = new Dictionary<string, string>();
    private QuestPackData editQuestActivePack;
    private string editQuestSelectedId;
    private bool editQuestIsCreatingNew;
    private bool isEditQuestLoading;

    private void BindEditQuestElements(VisualElement root)
    {
        editQuestButton = root.Q<Button>("EditQuestButton");
        backEditPemilihanQuestButton = root.Q<Button>("BackEditPemilihanQuestButton");
        editQuestPackNewButton = root.Q<Button>("EditQuestPackNewButton");
        editQuestPackDeleteButton = root.Q<Button>("EditQuestPackDeleteButton");
        nextEditPemilihanQuestButton = root.Q<Button>("NextEditPemilihanQuestButton");
        backEditQuestButton = root.Q<Button>("BackEditQuestButton");
        editQuestNewButton = root.Q<Button>("EditQuestNewButton");
        editQuestDeleteButton = root.Q<Button>("EditQuestDeleteButton");
        editQuestResetButton = root.Q<Button>("EditQuestResetButton");
        editQuestSaveButton = root.Q<Button>("EditQuestSaveButton");
        editQuestPackDropdown = root.Q<DropdownField>("EditQuestPackDropdown");
        editQuestDropdown = root.Q<DropdownField>("EditQuestDropdown");
        editQuestActivePackNameInput = root.Q<TextField>("EditQuestActivePackNameInput");
        editQuestIdInput = root.Q<TextField>("EditQuestIdInput");
        editQuestNamaInput = root.Q<TextField>("EditQuestNamaInput");
        editQuestPerintahInput = root.Q<TextField>("EditQuestPerintahInput");
        editPemilihanQuestContainer = root.Q<VisualElement>("EditPemilihanQuestContainer");
        editQuestContainer = root.Q<VisualElement>("EditQuestContainer");
    }

    private void RegisterEditQuestCallbacks()
    {
        editQuestButton?.RegisterCallback<ClickEvent>(OnEditQuestClicked);
        backEditPemilihanQuestButton?.RegisterCallback<ClickEvent>(OnBackEditPemilihanQuestClicked);
        editQuestPackNewButton?.RegisterCallback<ClickEvent>(OnEditQuestPackNewClicked);
        editQuestPackDeleteButton?.RegisterCallback<ClickEvent>(OnEditQuestPackDeleteClicked);
        nextEditPemilihanQuestButton?.RegisterCallback<ClickEvent>(OnNextEditPemilihanQuestClicked);
        backEditQuestButton?.RegisterCallback<ClickEvent>(OnBackEditQuestClicked);
        editQuestNewButton?.RegisterCallback<ClickEvent>(OnEditQuestNewClicked);
        editQuestDeleteButton?.RegisterCallback<ClickEvent>(OnEditQuestDeleteClicked);
        editQuestResetButton?.RegisterCallback<ClickEvent>(OnEditQuestResetClicked);
        editQuestSaveButton?.RegisterCallback<ClickEvent>(OnEditQuestSaveClicked);
        editQuestDropdown?.RegisterValueChangedCallback(OnEditQuestSelectionChanged);
    }

    private void ResetEditQuestNavigationState()
    {
        editPemilihanQuestContainer?.RemoveFromClassList("show-edit-narasi-select");
        editPemilihanQuestContainer?.RemoveFromClassList("hide-edit-narasi-select-left");
        editQuestContainer?.RemoveFromClassList("show-edit-narasi");
    }

    private async void OnEditQuestClicked(ClickEvent evt)
    {
        if (isEditQuestLoading)
        {
            return;
        }

        QuestPackRepository.ClearLastCloudWarning();
        BeginEditQuestLoading("Memuat daftar quest...");

        try
        {
            await SetupEditQuestPackSelectionAsync();
            editContainer?.AddToClassList("hide-edit-left");
            editPemilihanQuestContainer?.RemoveFromClassList("hide-edit-narasi-select-left");
            editPemilihanQuestContainer?.AddToClassList("show-edit-narasi-select");
        }
        catch (Exception ex)
        {
            Debug.LogError("Gagal membuka menu edit quest: " + ex.Message);
            ShowErrorPopup("Gagal membuka menu edit quest. Detail: " + ex.Message);
        }
        finally
        {
            EndEditQuestLoading();
        }

        ShowEditQuestCloudWarningIfNeeded();
    }

    private void OnBackEditPemilihanQuestClicked(ClickEvent evt)
    {
        editPemilihanQuestContainer?.RemoveFromClassList("show-edit-narasi-select");
        editPemilihanQuestContainer?.RemoveFromClassList("hide-edit-narasi-select-left");
        editContainer?.RemoveFromClassList("hide-edit-left");
        editContainer?.AddToClassList("show-edit");
    }

    private async void OnNextEditPemilihanQuestClicked(ClickEvent evt)
    {
        if (isEditQuestLoading)
        {
            return;
        }

        QuestPackRepository.ClearLastCloudWarning();
        BeginEditQuestLoading("Memuat isi quest...");

        try
        {
            if (!await SelectQuestPackForEditingAsync())
            {
                return;
            }

            editPemilihanQuestContainer?.AddToClassList("hide-edit-narasi-select-left");
            editQuestContainer?.AddToClassList("show-edit-narasi");
        }
        catch (Exception ex)
        {
            Debug.LogError("Gagal memuat isi quest: " + ex.Message);
            ShowErrorPopup("Gagal memuat isi quest. Detail: " + ex.Message);
        }
        finally
        {
            EndEditQuestLoading();
        }

        ShowEditQuestCloudWarningIfNeeded();
    }

    private async void OnBackEditQuestClicked(ClickEvent evt)
    {
        if (isEditQuestLoading)
        {
            return;
        }

        QuestPackRepository.ClearLastCloudWarning();
        BeginEditQuestLoading("Memuat ulang daftar quest...");

        try
        {
            await SetupEditQuestPackSelectionAsync();
            editQuestContainer?.RemoveFromClassList("show-edit-narasi");
            editPemilihanQuestContainer?.RemoveFromClassList("hide-edit-narasi-select-left");
            editPemilihanQuestContainer?.AddToClassList("show-edit-narasi-select");
        }
        catch (Exception ex)
        {
            Debug.LogError("Gagal kembali ke daftar quest: " + ex.Message);
            ShowErrorPopup("Gagal kembali ke daftar quest. Detail: " + ex.Message);
        }
        finally
        {
            EndEditQuestLoading();
        }

        ShowEditQuestCloudWarningIfNeeded();
    }

    private async void OnEditQuestPackNewClicked(ClickEvent evt)
    {
        if (isEditQuestLoading)
        {
            return;
        }

        QuestPackRepository.ClearLastCloudWarning();
        BeginEditQuestLoading("Membuat paket quest baru...");

        try
        {
            QuestPackCreateResult result = await QuestPackRepository.CreateNewPackAsync();
            if (!result.Success)
            {
                ShowErrorPopup(result.ErrorMessage);
                return;
            }

            editQuestActivePack = result.Pack;
            await SetupEditQuestPackSelectionAsync();
            ShowEditQuestCloudWarningOrSuccess("Paket quest baru berhasil dibuat. Kamu bisa ubah namanya setelah menekan Next.");
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        catch (Exception ex)
        {
            Debug.LogError("Gagal membuat paket quest: " + ex.Message);
            ShowErrorPopup("Gagal membuat paket quest. Detail: " + ex.Message);
        }
        finally
        {
            EndEditQuestLoading();
        }
    }

    private async void OnEditQuestPackDeleteClicked(ClickEvent evt)
    {
        if (isEditQuestLoading)
        {
            return;
        }

        string selectedOption = editQuestPackDropdown?.value;
        if (string.IsNullOrWhiteSpace(selectedOption) || !editQuestPackLookup.TryGetValue(selectedOption, out QuestPackData selectedPack))
        {
            ShowErrorPopup("Pilih paket quest yang ingin dihapus.");
            return;
        }

        QuestPackRepository.ClearLastCloudWarning();
        BeginEditQuestLoading("Menghapus paket quest...");

        try
        {
            QuestPackOperationResult result = await QuestPackRepository.DeletePackAsync(selectedPack.id);
            if (!result.Success)
            {
                ShowErrorPopup(result.ErrorMessage);
                return;
            }

            if (editQuestActivePack != null && editQuestActivePack.id == selectedPack.id)
            {
                editQuestActivePack = null;
                editQuestSelectedId = null;
                editQuestIsCreatingNew = false;
                ClearEditQuestForm();
            }

            await SetupEditQuestPackSelectionAsync();
            ShowEditQuestCloudWarningOrSuccess("Paket quest berhasil dihapus.");
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        catch (Exception ex)
        {
            Debug.LogError("Gagal menghapus paket quest: " + ex.Message);
            ShowErrorPopup("Gagal menghapus paket quest. Detail: " + ex.Message);
        }
        finally
        {
            EndEditQuestLoading();
        }
    }

    private void OnEditQuestNewClicked(ClickEvent evt)
    {
        if (isEditQuestLoading)
        {
            return;
        }

        BeginNewEditQuest();
    }

    private void OnEditQuestResetClicked(ClickEvent evt)
    {
        if (isEditQuestLoading)
        {
            return;
        }

        if (editQuestIsCreatingNew)
        {
            BeginNewEditQuest();
            return;
        }

        PopulateEditQuestForm(editQuestSelectedId);
    }

    private async void OnEditQuestSaveClicked(ClickEvent evt)
    {
        if (isEditQuestLoading)
        {
            return;
        }

        QuestPackRepository.ClearLastCloudWarning();
        BeginEditQuestLoading("Menyimpan quest...");

        try
        {
            await SaveEditQuestFormAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError("Gagal menyimpan quest: " + ex.Message);
            ShowErrorPopup("Gagal menyimpan quest. Detail: " + ex.Message);
        }
        finally
        {
            EndEditQuestLoading();
        }
    }

    private async void OnEditQuestDeleteClicked(ClickEvent evt)
    {
        if (isEditQuestLoading)
        {
            return;
        }

        QuestPackRepository.ClearLastCloudWarning();
        BeginEditQuestLoading("Menghapus quest...");

        try
        {
            await DeleteSelectedEditQuestAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError("Gagal menghapus quest: " + ex.Message);
            ShowErrorPopup("Gagal menghapus quest. Detail: " + ex.Message);
        }
        finally
        {
            EndEditQuestLoading();
        }
    }

    private void OnEditQuestSelectionChanged(ChangeEvent<string> evt)
    {
        PopulateEditQuestForm(evt.newValue);
    }

    private async Task SetupEditQuestPackSelectionAsync()
    {
        editQuestPackLookup.Clear();
        QuestManifestData manifest = await QuestPackRepository.LoadManifestAsync();
        List<string> packOptions = new List<string>();

        if (manifest?.questPacks != null)
        {
            foreach (QuestPackData pack in manifest.questPacks)
            {
                if (pack == null || string.IsNullOrWhiteSpace(pack.id) || string.IsNullOrWhiteSpace(pack.file))
                {
                    continue;
                }

                string label = string.IsNullOrWhiteSpace(pack.name) ? pack.id : pack.name;
                string uniqueLabel = label;
                int duplicateCount = 2;
                while (editQuestPackLookup.ContainsKey(uniqueLabel))
                {
                    uniqueLabel = label + " (" + duplicateCount + ")";
                    duplicateCount++;
                }

                editQuestPackLookup[uniqueLabel] = pack;
                packOptions.Add(uniqueLabel);
            }
        }

        if (editQuestPackDropdown == null)
        {
            return;
        }

        if (packOptions.Count == 0)
        {
            editQuestPackDropdown.choices = new List<string> { EmptyQuestPackOption };
            editQuestPackDropdown.SetValueWithoutNotify(EmptyQuestPackOption);
            return;
        }

        editQuestPackDropdown.choices = packOptions;
        string selectedOption = FindActiveQuestPackOption(packOptions) ?? packOptions[0];
        editQuestPackDropdown.SetValueWithoutNotify(selectedOption);
    }

    private string FindActiveQuestPackOption(List<string> options)
    {
        if (editQuestActivePack == null || options == null)
        {
            return null;
        }

        foreach (string option in options)
        {
            if (editQuestPackLookup.TryGetValue(option, out QuestPackData pack) && pack.id == editQuestActivePack.id)
            {
                return option;
            }
        }

        return null;
    }

    private async Task<bool> SelectQuestPackForEditingAsync()
    {
        string selectedOption = editQuestPackDropdown?.value;
        if (string.IsNullOrWhiteSpace(selectedOption) || !editQuestPackLookup.TryGetValue(selectedOption, out QuestPackData selectedPack))
        {
            ShowErrorPopup("Pilih paket quest terlebih dahulu.");
            return false;
        }

        editQuestActivePack = selectedPack;
        SetTextWithoutNotify(editQuestActivePackNameInput, selectedPack.name);

        // Editor narasi memakai paket ini untuk mengisi dropdown quest.
        PlayerPrefs.SetString(EditQuestPackIdPlayerPrefsKey, selectedPack.id ?? string.Empty);
        PlayerPrefs.Save();

        await SetupEditQuestDropdownAsync();
        return true;
    }

    private async Task<QuestDatabase> LoadEditQuestDatabaseAsync()
    {
        if (editQuestActivePack == null)
        {
            return QuestPackRepository.LoadFallbackQuestDatabase();
        }

        return await QuestPackRepository.LoadQuestDatabaseAsync(editQuestActivePack);
    }

    private async Task SetupEditQuestDropdownAsync()
    {
        editQuestLookup.Clear();
        QuestDatabase database = await LoadEditQuestDatabaseAsync();
        List<string> options = new List<string>();

        if (database?.quest != null)
        {
            foreach (QuestData quest in database.quest)
            {
                if (quest == null || string.IsNullOrWhiteSpace(quest.id))
                {
                    continue;
                }

                string label = BuildQuestOptionLabel(quest);
                string uniqueLabel = label;
                int duplicateCount = 2;
                while (editQuestLookup.ContainsKey(uniqueLabel))
                {
                    uniqueLabel = label + " (" + duplicateCount + ")";
                    duplicateCount++;
                }

                editQuestLookup[uniqueLabel] = quest;
                options.Add(uniqueLabel);
            }
        }

        if (editQuestDropdown == null)
        {
            return;
        }

        if (options.Count == 0)
        {
            editQuestDropdown.choices = new List<string> { EmptyQuestOption };
            editQuestDropdown.SetValueWithoutNotify(EmptyQuestOption);
            editQuestSelectedId = null;
            ClearEditQuestForm();
            return;
        }

        editQuestDropdown.choices = options;
        string selectedOption = FindQuestOptionById(options, editQuestSelectedId) ?? options[0];
        editQuestDropdown.SetValueWithoutNotify(selectedOption);
        PopulateEditQuestForm(selectedOption);
    }

    private string FindQuestOptionById(List<string> options, string questId)
    {
        if (string.IsNullOrWhiteSpace(questId) || options == null)
        {
            return null;
        }

        foreach (string option in options)
        {
            if (editQuestLookup.TryGetValue(option, out QuestData quest) && quest.id == questId)
            {
                return option;
            }
        }

        return null;
    }

    private static string BuildQuestOptionLabel(QuestData quest)
    {
        return string.IsNullOrWhiteSpace(quest.nama) ? quest.id : quest.nama;
    }

    private void PopulateEditQuestForm(string questOption)
    {
        if (string.IsNullOrWhiteSpace(questOption) || questOption == EmptyQuestOption || questOption == NewQuestOption)
        {
            ClearEditQuestForm();
            return;
        }

        if (!editQuestLookup.TryGetValue(questOption, out QuestData quest))
        {
            string option = FindQuestOptionById(new List<string>(editQuestLookup.Keys), questOption);
            if (option == null || !editQuestLookup.TryGetValue(option, out quest))
            {
                ClearEditQuestForm();
                return;
            }
        }

        editQuestSelectedId = quest.id;
        editQuestIsCreatingNew = false;
        SetTextWithoutNotify(editQuestIdInput, quest.id);
        SetTextWithoutNotify(editQuestNamaInput, quest.nama);
        SetTextWithoutNotify(editQuestPerintahInput, quest.perintah);
    }

    private void ClearEditQuestForm()
    {
        SetTextWithoutNotify(editQuestIdInput, string.Empty);
        SetTextWithoutNotify(editQuestNamaInput, string.Empty);
        SetTextWithoutNotify(editQuestPerintahInput, string.Empty);
    }

    private void BeginNewEditQuest()
    {
        editQuestIsCreatingNew = true;
        editQuestSelectedId = null;
        ClearEditQuestForm();

        if (editQuestDropdown == null)
        {
            return;
        }

        List<string> choices = new List<string>(editQuestDropdown.choices);
        if (!choices.Contains(NewQuestOption))
        {
            choices.Add(NewQuestOption);
            editQuestDropdown.choices = choices;
        }

        editQuestDropdown.SetValueWithoutNotify(NewQuestOption);
    }

    private async Task<bool> SaveActiveQuestPackNameAsync()
    {
        if (editQuestActivePack == null)
        {
            return true;
        }

        string activePackName = editQuestActivePackNameInput?.value?.Trim();
        if (string.IsNullOrWhiteSpace(activePackName))
        {
            ShowErrorPopup("Nama quest pack tidak boleh kosong.");
            return false;
        }

        if (activePackName == editQuestActivePack.name)
        {
            return true;
        }

        QuestPackOperationResult result = await QuestPackRepository.UpdatePackNameAsync(editQuestActivePack.id, activePackName);
        if (!result.Success)
        {
            ShowErrorPopup(result.ErrorMessage);
            return false;
        }

        editQuestActivePack.name = activePackName;
        return true;
    }

    private async Task<bool> SaveEditQuestFormAsync()
    {
        if (!await SaveActiveQuestPackNameAsync())
        {
            return false;
        }

        string questId = editQuestIdInput?.value?.Trim();
        if (string.IsNullOrWhiteSpace(questId))
        {
            ShowErrorPopup("Quest ID tidak boleh kosong.");
            return false;
        }

        string questNama = editQuestNamaInput?.value?.Trim();
        if (string.IsNullOrWhiteSpace(questNama))
        {
            ShowErrorPopup("Nama quest tidak boleh kosong.");
            return false;
        }

        string questPerintah = editQuestPerintahInput?.value?.Trim();
        if (string.IsNullOrWhiteSpace(questPerintah))
        {
            ShowErrorPopup("Perintah quest tidak boleh kosong.");
            return false;
        }

        QuestDatabase database = await LoadEditQuestDatabaseAsync();
        if (database == null)
        {
            database = new QuestDatabase();
        }

        if (database.quest == null)
        {
            database.quest = new List<QuestData>();
        }

        int existingIndex = FindQuestIndex(database.quest, editQuestSelectedId);
        int duplicateIndex = FindQuestIndex(database.quest, questId);
        bool isChangingId = !string.IsNullOrWhiteSpace(editQuestSelectedId) && editQuestSelectedId != questId;

        if ((editQuestIsCreatingNew || isChangingId) && duplicateIndex >= 0)
        {
            ShowErrorPopup("Quest ID sudah digunakan. Gunakan ID lain.");
            return false;
        }

        QuestData formData = new QuestData
        {
            id = questId,
            nama = questNama,
            perintah = questPerintah
        };

        if (existingIndex >= 0)
        {
            database.quest[existingIndex] = formData;
        }
        else
        {
            database.quest.Add(formData);
        }

        await QuestPackRepository.SaveQuestDatabaseAsync(editQuestActivePack, database);
        editQuestIsCreatingNew = false;
        editQuestSelectedId = questId;
        await SetupEditQuestDropdownAsync();
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
        ShowEditQuestCloudWarningOrSuccess("Save berhasil. Quest berhasil disimpan.");
        return true;
    }

    private async Task<bool> DeleteSelectedEditQuestAsync()
    {
        if (string.IsNullOrWhiteSpace(editQuestSelectedId))
        {
            ShowErrorPopup("Pilih quest yang ingin dihapus.");
            return false;
        }

        QuestDatabase database = await LoadEditQuestDatabaseAsync();
        int existingIndex = FindQuestIndex(database?.quest, editQuestSelectedId);
        if (existingIndex < 0)
        {
            ShowErrorPopup("Quest yang dipilih tidak ditemukan.");
            return false;
        }

        database.quest.RemoveAt(existingIndex);
        await QuestPackRepository.SaveQuestDatabaseAsync(editQuestActivePack, database);

        editQuestSelectedId = null;
        editQuestIsCreatingNew = false;
        await SetupEditQuestDropdownAsync();
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
        ShowEditQuestCloudWarningOrSuccess("Quest berhasil dihapus.");
        return true;
    }

    private int FindQuestIndex(List<QuestData> quests, string questId)
    {
        if (quests == null || string.IsNullOrWhiteSpace(questId))
        {
            return -1;
        }

        for (int i = 0; i < quests.Count; i++)
        {
            if (quests[i] != null && quests[i].id == questId)
            {
                return i;
            }
        }

        return -1;
    }

    // Dropdown quest pada editor narasi memakai paket quest yang terakhir dibuka di Edit Quest.
    private async Task SetupEditNarasiQuestDropdownsAsync()
    {
        editNarasiQuestOptionLookup.Clear();
        List<string> questOptions = new List<string> { NoQuestOption };

        QuestDatabase database = await LoadNarasiQuestDatabaseAsync();
        if (database?.quest != null)
        {
            foreach (QuestData quest in database.quest)
            {
                if (quest == null || string.IsNullOrWhiteSpace(quest.id))
                {
                    continue;
                }

                string label = BuildQuestOptionLabel(quest) + " [" + quest.id + "]";
                if (editNarasiQuestOptionLookup.ContainsKey(label))
                {
                    continue;
                }

                editNarasiQuestOptionLookup[label] = quest.id;
                questOptions.Add(label);
            }
        }

        ConfigureDropdown(editNarasiQuestEffectDropdown, questOptions, NoQuestOption);
        ConfigureDropdown(editNarasiQuestPrereqDropdown, new List<string>(questOptions), NoQuestOption);

        List<string> stateOptions = new List<string>(QuestState.All);
        ConfigureDropdown(editNarasiQuestEffectStateDropdown, stateOptions, QuestState.Aktif);
        ConfigureDropdown(editNarasiQuestPrereqStateDropdown, new List<string>(stateOptions), QuestState.Aktif);
    }

    private async Task<QuestDatabase> LoadNarasiQuestDatabaseAsync()
    {
        string packId = PlayerPrefs.GetString(EditQuestPackIdPlayerPrefsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(packId))
        {
            return QuestPackRepository.LoadFallbackQuestDatabase();
        }

        QuestManifestData manifest = await QuestPackRepository.LoadManifestAsync();
        if (manifest?.questPacks != null)
        {
            foreach (QuestPackData pack in manifest.questPacks)
            {
                if (pack != null && pack.id == packId)
                {
                    return await QuestPackRepository.LoadQuestDatabaseAsync(pack);
                }
            }
        }

        return QuestPackRepository.LoadFallbackQuestDatabase();
    }

    private string ResolveQuestIdFromOption(string option)
    {
        if (string.IsNullOrWhiteSpace(option) || option == NoQuestOption)
        {
            return string.Empty;
        }

        if (editNarasiQuestOptionLookup.TryGetValue(option, out string questId))
        {
            return questId;
        }

        // Quest dari paket lain tetap dipertahankan supaya tidak hilang saat dialog disimpan ulang.
        if (option.EndsWith(MissingQuestSuffix, StringComparison.Ordinal))
        {
            return option.Substring(0, option.Length - MissingQuestSuffix.Length);
        }

        return string.Empty;
    }

    // Bila quest yang dirujuk dialog tidak ada di paket quest terpilih, pilihannya ditambahkan dengan penanda.
    private void SetQuestDropdownValue(DropdownField dropdown, string questId)
    {
        if (dropdown == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(questId))
        {
            dropdown.SetValueWithoutNotify(NoQuestOption);
            return;
        }

        foreach (KeyValuePair<string, string> entry in editNarasiQuestOptionLookup)
        {
            if (entry.Value == questId && dropdown.choices.Contains(entry.Key))
            {
                dropdown.SetValueWithoutNotify(entry.Key);
                return;
            }
        }

        string missingOption = questId + MissingQuestSuffix;
        List<string> choices = new List<string>(dropdown.choices);
        if (!choices.Contains(missingOption))
        {
            choices.Add(missingOption);
            dropdown.choices = choices;
        }

        dropdown.SetValueWithoutNotify(missingOption);
    }

    private void BeginEditQuestLoading(string message)
    {
        if (isEditQuestLoading)
        {
            return;
        }

        isEditQuestLoading = true;
        SetEditQuestButtonsEnabled(false);
        BeginPlayLoading(message);
    }

    private void EndEditQuestLoading()
    {
        EndPlayLoading();
        SetEditQuestButtonsEnabled(true);
        isEditQuestLoading = false;
    }

    private void SetEditQuestButtonsEnabled(bool enabled)
    {
        editButton?.SetEnabled(enabled);
        backEditButton?.SetEnabled(enabled);
        editQuestButton?.SetEnabled(enabled);
        backEditPemilihanQuestButton?.SetEnabled(enabled);
        backEditQuestButton?.SetEnabled(enabled);
        editQuestPackNewButton?.SetEnabled(enabled);
        editQuestPackDeleteButton?.SetEnabled(enabled);
        nextEditPemilihanQuestButton?.SetEnabled(enabled);
        editQuestNewButton?.SetEnabled(enabled);
        editQuestDeleteButton?.SetEnabled(enabled);
        editQuestResetButton?.SetEnabled(enabled);
        editQuestSaveButton?.SetEnabled(enabled);
    }

    private void ShowEditQuestCloudWarningIfNeeded()
    {
        string warningMessage = QuestPackRepository.LastCloudWarningMessage;
        if (!string.IsNullOrWhiteSpace(warningMessage))
        {
            ShowErrorPopup(warningMessage);
        }
    }

    private void ShowEditQuestCloudWarningOrSuccess(string successMessage)
    {
        string warningMessage = QuestPackRepository.LastCloudWarningMessage;
        if (!string.IsNullOrWhiteSpace(warningMessage))
        {
            ShowErrorPopup(warningMessage);
            return;
        }

        ShowSuccessPopup(successMessage);
    }
}
