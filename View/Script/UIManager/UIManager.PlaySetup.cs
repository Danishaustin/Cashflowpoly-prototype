using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManager
{
    private const int PlayerSuggestionLimit = 4;
    private static readonly List<string> RulesetModeChoices = new List<string> { "Pemula", "Mahir", "Custom" };
    private const string RulesetModePlayerPrefsKey = "Narafin.RulesetMode";
    private const string RulesetPlayerPrefsKey = "Narafin.Ruleset";
    private const string RulesetIdPlayerPrefsKey = "Narafin.RulesetId";
    private const string RulesetVersionPlayerPrefsKey = "Narafin.RulesetVersion";
    private const string NarasiPackIdPlayerPrefsKey = "Narafin.NarasiPackId";
    private const string NarasiPackNamePlayerPrefsKey = "Narafin.NarasiPackName";

    private readonly List<NarafinPlayerSummary> currentPlayerSuggestions = new List<NarafinPlayerSummary>();
    private int activePlayerNameInputIndex = -1;

    // Player count, player names, validation, and starting the play scene.
    private void SetupPlayerCountDropdown()
    {
        playerCountDropdown.choices = new List<string>() { "3", "4" };
        playerCountDropdown.value = "3";
        playerCountDropdown.RegisterValueChangedCallback(evt => UpdatePlayerNameInputs());
        UpdatePlayerNameInputs();
        SetupRulesetDropdown();
    }

    private void OnPlayClicked(ClickEvent evt)
    {
        Debug.Log("Play button clicked!");
        if (isPlayLoading)
        {
            return;
        }

        ClearSessionSetupAndPlayInputs();
        _ = LoadPlayBootstrapAsync();
    }

    private void ClearSessionSetupAndPlayInputs()
    {
        if (sessionNameInput != null)
        {
            sessionNameInput.SetValueWithoutNotify(string.Empty);
        }

        if (rulesetSearchInput != null)
        {
            rulesetSearchInput.SetValueWithoutNotify(string.Empty);
        }

        if (rulesetModeDropdown != null)
        {
            rulesetModeDropdown.SetValueWithoutNotify("Mahir");
        }

        if (rulesetDropdown != null)
        {
            rulesetDropdown.SetValueWithoutNotify(string.Empty);
        }

        if (narasiPackDropdown != null)
        {
            narasiPackDropdown.SetValueWithoutNotify(NarasiSessionContext.DefaultOptionName);
        }

        if (playerCountDropdown != null)
        {
            playerCountDropdown.SetValueWithoutNotify("3");
        }

        if (playerNameInputs != null)
        {
            for (int i = 0; i < playerNameInputs.Length; i++)
            {
                if (playerNameInputs[i] != null)
                {
                    playerNameInputs[i].SetValueWithoutNotify(string.Empty);
                }
            }
        }

        playValidationText.text = string.Empty;
        sessionSetupValidationText.text = string.Empty;
        activePlayerNameInputIndex = -1;

        HidePlayerSuggestions();
        RefreshRulesetDropdownOptions();
        UpdatePlayerNameInputs();
    }

    private async System.Threading.Tasks.Task LoadPlayBootstrapAsync()
    {
        BeginPlayLoading("Memuat data...");
        try
        {
            if (LoginManager.Instance == null)
            {
                ShowErrorPopup("Sistem login belum siap.");
                return;
            }

            NarafinPlayBootstrapResult result = await LoginManager.Instance.LoadPlayBootstrapDataAsync();

            if (!result.Success)
            {
                ShowErrorPopup(result.ErrorMessage);
                return;
            }

            SetupRulesetDropdown();
            await SetupNarasiPackDropdownAsync();
            ShowSessionSetup();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Bootstrap Play gagal: " + ex.Message);
            ShowErrorPopup("Gagal memuat data ruleset dan player.");
        }
        finally
        {
            EndPlayLoading();
        }
    }

    private void OnBackSessionSetupClicked(ClickEvent evt)
    {
        Debug.Log("Back Session Setup button clicked!");
        sessionSetupContainer.RemoveFromClassList("hide-session-setup-left");
        sessionSetupContainer.RemoveFromClassList("show-session-setup");
        playContainer.RemoveFromClassList("show-play");
        sessionSetupValidationText.text = string.Empty;
    }

    private void OnNextSessionSetupClicked(ClickEvent evt)
    {
        Debug.Log("Next Session Setup button clicked!");

        string sessionName = sessionNameInput.value.Trim();
        string rulesetName = rulesetDropdown != null ? rulesetDropdown.value.Trim() : string.Empty;

        if (sessionName == string.Empty)
        {
            sessionSetupValidationText.text = "Nama session tidak boleh kosong.";
            return;
        }

        if (string.IsNullOrWhiteSpace(rulesetName))
        {
            sessionSetupValidationText.text = "Ruleset harus dipilih.";
            return;
        }

        NarafinRulesetSummary selectedRuleset = GetSelectedRulesetSummary(rulesetName);
        if (selectedRuleset == null)
        {
            sessionSetupValidationText.text = "Pilih ruleset dari daftar.";
            return;
        }

        sessionSetupValidationText.text = string.Empty;
        PlayerPrefs.SetString(RulesetPlayerPrefsKey, rulesetName);
        PlayerPrefs.SetString(RulesetModePlayerPrefsKey, rulesetModeDropdown != null ? rulesetModeDropdown.value : "Mahir");

        PlayerPrefs.SetString(RulesetIdPlayerPrefsKey, selectedRuleset.ruleset_id ?? string.Empty);
        PlayerPrefs.SetInt(RulesetVersionPlayerPrefsKey, selectedRuleset.latest_version);

        NarasiPackData selectedNarasiPack = GetSelectedNarasiPack();
        PlayerPrefs.SetString(NarasiPackIdPlayerPrefsKey, selectedNarasiPack != null ? selectedNarasiPack.id ?? string.Empty : string.Empty);
        PlayerPrefs.SetString(NarasiPackNamePlayerPrefsKey, selectedNarasiPack != null ? selectedNarasiPack.name ?? string.Empty : NarasiSessionContext.DefaultOptionName);

        NarafinSessionScope.BeginNewSession(sessionName);

        PlayerPrefs.Save();

        sessionSetupContainer.AddToClassList("hide-session-setup-left");
        playContainer.AddToClassList("show-play");
    }

    private void OnBackPlayClicked(ClickEvent evt)
    {
        Debug.Log("Back Play button clicked!");
        playContainer.RemoveFromClassList("show-play");
        sessionSetupContainer.RemoveFromClassList("hide-session-setup-left");
        sessionSetupContainer.AddToClassList("show-session-setup");
        playValidationText.text = string.Empty;
    }

    private async void OnPlay2Clicked(ClickEvent evt)
    {
        Debug.Log("Play2 button clicked!");
        if (isPlayLoading)
        {
            return;
        }

        HidePlayerSuggestions();
        playValidationText.text = string.Empty;

        if (LoginManager.Instance == null)
        {
            ShowErrorPopup("Sistem login belum siap.");
            return;
        }

        string sessionName = sessionNameInput != null ? sessionNameInput.value.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(sessionName))
        {
            ShowErrorPopup("Nama session tidak boleh kosong.");
            return;
        }

        string selectedRulesetMode = PlayerPrefs.GetString(RulesetModePlayerPrefsKey, rulesetModeDropdown != null ? rulesetModeDropdown.value : "Mahir");
        string selectedRulesetName = PlayerPrefs.GetString(RulesetPlayerPrefsKey, rulesetDropdown != null ? rulesetDropdown.value.Trim() : string.Empty);
        string selectedRulesetId = PlayerPrefs.GetString(RulesetIdPlayerPrefsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(selectedRulesetId) && !string.Equals(selectedRulesetMode, "Custom", System.StringComparison.OrdinalIgnoreCase))
        {
            NarafinRulesetSummary selectedRuleset = GetSelectedRulesetSummary(selectedRulesetName);
            selectedRulesetId = selectedRuleset != null ? selectedRuleset.ruleset_id : string.Empty;
        }

        if (string.IsNullOrWhiteSpace(selectedRulesetMode))
        {
            selectedRulesetMode = rulesetModeDropdown != null ? rulesetModeDropdown.value : "Mahir";
        }

        if (string.IsNullOrWhiteSpace(selectedRulesetName))
        {
            selectedRulesetName = rulesetDropdown != null ? rulesetDropdown.value.Trim() : string.Empty;
        }

        if (string.IsNullOrWhiteSpace(selectedRulesetId) && string.Equals(selectedRulesetMode, "Custom", System.StringComparison.OrdinalIgnoreCase))
        {
            ShowErrorPopup("Ruleset custom belum dipilih.");
            return;
        }

        Debug.Log("Play bootstrap mode: " + selectedRulesetMode + ", ruleset_id: " + selectedRulesetId + ", ruleset_name: " + selectedRulesetName);

        int playerCount = GetSelectedPlayerCount();
        List<string> playerNames = new List<string>();
        HashSet<string> uniqueNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < playerCount; i++)
        {
            string playerName = playerNameInputs[i].value.Trim();

            if (playerName == string.Empty)
            {
                Debug.Log("Player name is empty.");
                ShowErrorPopup("Nama Player " + (i + 1) + " tidak boleh kosong.");
                return;
            }

            if (!uniqueNames.Add(playerName))
            {
                ShowErrorPopup("Nama player tidak boleh sama.");
                return;
            }

            if (!IsPlayerRegistered(playerName))
            {
                ShowErrorPopup("Nama Player " + (i + 1) + " harus ada di list player.");
                return;
            }

            playerNames.Add(playerName);
        }

        BeginPlayLoading("Menyiapkan session...");
        bool startedSceneTransition = false;
        try
        {
            await System.Threading.Tasks.Task.Yield();

            NarafinSessionOperationResult narasiResult = await ApplySelectedNarasiPackAsync();
            if (!narasiResult.Success)
            {
                ShowErrorPopup(narasiResult.ErrorMessage);
                return;
            }

            NarafinPlaySessionResult result = await LoginManager.Instance.CreateAndStartPlaySessionAsync(sessionName, selectedRulesetMode, selectedRulesetName, selectedRulesetId, playerNames);
            if (!result.Success)
            {
                ShowErrorPopup(result.ErrorMessage);
                return;
            }

            PlayerPrefs.SetInt("PlayerCount", playerCount);

            for (int i = 0; i < playerNameInputs.Length; i++)
            {
                if (i < playerCount)
                {
                    string playerName = playerNameInputs[i].value.Trim();
                    PlayerPrefs.SetString("PlayerName_" + (i + 1), playerName);

                    if (i == 0)
                    {
                        PlayerPrefs.SetString("PlayerName", playerName);
                    }
                }
                else
                {
                    PlayerPrefs.DeleteKey("PlayerName_" + (i + 1));
                }
            }

            PlayerPrefs.Save();
            NarafinSessionScope.BeginNewSession(NarafinSessionScope.CurrentSessionName);
            startedSceneTransition = true;
            ChangeScene.Instance.ChangeToScene(1);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Gagal memulai session: " + ex.Message);
            ShowErrorPopup("Gagal memulai session. Silakan coba lagi.");
        }
        finally
        {
            if (!startedSceneTransition)
            {
                EndPlayLoading();
            }
        }
    }

    private async System.Threading.Tasks.Task SetupNarasiPackDropdownAsync()
    {
        if (narasiPackDropdown == null)
        {
            return;
        }

        string previouslySelected = narasiPackDropdown.value;
        currentNarasiPackOptions.Clear();
        NarasiPackRepository.ClearLastCloudWarning();

        NarasiManifestData manifest = await NarasiPackRepository.LoadManifestAsync();
        if (manifest?.narasiPacks != null)
        {
            foreach (NarasiPackData pack in manifest.narasiPacks)
            {
                if (pack != null && !string.IsNullOrWhiteSpace(pack.id) && !string.IsNullOrWhiteSpace(pack.name))
                {
                    currentNarasiPackOptions.Add(pack);
                }
            }
        }

        List<string> choices = new List<string> { NarasiSessionContext.DefaultOptionName };
        foreach (NarasiPackData pack in currentNarasiPackOptions)
        {
            choices.Add(pack.name);
        }

        narasiPackDropdown.choices = choices;
        string selectedOption = choices.Contains(previouslySelected) ? previouslySelected : NarasiSessionContext.DefaultOptionName;
        narasiPackDropdown.SetValueWithoutNotify(selectedOption);

        if (!string.IsNullOrWhiteSpace(NarasiPackRepository.LastCloudWarningMessage))
        {
            Debug.LogWarning(NarasiPackRepository.LastCloudWarningMessage);
        }
    }

    private NarasiPackData GetSelectedNarasiPack()
    {
        string selectedName = narasiPackDropdown != null ? narasiPackDropdown.value : NarasiSessionContext.DefaultOptionName;
        if (string.IsNullOrWhiteSpace(selectedName) || string.Equals(selectedName, NarasiSessionContext.DefaultOptionName, System.StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        foreach (NarasiPackData pack in currentNarasiPackOptions)
        {
            if (pack != null && string.Equals(pack.name, selectedName, System.StringComparison.Ordinal))
            {
                return pack;
            }
        }

        return null;
    }

    private async System.Threading.Tasks.Task<NarafinSessionOperationResult> ApplySelectedNarasiPackAsync()
    {
        string selectedPackId = PlayerPrefs.GetString(NarasiPackIdPlayerPrefsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(selectedPackId))
        {
            return await NarasiSessionContext.ApplyAsync(null);
        }

        NarasiManifestData manifest = await NarasiPackRepository.LoadManifestAsync();
        NarasiPackData selectedPack = null;
        if (manifest?.narasiPacks != null)
        {
            foreach (NarasiPackData pack in manifest.narasiPacks)
            {
                if (pack != null && string.Equals(pack.id, selectedPackId, System.StringComparison.OrdinalIgnoreCase))
                {
                    selectedPack = pack;
                    break;
                }
            }
        }

        if (selectedPack == null)
        {
            string packName = PlayerPrefs.GetString(NarasiPackNamePlayerPrefsKey, "paket terpilih");
            return new NarafinSessionOperationResult
            {
                Success = false,
                ErrorCode = "NARASI_PACK_NOT_FOUND",
                ErrorMessage = "Paket narasi \"" + packName + "\" tidak ditemukan."
            };
        }

        return await NarasiSessionContext.ApplyAsync(selectedPack);
    }

    private int GetSelectedPlayerCount()
    {
        if (int.TryParse(playerCountDropdown.value, out int playerCount))
        {
            return Mathf.Clamp(playerCount, 3, 4);
        }

        return 3;
    }

    private bool IsPlayerRegistered(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName) || LoginManager.Instance == null || LoginManager.Instance.CachedPlayers == null)
        {
            return false;
        }

        string normalizedPlayerName = playerName.Trim();

        foreach (NarafinPlayerSummary player in LoginManager.Instance.CachedPlayers)
        {
            if (player == null)
            {
                continue;
            }

            string displayName = !string.IsNullOrWhiteSpace(player.display_name) ? player.display_name.Trim() : string.Empty;
            string username = !string.IsNullOrWhiteSpace(player.username) ? player.username.Trim() : string.Empty;

            if (string.Equals(displayName, normalizedPlayerName, System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(username, normalizedPlayerName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdatePlayerNameInputs()
    {
        int playerCount = GetSelectedPlayerCount();

        for (int i = 0; i < playerNameInputs.Length; i++)
        {
            playerNameGroups[i].style.display = i < playerCount ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (activePlayerNameInputIndex >= playerCount)
        {
            HidePlayerSuggestions();
        }
    }

    private void SetupRulesetDropdown()
    {
        if (rulesetModeDropdown != null && (rulesetModeDropdown.choices == null || rulesetModeDropdown.choices.Count == 0))
        {
            rulesetModeDropdown.choices = new List<string>(RulesetModeChoices);
            if (string.IsNullOrWhiteSpace(rulesetModeDropdown.value))
            {
                rulesetModeDropdown.value = "Mahir";
            }
        }

        RefreshRulesetDropdownOptions();
    }

    private NarafinRulesetSummary GetSelectedRulesetSummary(string rulesetName)
    {
        if (LoginManager.Instance == null || LoginManager.Instance.CachedRulesets == null)
        {
            return null;
        }

        string mode = rulesetModeDropdown != null ? rulesetModeDropdown.value : "Custom";
        if (!string.IsNullOrWhiteSpace(mode) && !string.Equals(mode, "Custom", System.StringComparison.OrdinalIgnoreCase))
        {
            return GetDefaultRulesetForMode(mode);
        }

        if (string.IsNullOrWhiteSpace(rulesetName))
        {
            return null;
        }

        foreach (NarafinRulesetSummary ruleset in LoginManager.Instance.CachedRulesets)
        {
            if (ruleset != null && string.Equals(ruleset.name, rulesetName, System.StringComparison.OrdinalIgnoreCase))
            {
                return ruleset;
            }
        }

        return null;
    }

    private void OnRulesetModeChanged(ChangeEvent<string> evt)
    {
        RefreshRulesetDropdownOptions(evt.newValue);
    }

    private void OnRulesetDropdownChanged(ChangeEvent<string> evt)
    {
        if (string.IsNullOrWhiteSpace(evt.newValue))
        {
            sessionSetupValidationText.text = string.Empty;
        }
    }

    private void RegisterPlayerNameCallbacks()
    {
        if (playerNameInputs == null)
        {
            return;
        }

        for (int i = 0; i < playerNameInputs.Length; i++)
        {
            int playerIndex = i;
            TextField playerInput = playerNameInputs[i];
            if (playerInput == null)
            {
                continue;
            }

            playerInput.RegisterValueChangedCallback(evt => OnPlayerNameChanged(playerIndex, evt.newValue));
            playerInput.RegisterCallback<FocusInEvent>(evt => OnPlayerNameFocused(playerIndex));
        }
    }

    private void OnPlayerNameFocused(int playerIndex)
    {
        activePlayerNameInputIndex = playerIndex;
        RefreshPlayerSuggestions(playerIndex, GetPlayerInputValue(playerIndex));
    }

    private void OnPlayerNameChanged(int playerIndex, string newValue)
    {
        activePlayerNameInputIndex = playerIndex;
        RefreshPlayerSuggestions(playerIndex, newValue);
    }

    private void OnPlayerSuggestionClicked(int suggestionIndex)
    {
        if (suggestionIndex < 0 || suggestionIndex >= currentPlayerSuggestions.Count)
        {
            return;
        }

        if (activePlayerNameInputIndex < 0 || activePlayerNameInputIndex >= playerNameInputs.Length)
        {
            return;
        }

        TextField activeInput = playerNameInputs[activePlayerNameInputIndex];
        if (activeInput == null)
        {
            return;
        }

        NarafinPlayerSummary selectedPlayer = currentPlayerSuggestions[suggestionIndex];
        string playerName = GetPlayerSuggestionLabel(selectedPlayer);
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return;
        }

        activeInput.SetValueWithoutNotify(playerName);
        activePlayerNameInputIndex = -1;
        HidePlayerSuggestions();
    }

    private void RefreshPlayerSuggestions(int playerIndex, string query)
    {
        if (playerSuggestionsOverlay == null || playerSuggestionButtons == null || playerSuggestionsPanel == null)
        {
            return;
        }

        if (playerIndex < 0 || playerIndex >= playerNameInputs.Length)
        {
            HidePlayerSuggestions();
            return;
        }

        if (playerCountDropdown != null && int.TryParse(playerCountDropdown.value, out int playerCount) && playerIndex >= playerCount)
        {
            HidePlayerSuggestions();
            return;
        }

        currentPlayerSuggestions.Clear();

        if (LoginManager.Instance == null || LoginManager.Instance.CachedPlayers == null)
        {
            HidePlayerSuggestions();
            return;
        }

        string normalizedQuery = string.IsNullOrWhiteSpace(query) ? string.Empty : query.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            HidePlayerSuggestions();
            return;
        }

        foreach (NarafinPlayerSummary player in LoginManager.Instance.CachedPlayers)
        {
            if (player == null)
            {
                continue;
            }

            string displayLabel = GetPlayerSuggestionLabel(player);
            string usernameLabel = !string.IsNullOrWhiteSpace(player.username) ? player.username.Trim() : string.Empty;

            bool matches = (!string.IsNullOrWhiteSpace(displayLabel) &&
                            displayLabel.IndexOf(normalizedQuery, System.StringComparison.OrdinalIgnoreCase) >= 0)
                           || (!string.IsNullOrWhiteSpace(usernameLabel) &&
                               usernameLabel.IndexOf(normalizedQuery, System.StringComparison.OrdinalIgnoreCase) >= 0);

            if (!matches)
            {
                continue;
            }

            currentPlayerSuggestions.Add(player);

            if (currentPlayerSuggestions.Count >= PlayerSuggestionLimit)
            {
                break;
            }
        }

        bool shouldShow = currentPlayerSuggestions.Count > 0;

        for (int i = 0; i < playerSuggestionButtons.Length; i++)
        {
            Button suggestionButton = playerSuggestionButtons[i];
            if (suggestionButton == null)
            {
                continue;
            }

            if (i < currentPlayerSuggestions.Count)
            {
                suggestionButton.text = GetPlayerSuggestionLabel(currentPlayerSuggestions[i]);
                suggestionButton.style.display = DisplayStyle.Flex;
            }
            else
            {
                suggestionButton.text = string.Empty;
                suggestionButton.style.display = DisplayStyle.None;
            }
        }

        if (!shouldShow)
        {
            HidePlayerSuggestions();
            return;
        }

        PositionPlayerSuggestionsPanel(playerIndex);
        playerSuggestionsOverlay.style.display = DisplayStyle.Flex;
        playerSuggestionsPanel.style.display = DisplayStyle.Flex;
    }

    private void PositionPlayerSuggestionsPanel(int playerIndex)
    {
        if (playerNameInputs == null || playerIndex < 0 || playerIndex >= playerNameInputs.Length)
        {
            return;
        }

        TextField activeInput = playerNameInputs[playerIndex];
        if (activeInput == null || playerSuggestionsPanel == null || playerSuggestionsOverlay == null)
        {
            return;
        }

        Rect fieldWorldBound = activeInput.worldBound;
        if (fieldWorldBound.width <= 0f || fieldWorldBound.height <= 0f)
        {
            return;
        }

        Vector2 panelAnchor = playerSuggestionsOverlay.WorldToLocal(new Vector2(fieldWorldBound.xMin, fieldWorldBound.yMax + 6f));
        float panelWidth = Mathf.Max(fieldWorldBound.width, 280f);
        float overlayWidth = playerSuggestionsOverlay.resolvedStyle.width;
        float overlayHeight = playerSuggestionsOverlay.resolvedStyle.height;

        if (overlayWidth > 0f)
        {
            panelWidth = Mathf.Min(panelWidth, overlayWidth - 16f);
        }

        float panelHeight = 316f;

        if (overlayWidth > 0f && panelAnchor.x + panelWidth > overlayWidth - 8f)
        {
            panelAnchor.x = Mathf.Max(8f, overlayWidth - panelWidth - 8f);
        }

        if (overlayHeight > 0f && panelAnchor.y + panelHeight > overlayHeight - 8f)
        {
            panelAnchor.y = Mathf.Max(8f, fieldWorldBound.yMin - panelHeight - 6f);
        }

        playerSuggestionsPanel.style.left = panelAnchor.x;
        playerSuggestionsPanel.style.top = panelAnchor.y;
        playerSuggestionsPanel.style.width = panelWidth;
    }

    private string GetPlayerSuggestionLabel(NarafinPlayerSummary player)
    {
        if (player == null)
        {
            return string.Empty;
        }

        string displayName = !string.IsNullOrWhiteSpace(player.display_name) ? player.display_name.Trim() : string.Empty;
        string username = !string.IsNullOrWhiteSpace(player.username) ? player.username.Trim() : string.Empty;

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        return username;
    }

    private string GetPlayerInputValue(int playerIndex)
    {
        if (playerNameInputs == null || playerIndex < 0 || playerIndex >= playerNameInputs.Length || playerNameInputs[playerIndex] == null)
        {
            return string.Empty;
        }

        return playerNameInputs[playerIndex].value;
    }

    private void HidePlayerSuggestions()
    {
        currentPlayerSuggestions.Clear();

        if (playerSuggestionsOverlay != null)
        {
            playerSuggestionsOverlay.style.display = DisplayStyle.None;
        }

        if (playerSuggestionsPanel != null)
        {
            playerSuggestionsPanel.style.display = DisplayStyle.None;
        }

        if (playerSuggestionButtons == null)
        {
            return;
        }

        foreach (Button suggestionButton in playerSuggestionButtons)
        {
            if (suggestionButton != null)
            {
                suggestionButton.text = string.Empty;
                suggestionButton.style.display = DisplayStyle.None;
            }
        }
    }

    private void RefreshRulesetDropdownOptions(string selectedMode = null)
    {
        if (rulesetDropdown == null)
        {
            return;
        }

        currentRulesetOptions.Clear();

        if (LoginManager.Instance == null || LoginManager.Instance.CachedRulesets == null)
        {
            rulesetDropdown.choices = new List<string>();
            rulesetDropdown.SetValueWithoutNotify(string.Empty);
            rulesetDropdown.SetEnabled(false);
            rulesetDropdown.RemoveFromClassList("ruleset-dropdown-locked");
            return;
        }

        string mode = !string.IsNullOrWhiteSpace(selectedMode)
            ? selectedMode.Trim()
            : (rulesetModeDropdown != null ? rulesetModeDropdown.value : "Custom");

        if (string.Equals(mode, "Custom", System.StringComparison.OrdinalIgnoreCase))
        {
            List<NarafinRulesetSummary> filteredRulesets = new List<NarafinRulesetSummary>();
            foreach (NarafinRulesetSummary ruleset in LoginManager.Instance.CachedRulesets)
            {
                if (ruleset == null || string.IsNullOrWhiteSpace(ruleset.name))
                {
                    continue;
                }

                filteredRulesets.Add(ruleset);
            }

            currentRulesetOptions.AddRange(filteredRulesets);

            List<string> choices = new List<string>();
            foreach (NarafinRulesetSummary ruleset in currentRulesetOptions)
            {
                choices.Add(ruleset.name);
            }

            rulesetDropdown.choices = choices;
            rulesetDropdown.SetEnabled(true);
            rulesetDropdown.RemoveFromClassList("ruleset-dropdown-locked");

            if (choices.Count == 0)
            {
                rulesetDropdown.SetValueWithoutNotify(string.Empty);
                return;
            }

            string currentValue = rulesetDropdown.value;
            if (string.IsNullOrWhiteSpace(currentValue) || !choices.Contains(currentValue))
            {
                rulesetDropdown.SetValueWithoutNotify(choices[0]);
            }

            return;
        }

        NarafinRulesetSummary defaultRuleset = GetDefaultRulesetForMode(mode);
        currentRulesetOptions.Clear();
        if (defaultRuleset != null)
        {
            currentRulesetOptions.Add(defaultRuleset);
        }

        rulesetDropdown.choices = new List<string> { "Default" };
        rulesetDropdown.SetValueWithoutNotify("Default");
        rulesetDropdown.SetEnabled(false);
        rulesetDropdown.AddToClassList("ruleset-dropdown-locked");
    }

    private NarafinRulesetSummary GetDefaultRulesetForMode(string mode)
    {
        if (LoginManager.Instance == null || LoginManager.Instance.CachedRulesets == null)
        {
            return null;
        }

        NarafinRulesetSummary fallbackDefault = null;
        foreach (NarafinRulesetSummary ruleset in LoginManager.Instance.CachedRulesets)
        {
            if (ruleset == null || string.IsNullOrWhiteSpace(ruleset.name))
            {
                continue;
            }

            if (ruleset.is_default && fallbackDefault == null)
            {
                fallbackDefault = ruleset;
            }

            if (ruleset.is_default &&
                !string.IsNullOrWhiteSpace(ruleset.name) &&
                ruleset.name.IndexOf(mode, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ruleset;
            }
        }

        return fallbackDefault;
    }

    private void ShowSessionSetup()
    {
        sessionSetupValidationText.text = string.Empty;
        playContainer.RemoveFromClassList("show-play");
        sessionSetupContainer.RemoveFromClassList("hide-session-setup-left");
        sessionSetupContainer.AddToClassList("show-session-setup");
        HidePlayerSuggestions();
        RefreshRulesetDropdownOptions();
    }

    private void BeginPlayLoading(string message)
    {
        isPlayLoading = true;
        SetHomeButtonsEnabled(false);

        if (playLoadingOverlay != null)
        {
            playLoadingOverlay.style.display = DisplayStyle.Flex;
            playLoadingOverlay.BringToFront();
        }

        if (playLoadingText != null && !string.IsNullOrWhiteSpace(message))
        {
            playLoadingText.text = message;
        }

        if (playLoadingSpinner != null)
        {
            playLoadingSpinner.style.rotate = new Rotate(new Angle(0f, AngleUnit.Degree));
            if (playLoadingSpinnerCoroutine == null)
            {
                playLoadingSpinnerCoroutine = StartCoroutine(AnimatePlayLoadingSpinner());
            }
        }
    }

    private void EndPlayLoading()
    {
        isPlayLoading = false;
        SetHomeButtonsEnabled(true);

        if (playLoadingOverlay != null)
        {
            playLoadingOverlay.style.display = DisplayStyle.None;
        }

        if (playLoadingSpinnerCoroutine != null)
        {
            StopCoroutine(playLoadingSpinnerCoroutine);
            playLoadingSpinnerCoroutine = null;
        }
    }

    private void ResetPlayLoadingState()
    {
        isPlayLoading = false;

        if (playLoadingOverlay != null)
        {
            playLoadingOverlay.style.display = DisplayStyle.None;
        }

        if (playLoadingSpinnerCoroutine != null)
        {
            StopCoroutine(playLoadingSpinnerCoroutine);
            playLoadingSpinnerCoroutine = null;
        }
    }

    private IEnumerator AnimatePlayLoadingSpinner()
    {
        float angle = 0f;

        while (true)
        {
            if (playLoadingSpinner != null)
            {
                angle += Time.deltaTime * 360f;
                playLoadingSpinner.style.rotate = new Rotate(new Angle(angle, AngleUnit.Degree));
            }

            yield return null;
        }
    }

    private void SetHomeButtonsEnabled(bool enabled)
    {
        addPlayerButton?.SetEnabled(enabled);
        playButton?.SetEnabled(enabled);
        editButton?.SetEnabled(enabled);
        exitButton?.SetEnabled(enabled);
        signOutButton?.SetEnabled(enabled);
    }
}
