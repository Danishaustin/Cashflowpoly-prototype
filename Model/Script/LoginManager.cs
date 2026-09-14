using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;

public class LoginManager : MonoBehaviour
{
    public static LoginManager Instance;

    private const string InstructorRole = "INSTRUCTOR";
    private const string RoleNotAllowedErrorCode = "AUTH_ROLE_NOT_ALLOWED";
    private const string PemulaDefaultRulesetName = "Ruleset PEMULA Default";
    private const string MahirDefaultRulesetName = "Ruleset MAHIR Default";
    private const string PemulaDefaultRulesetResource = "Data/rulesetPemula";
    private const string MahirDefaultRulesetResource = "Data/rulesetMahir";

    private NarafinApiClient apiClient;
    private NarafinUgsAuthBridge ugsAuthBridge;
    private string accessToken;
    private string userId;
    private string username;
    private string displayName;
    private string role;
    private DateTimeOffset? expiresAtUtc;
    private string lastAuthErrorCode = string.Empty;
    private string lastAuthErrorMessage = string.Empty;
    private List<NarafinRulesetSummary> cachedRulesets = new List<NarafinRulesetSummary>();
    private List<NarafinPlayerSummary> cachedPlayers = new List<NarafinPlayerSummary>();

    public string AccessToken
    {
        get
        {
            return IsSignedIn() ? accessToken : string.Empty;
        }
    }

    public string CurrentUserId => userId;
    public string CurrentUsername => username;
    public string CurrentDisplayName => displayName;
    public string CurrentRole => role;
    public string CurrentApiSessionId => NarafinPlayerPrefs.ApiSessionId;
    public string CurrentApiRulesetVersionId => NarafinPlayerPrefs.ApiRulesetVersionId;
    public string CurrentUgsPlayerId => GetOrCreateUgsAuthBridge().PlayerId;
    public DateTimeOffset? ExpiresAtUtc => expiresAtUtc;
    public string LastAuthErrorCode => lastAuthErrorCode;
    public string LastAuthErrorMessage => lastAuthErrorMessage;
    public IReadOnlyList<NarafinRulesetSummary> CachedRulesets => cachedRulesets;
    public IReadOnlyList<NarafinPlayerSummary> CachedPlayers => cachedPlayers;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        apiClient = new NarafinApiClient();
        ugsAuthBridge = new NarafinUgsAuthBridge();
        LoadPersistedSession();
    }

    private void OnApplicationQuit()
    {
        SignOut();
    }

    public async Task InitializeServicesAsync()
    {
        LoadPersistedSession();
        await GetOrCreateUgsAuthBridge().InitializeAsync();
    }

    public async Task<bool> SignUp(string username, string password)
    {
        ClearLastAuthError();
        if (!ValidateCredentials(username, password))
        {
            SetLastAuthError("VALIDATION_ERROR", "Username dan password harus diisi.");
            return false;
        }

        NarafinAuthResult result = await apiClient.RegisterAsync(username, password);
        if (!result.Success)
        {
            SetLastAuthError(result.ErrorCode, result.ErrorMessage);
            Debug.LogWarning("Narafin register gagal: " + result.ErrorCode + " - " + result.ErrorMessage);
            return false;
        }

        ApplySession(result.Session);
        if (!await SyncUnityGameServicesAuthAsync(result.Session))
        {
            return false;
        }

        ClearLastAuthError();
        Debug.Log("Register berhasil. User ID: " + userId);
        return true;
    }

    public async Task<NarafinPlayBootstrapResult> LoadPlayBootstrapDataAsync()
    {
        if (!IsSignedIn())
        {
            return CreatePlayBootstrapFailure("AUTH_SESSION_MISSING", "Sesi login tidak tersedia. Silakan login ulang.");
        }

        try
        {
            ClearPlayBootstrapCache();

            Task<NarafinRulesetListResult> rulesetsTask = apiClient.GetRulesetsAsync(AccessToken);
            Task<NarafinPlayerListResult> playersTask = apiClient.GetPlayersAsync(AccessToken);

            await Task.WhenAll(rulesetsTask, playersTask);

            NarafinRulesetListResult rulesetsResult = rulesetsTask.Result;
            NarafinPlayerListResult playersResult = playersTask.Result;

            if (!rulesetsResult.Success)
            {
                return CreatePlayBootstrapFailure(rulesetsResult.ErrorCode, rulesetsResult.ErrorMessage);
            }

            if (!playersResult.Success)
            {
                return CreatePlayBootstrapFailure(playersResult.ErrorCode, playersResult.ErrorMessage);
            }

            cachedRulesets = rulesetsResult.Items ?? new List<NarafinRulesetSummary>();
            cachedPlayers = playersResult.Items ?? new List<NarafinPlayerSummary>();

            if (cachedRulesets.Count == 0)
            {
                return CreatePlayBootstrapFailure("NO_RULESETS_AVAILABLE", "Tidak ada ruleset aktif yang bisa dimuat.");
            }

            Debug.Log("Play bootstrap loaded. Rulesets: " + cachedRulesets.Count + ", Players: " + cachedPlayers.Count);

            return new NarafinPlayBootstrapResult
            {
                Success = true,
                Rulesets = cachedRulesets,
                Players = cachedPlayers
            };
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal memuat data play bootstrap: " + ex.Message);
            return CreatePlayBootstrapFailure("PLAY_BOOTSTRAP_FAILED", "Gagal memuat data ruleset dan player.");
        }
    }

    public async Task<NarafinPlaySessionResult> CreateAndStartPlaySessionAsync(string sessionName, string rulesetMode, string selectedRulesetName, string selectedRulesetId, IReadOnlyList<string> playerNames)
    {
        if (!IsSignedIn())
        {
            return CreatePlaySessionFailure("AUTH_SESSION_MISSING", "Sesi login tidak tersedia. Silakan login ulang.");
        }

        if (!string.Equals(CurrentRole, InstructorRole, StringComparison.OrdinalIgnoreCase))
        {
            return CreatePlaySessionFailure(RoleNotAllowedErrorCode, "Akun ini bukan instructor.");
        }

        if (string.IsNullOrWhiteSpace(sessionName))
        {
            return CreatePlaySessionFailure("VALIDATION_ERROR", "Nama session tidak boleh kosong.");
        }

        if (playerNames == null || playerNames.Count == 0)
        {
            return CreatePlaySessionFailure("VALIDATION_ERROR", "Minimal satu player harus diisi.");
        }

        List<string> cleanPlayerNames = new List<string>();
        HashSet<string> seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < playerNames.Count; i++)
        {
            string cleanName = playerNames[i] != null ? playerNames[i].Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(cleanName))
            {
                return CreatePlaySessionFailure("VALIDATION_ERROR", "Nama Player " + (i + 1) + " tidak boleh kosong.");
            }

            if (!seenNames.Add(cleanName))
            {
                return CreatePlaySessionFailure("VALIDATION_ERROR", "Nama player tidak boleh sama.");
            }

            cleanPlayerNames.Add(cleanName);
        }

        List<NarafinPlayerSummary> selectedPlayers = new List<NarafinPlayerSummary>();
        for (int i = 0; i < cleanPlayerNames.Count; i++)
        {
            NarafinPlayerSummary matchedPlayer = ResolveCachedPlayer(cleanPlayerNames[i]);
            if (matchedPlayer == null)
            {
                return CreatePlaySessionFailure("PLAYER_NOT_FOUND", "Player \"" + cleanPlayerNames[i] + "\" tidak ditemukan di daftar player.");
            }

            selectedPlayers.Add(matchedPlayer);
        }

        ResolvedPlayRuleset resolvedRuleset = await ResolvePlayRulesetAsync(rulesetMode, selectedRulesetName, selectedRulesetId);
        if (!resolvedRuleset.Success)
        {
            return CreatePlaySessionFailure(resolvedRuleset.ErrorCode, resolvedRuleset.ErrorMessage);
        }

        Debug.Log("Ruleset resolved. mode: " + resolvedRuleset.Mode + ", ruleset_id: " + resolvedRuleset.RulesetId + ", ruleset_version_id: " + resolvedRuleset.RulesetVersionId + ", version: " + resolvedRuleset.Version);

        NarafinSessionCreateResult createResult = await apiClient.CreateSessionAsync(AccessToken, sessionName.Trim(), resolvedRuleset.Mode, resolvedRuleset.RulesetVersionId);
        if (!createResult.Success)
        {
            return CreatePlaySessionFailure(createResult.ErrorCode, createResult.ErrorMessage);
        }

        for (int i = 0; i < selectedPlayers.Count; i++)
        {
            NarafinPlayerSummary selectedPlayer = selectedPlayers[i];
            string selectedUserId = !string.IsNullOrWhiteSpace(selectedPlayer.user_id) ? selectedPlayer.user_id : string.Empty;
            NarafinSessionOperationResult addResult = await apiClient.AddPlayerToSessionAsync(
                AccessToken,
                createResult.SessionId,
                selectedUserId,
                i + 1);

            if (!addResult.Success)
            {
                return CreatePlaySessionFailure(addResult.ErrorCode, addResult.ErrorMessage);
            }
        }

        if (!NarafinRuntimeConfig.UseOfflineMode)
        {
            NarafinSessionOperationResult setupResult = await ValidateAndSaveInitialSessionSetupAsync(
                createResult.SessionId,
                resolvedRuleset,
                selectedPlayers);

            if (!setupResult.Success)
            {
                return CreatePlaySessionFailure(setupResult.ErrorCode, setupResult.ErrorMessage);
            }
        }

        NarafinSessionOperationResult startResult = await apiClient.StartSessionAsync(AccessToken, createResult.SessionId);
        if (!startResult.Success)
        {
            return CreatePlaySessionFailure(startResult.ErrorCode, startResult.ErrorMessage);
        }

        NarafinPlayerPrefs.StorePlaySession(
            createResult.SessionId,
            resolvedRuleset.RulesetVersionId ?? createResult.RulesetVersionId,
            selectedPlayers);

        NarafinSessionEventSequenceResult sequenceResult = await apiClient.GetLastSessionEventSequenceAsync(AccessToken, createResult.SessionId);
        if (!sequenceResult.Success)
        {
            return CreatePlaySessionFailure(sequenceResult.ErrorCode, "Session berhasil dimulai, tetapi gagal menyinkronkan urutan event: " + sequenceResult.ErrorMessage);
        }

        NarafinPlayerPrefs.SetLastEventSequenceNumber(sequenceResult.LastSequenceNumber);
        Debug.Log("Narafin event sequence disinkronkan. Sequence terakhir: " + sequenceResult.LastSequenceNumber);

        return new NarafinPlaySessionResult
        {
            Success = true,
            SessionId = createResult.SessionId,
            RulesetId = resolvedRuleset.RulesetId,
            RulesetVersionId = resolvedRuleset.RulesetVersionId
        };
    }

    private async Task<NarafinSessionOperationResult> ValidateAndSaveInitialSessionSetupAsync(
        string sessionId,
        ResolvedPlayRuleset resolvedRuleset,
        IReadOnlyList<NarafinPlayerSummary> selectedPlayers)
    {
        NarafinSessionStateResult stateResult = await apiClient.GetSessionStateAsync(AccessToken, sessionId);
        if (!stateResult.Success)
        {
            return CreateSessionOperationFailure(stateResult.ErrorCode, stateResult.ErrorMessage);
        }

        NarafinRulesetSetupCatalogResult catalogResult = await apiClient.GetRulesetSetupCatalogAsync(
            AccessToken,
            resolvedRuleset.RulesetId,
            resolvedRuleset.Version);
        if (!catalogResult.Success)
        {
            return CreateSessionOperationFailure(catalogResult.ErrorCode, catalogResult.ErrorMessage);
        }

        if (!TryBuildInitialSetupPayload(sessionId, resolvedRuleset.Mode, stateResult.Players, selectedPlayers, catalogResult.Definition, out string setupJson, out string errorMessage))
        {
            return CreateSessionOperationFailure("SETUP_VALIDATION_ERROR", errorMessage);
        }

        NarafinSessionOperationResult validationResult = await apiClient.ValidateSessionSetupAsync(AccessToken, sessionId, setupJson);
        if (!validationResult.Success)
        {
            return validationResult;
        }

        return await apiClient.SaveSessionSetupAsync(AccessToken, sessionId, setupJson);
    }

    private static bool TryBuildInitialSetupPayload(
        string sessionId,
        string rulesetMode,
        IReadOnlyList<NarafinSessionStatePlayer> sessionPlayers,
        IReadOnlyList<NarafinPlayerSummary> selectedPlayers,
        NarafinRulesetSetupDefinition definition,
        out string setupJson,
        out string errorMessage)
    {
        setupJson = string.Empty;
        errorMessage = string.Empty;

        if (definition == null || definition.tie_breakers == null || definition.ingredients == null || definition.collection_missions == null)
        {
            errorMessage = "Komponen ruleset untuk pembagian awal tidak lengkap.";
            return false;
        }

        if (definition.tie_breakers.Count < selectedPlayers.Count ||
            definition.ingredients.Count == 0 ||
            definition.collection_missions.Count < selectedPlayers.Count)
        {
            errorMessage = "Ruleset tidak memiliki Tie Breaker, bahan, atau misi awal yang cukup untuk seluruh player.";
            return false;
        }

        bool isMahir = string.Equals(rulesetMode, "MAHIR", StringComparison.OrdinalIgnoreCase);
        string loanCode = GetFirstSetupLoanCode(definition);
        string insuranceProductCode = GetFirstSetupInsuranceProductCode(definition);
        if (isMahir && (string.IsNullOrWhiteSpace(loanCode) || string.IsNullOrWhiteSpace(insuranceProductCode)))
        {
            errorMessage = "Ruleset Mahir harus memiliki kode pinjaman dan produk asuransi untuk pembagian awal.";
            return false;
        }

        List<NarafinSessionSetupPlayer> setupPlayers = new List<NarafinSessionSetupPlayer>();
        for (int i = 0; i < selectedPlayers.Count; i++)
        {
            NarafinPlayerSummary selectedPlayer = selectedPlayers[i];
            NarafinSessionStatePlayer sessionPlayer = FindSessionPlayer(sessionPlayers, selectedPlayer.user_id);
            if (sessionPlayer == null || string.IsNullOrWhiteSpace(sessionPlayer.session_player_id))
            {
                errorMessage = "session_player_id untuk player \"" + selectedPlayer.display_name + "\" tidak ditemukan.";
                return false;
            }

            NarafinSetupTieBreaker tieBreaker = definition.tie_breakers[i];
            NarafinSetupIngredient ingredient = definition.ingredients[i % definition.ingredients.Count];
            NarafinSetupMission mission = definition.collection_missions[i];
            if (tieBreaker == null || ingredient == null || mission == null ||
                string.IsNullOrWhiteSpace(tieBreaker.tie_breaker_code) ||
                string.IsNullOrWhiteSpace(ingredient.id) ||
                string.IsNullOrWhiteSpace(mission.id))
            {
                errorMessage = "Komponen ruleset untuk pembagian awal memiliki kode yang kosong.";
                return false;
            }

            setupPlayers.Add(new NarafinSessionSetupPlayer
            {
                session_player_id = sessionPlayer.session_player_id,
                tie_breaker_code = tieBreaker.tie_breaker_code,
                ingredient_card_id = ingredient.id,
                gold_quantity = 1,
                mission_id = mission.id,
                loan_code = isMahir ? loanCode : null,
                insurance_product_code = isMahir ? insuranceProductCode : null
            });
        }

        setupJson = BuildInitialSetupJson("unity-setup-" + sessionId + "-" + Guid.NewGuid().ToString("N"), setupPlayers);
        Debug.Log("Narafin initial setup payload: " + setupJson);
        return true;
    }

    private static string GetFirstSetupLoanCode(NarafinRulesetSetupDefinition definition)
    {
        if (definition?.sharia_loans == null)
        {
            return null;
        }

        foreach (NarafinSetupLoan loan in definition.sharia_loans)
        {
            if (loan != null && !string.IsNullOrWhiteSpace(loan.loan_code))
            {
                return loan.loan_code;
            }
        }

        return null;
    }

    private static string GetFirstSetupInsuranceProductCode(NarafinRulesetSetupDefinition definition)
    {
        if (definition?.insurance_products == null)
        {
            return null;
        }

        foreach (NarafinSetupInsurance insurance in definition.insurance_products)
        {
            if (insurance != null && !string.IsNullOrWhiteSpace(insurance.product_code))
            {
                return insurance.product_code;
            }
        }

        return null;
    }

    private static string BuildInitialSetupJson(string clientRequestId, IReadOnlyList<NarafinSessionSetupPlayer> players)
    {
        System.Text.StringBuilder json = new System.Text.StringBuilder();
        json.Append("{\"client_request_id\":").Append(JsonString(clientRequestId)).Append(",\"players\":[");

        for (int i = 0; i < players.Count; i++)
        {
            NarafinSessionSetupPlayer player = players[i];
            if (i > 0)
            {
                json.Append(',');
            }

            json.Append("{\"session_player_id\":").Append(JsonString(player.session_player_id));
            json.Append(",\"tie_breaker_code\":").Append(JsonString(player.tie_breaker_code));
            json.Append(",\"ingredient_card_id\":").Append(JsonString(player.ingredient_card_id));
            json.Append(",\"gold_quantity\":").Append(player.gold_quantity);
            json.Append(",\"mission_id\":").Append(JsonString(player.mission_id));
            json.Append(",\"loan_code\":").Append(JsonNullableString(player.loan_code));
            json.Append(",\"insurance_product_code\":").Append(JsonNullableString(player.insurance_product_code));
            json.Append('}');
        }

        json.Append("]}");
        return json.ToString();
    }

    private static string JsonNullableString(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "null" : JsonString(value);
    }

    private static string JsonString(string value)
    {
        return "\"" + (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t") + "\"";
    }

    private static NarafinSessionStatePlayer FindSessionPlayer(IReadOnlyList<NarafinSessionStatePlayer> sessionPlayers, string selectedUserId)
    {
        if (sessionPlayers == null || string.IsNullOrWhiteSpace(selectedUserId))
        {
            return null;
        }

        for (int i = 0; i < sessionPlayers.Count; i++)
        {
            NarafinSessionStatePlayer player = sessionPlayers[i];
            if (player != null && string.Equals(player.user_id, selectedUserId, StringComparison.OrdinalIgnoreCase))
            {
                return player;
            }
        }

        return null;
    }

    private static NarafinSessionOperationResult CreateSessionOperationFailure(string errorCode, string errorMessage)
    {
        return new NarafinSessionOperationResult
        {
            Success = false,
            ErrorCode = errorCode ?? string.Empty,
            ErrorMessage = errorMessage ?? string.Empty
        };
    }

    public async Task<bool> RegisterPlayer(string username, string password)
    {
        ClearLastAuthError();
        if (!ValidateCredentials(username, password))
        {
            SetLastAuthError("VALIDATION_ERROR", "Username dan password harus diisi.");
            return false;
        }

        NarafinAuthResult result = await apiClient.RegisterAsync(username, password, "PLAYER");
        if (!result.Success)
        {
            SetLastAuthError(result.ErrorCode, result.ErrorMessage);
            Debug.LogWarning("Narafin register player gagal: " + result.ErrorCode + " - " + result.ErrorMessage);
            return false;
        }

        ClearLastAuthError();
        Debug.Log("Register player berhasil. User ID: " + result.Session.user_id);
        return true;
    }

    public Task<NarafinSessionOperationResult> PostEventAsync(string rawJsonBody)
    {
        if (!IsSignedIn())
        {
            return Task.FromResult(new NarafinSessionOperationResult
            {
                Success = false,
                ErrorCode = "AUTH_SESSION_MISSING",
                ErrorMessage = "Sesi login tidak tersedia."
            });
        }

        return apiClient.PostEventAsync(AccessToken, rawJsonBody);
    }

    public async Task<bool> SignIn(string username, string password)
    {
        ClearLastAuthError();
        if (!ValidateCredentials(username, password))
        {
            SetLastAuthError("VALIDATION_ERROR", "Username dan password harus diisi.");
            return false;
        }

        NarafinAuthResult result = await apiClient.LoginAsync(username, password);
        if (!result.Success)
        {
            SetLastAuthError(result.ErrorCode, result.ErrorMessage);
            Debug.LogWarning("Narafin login gagal: " + result.ErrorCode + " - " + result.ErrorMessage);
            return false;
        }

        if (result.Session == null || !string.Equals(result.Session.role, InstructorRole, StringComparison.OrdinalIgnoreCase))
        {
            SetLastAuthError(RoleNotAllowedErrorCode, "Akun ini bukan instructor. Silakan login menggunakan akun instructor.");
            Debug.LogWarning("Narafin login ditolak karena role bukan instructor.");
            return false;
        }

        Debug.Log("LoginManager SignIn checkpoint UGS v2. UseUGS: " + NarafinRuntimeConfig.UseUnityGameServicesAuth);
        ApplySession(result.Session);
        if (!await SyncUnityGameServicesAuthAsync(result.Session))
        {
            return false;
        }

        ClearLastAuthError();
        Debug.Log("Login berhasil. User ID: " + userId + ", Unity PlayerId: " + CurrentUgsPlayerId);
        return true;
    }

    public void SignOut()
    {
        ClearSession();
        ClearLastAuthError();
        ClearPlayBootstrapCache();
        Debug.Log("Player signed out.");
    }

    public bool IsSignedIn()
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        if (expiresAtUtc.HasValue && expiresAtUtc.Value <= DateTimeOffset.UtcNow)
        {
            Debug.Log("Narafin session expired.");
            ClearSession();
            return false;
        }

        return true;
    }

    private static bool ValidateCredentials(string usernameValue, string passwordValue)
    {
        if (string.IsNullOrWhiteSpace(usernameValue))
        {
            Debug.Log("Username is empty.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(passwordValue))
        {
            Debug.Log("Password is empty.");
            return false;
        }

        return true;
    }

    private void ApplySession(NarafinAuthSession session)
    {
        if (session == null)
        {
            Debug.LogWarning("Session Narafin tidak valid.");
            ClearSession();
            return;
        }

        accessToken = session.access_token;
        userId = session.user_id;
        username = session.username;
        displayName = string.IsNullOrWhiteSpace(session.display_name) ? session.username : session.display_name;
        role = session.role;
        ClearPlayBootstrapCache();

        if (DateTimeOffset.TryParse(session.expires_at, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset parsedExpiresAt))
        {
            expiresAtUtc = parsedExpiresAt;
        }
        else
        {
            expiresAtUtc = null;
        }

        PersistSession();
    }

    private void LoadPersistedSession()
    {
        accessToken = PlayerPrefs.GetString(NarafinPlayerPrefs.AccessTokenKey, string.Empty);
        userId = PlayerPrefs.GetString(NarafinPlayerPrefs.UserIdKey, string.Empty);
        username = PlayerPrefs.GetString(NarafinPlayerPrefs.UsernameKey, string.Empty);
        displayName = PlayerPrefs.GetString(NarafinPlayerPrefs.DisplayNameKey, string.Empty);
        role = PlayerPrefs.GetString(NarafinPlayerPrefs.RoleKey, string.Empty);

        string expiresAtText = PlayerPrefs.GetString(NarafinPlayerPrefs.ExpiresAtKey, string.Empty);
        if (DateTimeOffset.TryParse(expiresAtText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset parsedExpiresAt))
        {
            expiresAtUtc = parsedExpiresAt;
        }
        else
        {
            expiresAtUtc = null;
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            ClearCachedFields();
            return;
        }

        if (expiresAtUtc.HasValue && expiresAtUtc.Value <= DateTimeOffset.UtcNow)
        {
            Debug.Log("Discarding expired persisted Narafin session.");
            ClearSession();
        }
    }

    private void PersistSession()
    {
        PlayerPrefs.SetString(NarafinPlayerPrefs.AccessTokenKey, accessToken ?? string.Empty);
        PlayerPrefs.SetString(NarafinPlayerPrefs.UserIdKey, userId ?? string.Empty);
        PlayerPrefs.SetString(NarafinPlayerPrefs.UsernameKey, username ?? string.Empty);
        PlayerPrefs.SetString(NarafinPlayerPrefs.DisplayNameKey, displayName ?? string.Empty);
        PlayerPrefs.SetString(NarafinPlayerPrefs.RoleKey, role ?? string.Empty);
        PlayerPrefs.SetString(NarafinPlayerPrefs.ExpiresAtKey, expiresAtUtc.HasValue ? expiresAtUtc.Value.ToString("o") : string.Empty);
        PlayerPrefs.Save();
    }

    private void ClearSession()
    {
        GetOrCreateUgsAuthBridge().SignOut();
        ClearCachedFields();

        NarafinPlayerPrefs.ClearAuthSession();
    }

    private async Task<bool> SyncUnityGameServicesAuthAsync(NarafinAuthSession session)
    {
        Debug.Log("Sync UGS auth dimulai. UseUGS: " + NarafinRuntimeConfig.UseUnityGameServicesAuth +
                  ", RequireUGS: " + NarafinRuntimeConfig.RequireUnityGameServicesAuth +
                  ", Narafin user_id: " + (session != null ? session.user_id : "<null>"));

        if (!NarafinRuntimeConfig.UseUnityGameServicesAuth)
        {
            Debug.Log("Sync UGS auth dilewati karena UseUnityGameServicesAuth = false.");
            return true;
        }

        NarafinUgsAuthBridge bridge = GetOrCreateUgsAuthBridge();
        NarafinSessionOperationResult result = await bridge.EnsureSignedInAsync(session);
        if (result.Success)
        {
            Debug.Log("Sync UGS auth selesai. Unity PlayerId: " + bridge.PlayerId);
            return true;
        }

        if (!NarafinRuntimeConfig.RequireUnityGameServicesAuth)
        {
            Debug.LogWarning("Narafin auth tetap dilanjutkan karena UGS auth tidak diwajibkan. Error: " + result.ErrorMessage);
            return true;
        }

        SetLastAuthError(result.ErrorCode, result.ErrorMessage);
        ClearSession();
        return false;
    }

    private NarafinUgsAuthBridge GetOrCreateUgsAuthBridge()
    {
        if (ugsAuthBridge == null)
        {
            ugsAuthBridge = new NarafinUgsAuthBridge();
            Debug.Log("UGS auth bridge dibuat ulang.");
        }

        return ugsAuthBridge;
    }

    private void ClearPlayBootstrapCache()
    {
        cachedRulesets = new List<NarafinRulesetSummary>();
        cachedPlayers = new List<NarafinPlayerSummary>();
    }

    private void ClearCachedFields()
    {
        accessToken = string.Empty;
        userId = string.Empty;
        username = string.Empty;
        displayName = string.Empty;
        role = string.Empty;
        expiresAtUtc = null;
    }

    private NarafinPlayerSummary ResolveCachedPlayer(string inputName)
    {
        if (string.IsNullOrWhiteSpace(inputName) || cachedPlayers == null)
        {
            return null;
        }

        for (int i = 0; i < cachedPlayers.Count; i++)
        {
            NarafinPlayerSummary player = cachedPlayers[i];
            if (player == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(player.username) &&
                string.Equals(player.username.Trim(), inputName, StringComparison.OrdinalIgnoreCase))
            {
                return player;
            }

            if (!string.IsNullOrWhiteSpace(player.display_name) &&
                string.Equals(player.display_name.Trim(), inputName, StringComparison.OrdinalIgnoreCase))
            {
                return player;
            }
        }

        return null;
    }

    private async Task<ResolvedPlayRuleset> ResolvePlayRulesetAsync(string rulesetMode, string selectedRulesetName, string selectedRulesetId)
    {
        string normalizedMode = NormalizeRulesetMode(rulesetMode);

        if (string.Equals(normalizedMode, "CUSTOM", StringComparison.OrdinalIgnoreCase))
        {
            return await ResolveCustomRulesetAsync(selectedRulesetName, selectedRulesetId);
        }

        if (string.Equals(normalizedMode, "PEMULA", StringComparison.OrdinalIgnoreCase))
        {
            return await ResolveDefaultRulesetAsync("PEMULA");
        }

        if (string.Equals(normalizedMode, "MAHIR", StringComparison.OrdinalIgnoreCase))
        {
            return await ResolveDefaultRulesetAsync("MAHIR");
        }

        return CreateResolvedPlayRulesetFailure("VALIDATION_ERROR", "Mode ruleset tidak valid.");
    }

    private async Task<ResolvedPlayRuleset> ResolveCustomRulesetAsync(string selectedRulesetName, string selectedRulesetId)
    {
        string normalizedRulesetId = !string.IsNullOrWhiteSpace(selectedRulesetId) ? selectedRulesetId.Trim() : string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedRulesetId) && !string.IsNullOrWhiteSpace(selectedRulesetName))
        {
            NarafinRulesetSummary cachedRuleset = FindCachedRulesetByName(selectedRulesetName);
            if (cachedRuleset != null && !string.IsNullOrWhiteSpace(cachedRuleset.ruleset_id))
            {
                normalizedRulesetId = cachedRuleset.ruleset_id.Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(normalizedRulesetId))
        {
            return CreateResolvedPlayRulesetFailure("VALIDATION_ERROR", "Ruleset custom belum dipilih.");
        }

        Debug.Log("Mengecek ruleset custom dengan ruleset_id: " + normalizedRulesetId);
        NarafinRulesetDetailResult rulesetDetail = await apiClient.GetRulesetDetailAsync(AccessToken, normalizedRulesetId);
        if (!rulesetDetail.Success)
        {
            Debug.LogWarning("Gagal ambil detail ruleset custom. ruleset_id: " + normalizedRulesetId + ", error: " + rulesetDetail.ErrorCode + " - " + rulesetDetail.ErrorMessage);
            return CreateResolvedPlayRulesetFailure(rulesetDetail.ErrorCode, rulesetDetail.ErrorMessage);
        }

        return new ResolvedPlayRuleset
        {
            Success = true,
            RulesetId = rulesetDetail.RulesetId,
            RulesetVersionId = rulesetDetail.RulesetVersionId,
            Version = rulesetDetail.Version,
            Mode = string.IsNullOrWhiteSpace(rulesetDetail.Mode) ? "MAHIR" : NormalizeRulesetMode(rulesetDetail.Mode)
        };
    }

    private async Task<ResolvedPlayRuleset> ResolveDefaultRulesetAsync(string mode)
    {
        string normalizedMode = NormalizeRulesetMode(mode);
        string expectedRulesetName = GetDefaultRulesetNameForMode(normalizedMode);
        NarafinRulesetSummary cachedRuleset = FindCachedRulesetByName(expectedRulesetName);

        if (cachedRuleset != null && !string.IsNullOrWhiteSpace(cachedRuleset.ruleset_id))
        {
            Debug.Log("Mengecek default ruleset yang sudah ada. mode: " + normalizedMode + ", ruleset_id: " + cachedRuleset.ruleset_id);
            NarafinRulesetDetailResult rulesetDetail = await apiClient.GetRulesetDetailAsync(AccessToken, cachedRuleset.ruleset_id.Trim());
            if (rulesetDetail.Success)
            {
                return new ResolvedPlayRuleset
                {
                    Success = true,
                    RulesetId = rulesetDetail.RulesetId,
                    RulesetVersionId = rulesetDetail.RulesetVersionId,
                    Version = rulesetDetail.Version,
                    Mode = string.IsNullOrWhiteSpace(rulesetDetail.Mode) ? normalizedMode : NormalizeRulesetMode(rulesetDetail.Mode)
                };
            }

            if (!string.IsNullOrWhiteSpace(rulesetDetail.ErrorCode) &&
                !IsRulesetNotFoundError(rulesetDetail.ErrorCode))
            {
                Debug.LogWarning("Gagal ambil detail default ruleset. ruleset_id: " + cachedRuleset.ruleset_id + ", error: " + rulesetDetail.ErrorCode + " - " + rulesetDetail.ErrorMessage);
                return CreateResolvedPlayRulesetFailure(rulesetDetail.ErrorCode, rulesetDetail.ErrorMessage);
            }
        }

        TextAsset templateAsset = LoadDefaultRulesetTemplate(normalizedMode);
        if (templateAsset == null)
        {
            return CreateResolvedPlayRulesetFailure("RULESET_TEMPLATE_MISSING", "Template ruleset " + normalizedMode + " tidak ditemukan.");
        }

        Debug.Log("Ruleset default belum ada, membuat baru untuk mode: " + normalizedMode);
        NarafinRulesetCreateResult createResult = await apiClient.CreateRulesetAsync(AccessToken, templateAsset.text);
        if (!createResult.Success)
        {
            return CreateResolvedPlayRulesetFailure(createResult.ErrorCode, createResult.ErrorMessage);
        }

        CacheCreatedRulesetSummary(expectedRulesetName, createResult.RulesetId, createResult.Version);

        return new ResolvedPlayRuleset
        {
            Success = true,
            RulesetId = createResult.RulesetId,
            RulesetVersionId = createResult.RulesetVersionId,
            Version = createResult.Version,
            Mode = normalizedMode
        };
    }

    private static string NormalizeRulesetMode(string mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return "MAHIR";
        }

        string trimmed = mode.Trim();
        if (string.Equals(trimmed, "Pemula", StringComparison.OrdinalIgnoreCase))
        {
            return "PEMULA";
        }

        if (string.Equals(trimmed, "Mahir", StringComparison.OrdinalIgnoreCase))
        {
            return "MAHIR";
        }

        if (string.Equals(trimmed, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            return "CUSTOM";
        }

        return trimmed.ToUpperInvariant();
    }

    private static string GetDefaultRulesetNameForMode(string normalizedMode)
    {
        if (string.Equals(normalizedMode, "PEMULA", StringComparison.OrdinalIgnoreCase))
        {
            return PemulaDefaultRulesetName;
        }

        return MahirDefaultRulesetName;
    }

    private NarafinRulesetSummary FindCachedRulesetByName(string rulesetName)
    {
        if (string.IsNullOrWhiteSpace(rulesetName) || cachedRulesets == null)
        {
            return null;
        }

        for (int i = 0; i < cachedRulesets.Count; i++)
        {
            NarafinRulesetSummary ruleset = cachedRulesets[i];
            if (ruleset == null || string.IsNullOrWhiteSpace(ruleset.name))
            {
                continue;
            }

            if (string.Equals(ruleset.name.Trim(), rulesetName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return ruleset;
            }
        }

        return null;
    }

    private TextAsset LoadDefaultRulesetTemplate(string normalizedMode)
    {
        string resourcePath = string.Equals(normalizedMode, "PEMULA", StringComparison.OrdinalIgnoreCase)
            ? PemulaDefaultRulesetResource
            : MahirDefaultRulesetResource;

        return Resources.Load<TextAsset>(resourcePath);
    }

    private void CacheCreatedRulesetSummary(string rulesetName, string rulesetId, int version)
    {
        if (string.IsNullOrWhiteSpace(rulesetId))
        {
            return;
        }

        if (cachedRulesets == null)
        {
            cachedRulesets = new List<NarafinRulesetSummary>();
        }

        for (int i = cachedRulesets.Count - 1; i >= 0; i--)
        {
            NarafinRulesetSummary existing = cachedRulesets[i];
            if (existing == null)
            {
                continue;
            }

            if (string.Equals(existing.ruleset_id, rulesetId, StringComparison.OrdinalIgnoreCase))
            {
                cachedRulesets.RemoveAt(i);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(existing.name) &&
                string.Equals(existing.name.Trim(), rulesetName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                cachedRulesets.RemoveAt(i);
            }
        }

        cachedRulesets.Insert(0, new NarafinRulesetSummary
        {
            ruleset_id = rulesetId,
            name = rulesetName,
            latest_version = version > 0 ? version : 1,
            status = "ACTIVE",
            is_default = true,
            is_locked_by_session = false
        });
    }

    private static bool IsRulesetNotFoundError(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return false;
        }

        return errorCode.IndexOf("NOT_FOUND", StringComparison.OrdinalIgnoreCase) >= 0 ||
               errorCode.IndexOf("RULESET_NOT_FOUND", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static ResolvedPlayRuleset CreateResolvedPlayRulesetFailure(string errorCode, string errorMessage)
    {
        return new ResolvedPlayRuleset
        {
            Success = false,
            ErrorCode = errorCode ?? string.Empty,
            ErrorMessage = errorMessage ?? string.Empty
        };
    }

    private sealed class ResolvedPlayRuleset
    {
        public bool Success;
        public string ErrorCode;
        public string ErrorMessage;
        public string RulesetId;
        public string RulesetVersionId;
        public int Version;
        public string Mode;
    }

    private void SetLastAuthError(string errorCode, string errorMessage)
    {
        lastAuthErrorCode = errorCode ?? string.Empty;
        lastAuthErrorMessage = errorMessage ?? string.Empty;
    }

    private void ClearLastAuthError()
    {
        lastAuthErrorCode = string.Empty;
        lastAuthErrorMessage = string.Empty;
    }

    private static NarafinPlaySessionResult CreatePlaySessionFailure(string errorCode, string errorMessage)
    {
        return new NarafinPlaySessionResult
        {
            Success = false,
            ErrorCode = errorCode ?? string.Empty,
            ErrorMessage = errorMessage ?? string.Empty
        };
    }

    private static NarafinPlayBootstrapResult CreatePlayBootstrapFailure(string errorCode, string errorMessage)
    {
        return new NarafinPlayBootstrapResult
        {
            Success = false,
            ErrorCode = errorCode ?? string.Empty,
            ErrorMessage = errorMessage ?? string.Empty,
            Rulesets = new List<NarafinRulesetSummary>(),
            Players = new List<NarafinPlayerSummary>()
        };
    }
}

public class NarafinPlaySessionResult
{
    public bool Success;
    public string ErrorCode;
    public string ErrorMessage;
    public string SessionId;
    public string RulesetId;
    public string RulesetVersionId;
}
