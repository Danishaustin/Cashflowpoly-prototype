using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class NarafinAuthSession
{
    public string user_id;
    public string username;
    public string role;
    public string display_name;
    public string access_token;
    public string expires_at;
}

public class NarafinAuthResult
{
    public bool Success;
    public string ErrorCode;
    public string ErrorMessage;
    public NarafinAuthSession Session;
}

[Serializable]
internal class NarafinLoginRequest
{
    public string username;
    public string password;
}

[Serializable]
internal class NarafinRegisterRequest
{
    public string username;
    public string password;
    public string role;
    public string display_name;
}

[Serializable]
internal class NarafinAuthResponse
{
    public string user_id;
    public string username;
    public string role;
    public string display_name;
    public string access_token;
    public string expires_at;
}

[Serializable]
internal class NarafinErrorResponse
{
    public string error_code;
    public string message;
    public List<NarafinErrorDetail> details;
    public string trace_id;

    public string FormatDetails()
    {
        if (details == null || details.Count == 0)
        {
            return string.Empty;
        }

        List<string> parts = new List<string>();
        foreach (NarafinErrorDetail detail in details)
        {
            if (detail != null && !string.IsNullOrWhiteSpace(detail.field))
            {
                parts.Add(detail.field + ": " + detail.issue);
            }
        }

        return string.Join(", ", parts);
    }
}

[Serializable]
internal class NarafinErrorDetail
{
    public string field;
    public string issue;
}

[Serializable]
public class NarafinRulesetSummary
{
    public string ruleset_id;
    public string name;
    public int latest_version;
    public string status;
    public bool is_default;
    public bool is_locked_by_session;
}

[Serializable]
internal class NarafinRulesetListResponse
{
    public List<NarafinRulesetSummary> items;
}

internal class NarafinRulesetVersionSummary
{
    public string ruleset_version_id;
    public int version;
    public string status;
    public string created_at;
}

internal class NarafinRulesetDetailResponse
{
    public string ruleset_id;
    public string name;
    public string description;
    public List<NarafinRulesetVersionSummary> versions;
    public string ruleset_version_id;
    public int version;
    public string mode;
    public bool is_default;
    public bool is_locked_by_session;
}

[Serializable]
internal class NarafinRulesetCreateResponse
{
    public string ruleset_id;
    public string ruleset_version_id;
    public int version;
}

public class NarafinRulesetListResult
{
    public bool Success;
    public string ErrorCode;
    public string ErrorMessage;
    public List<NarafinRulesetSummary> Items;
}

public class NarafinRulesetDetailResult
{
    public bool Success;
    public string ErrorCode;
    public string ErrorMessage;
    public string RulesetId;
    public string RulesetVersionId;
    public int Version;
    public string Name;
    public string Mode;
}

public class NarafinRulesetCreateResult
{
    public bool Success;
    public string ErrorCode;
    public string ErrorMessage;
    public string RulesetId;
    public string RulesetVersionId;
    public int Version;
}

[Serializable]
public class NarafinPlayerSummary
{
    public string user_id;
    public string display_name;
    public string username;
    public string role;
}

[Serializable]
internal class NarafinPlayerListResponse
{
    public List<NarafinPlayerSummary> items;
}

internal class NarafinCreateSessionRequest
{
    public string session_name;
    public string mode;
    public string ruleset_version_id;
}

internal class NarafinAddSessionPlayerRequest
{
    public string user_id;
    public int player_order_no;
}

internal class NarafinSessionCreateResponse
{
    public string session_id;
    public string ruleset_id;
    public string ruleset_version_id;
}

[Serializable]
internal class NarafinSessionStateResponse
{
    public List<NarafinSessionStatePlayer> players;
}

[Serializable]
internal class NarafinSessionEventsResponse
{
    public List<NarafinSessionEventSummary> items;
    public string next_cursor;
    public bool has_more;
}

[Serializable]
internal class NarafinSessionEventSummary
{
    public long sequence_number;
}

[Serializable]
public class NarafinSessionStatePlayer
{
    public string session_player_id;
    public string user_id;
    public int player_order_no;
    public string name;
    public int coins;
}

[Serializable]
internal class NarafinRulesetComponentsResponse
{
    public string ruleset_id;
    public string ruleset_version_id;
    public int version;
    public string mode;
    public NarafinRulesetSetupDefinition definition;
}

// primary_need_max_per_day sengaja tidak dideklarasikan karena bisa bernilai null di ruleset.
[Serializable]
public class NarafinRulesetSettings
{
    public int actions_per_turn;
    public int initial_coins;
    public int initial_happiness;
    public int initial_saving;
    public int finish_day;
    public int min_players;
    public int max_players;
    public int max_ingredient_total;
    public int max_same_ingredient;
    public bool require_primary_before_others;
    public bool gold_trade_allow_buy;
    public bool gold_trade_allow_sell;
    public bool loan_enabled;
    public bool insurance_enabled;
    public int freelance_income;
}

[Serializable]
public class NarafinRulesetSetupDefinition
{
    public string mode;
    public NarafinRulesetSettings settings;
    public List<NarafinSetupIngredient> ingredients;
    public List<NarafinSetupMission> collection_missions;
    public List<NarafinSetupTieBreaker> tie_breakers;
    public List<NarafinSetupLoan> sharia_loans;
    public List<NarafinSetupInsurance> insurance_products;
}

[Serializable]
public class NarafinSetupIngredient
{
    public string id;
    public string nama;
    public int hargaBeli;
    public int cardQty;
}

[Serializable]
public class NarafinSetupMission
{
    public string id;
    public string nama;
    public int penaltyPoints;
}

[Serializable]
public class NarafinSetupTieBreaker
{
    public string tie_breaker_code;
    public int tie_number;
    public int card_qty;
}

[Serializable]
public class NarafinSetupLoan
{
    public string loan_code;
    public string item_name;
    public int principal;
    public int repayment_amount;
    public int duration_days;
    public int penalty_points;
    public int card_qty;
}

[Serializable]
public class NarafinSetupInsurance
{
    public string product_code;
    public string item_name;
    public int premium;
    public int usage_limit;
    public int card_qty;
}

[Serializable]
internal class NarafinSessionSetupRequest
{
    public string client_request_id;
    public List<NarafinSessionSetupPlayer> players;
}

[Serializable]
internal class NarafinSessionSetupPlayer
{
    public string session_player_id;
    public string tie_breaker_code;
    public string ingredient_card_id;
    public int gold_quantity;
    public string mission_id;
    public string loan_code;
    public string insurance_product_code;
}

public class NarafinPlayerListResult
{
    public bool Success;
    public string ErrorCode;
    public string ErrorMessage;
    public List<NarafinPlayerSummary> Items;
}

public class NarafinPlayBootstrapResult
{
    public bool Success;
    public string ErrorCode;
    public string ErrorMessage;
    public List<NarafinRulesetSummary> Rulesets;
    public List<NarafinPlayerSummary> Players;
}

public class NarafinSessionCreateResult
{
    public bool Success;
    public string ErrorCode;
    public string ErrorMessage;
    public string SessionId;
    public string RulesetId;
    public string RulesetVersionId;
}

public class NarafinSessionOperationResult
{
    public bool Success;
    public string ErrorCode;
    public string ErrorMessage;
}

public class NarafinSessionStateResult
{
    public bool Success;
    public string ErrorCode;
    public string ErrorMessage;
    public List<NarafinSessionStatePlayer> Players;
}

public class NarafinSessionEventSequenceResult
{
    public bool Success;
    public string ErrorCode;
    public string ErrorMessage;
    public int LastSequenceNumber;
}

public class NarafinRulesetSetupCatalogResult
{
    public bool Success;
    public string ErrorCode;
    public string ErrorMessage;
    public NarafinRulesetSetupDefinition Definition;
}

public sealed class NarafinApiClient
{
    private const string BaseUrl = "https://narafin.org";
    private const string LoginPath = "/api/v1/auth/login";
    private const string RegisterPath = "/api/v1/auth/register";
    private const string RulesetsPath = "/api/v1/rulesets";
    private const string PlayersPath = "/api/v1/players";
    private const string RulesetDetailPathPrefix = "/api/v1/rulesets/";
    private const string SessionsPath = "/api/v1/sessions";
    private const string EventsPath = "/api/v1/events";
    private const int RequestTimeoutSeconds = 10;
    private const string NetworkOfflineCode = "NETWORK_OFFLINE";
    private const string RequestTimeoutCode = "REQUEST_TIMEOUT";
    private const string ServerDownCode = "SERVER_DOWN";
    private const string ServerUnreachableCode = "SERVER_UNREACHABLE";
    private const string AuthRequiredCode = "AUTH_SESSION_MISSING";
    private const string InvalidCredentialsCode = "AUTH_INVALID_CREDENTIALS";
    private const string UsernameTakenCode = "USERNAME_ALREADY_EXISTS";
    private const string AuthForbiddenCode = "AUTH_FORBIDDEN";
    private static readonly List<NarafinRulesetSummary> OfflineRulesets = new List<NarafinRulesetSummary>
    {
        new NarafinRulesetSummary
        {
            ruleset_id = "offline-ruleset-01",
            name = "Offline Demo Ruleset",
            latest_version = 1,
            status = "ACTIVE",
            is_default = true,
            is_locked_by_session = false
        },
        new NarafinRulesetSummary
        {
            ruleset_id = "offline-ruleset-02",
            name = "Offline Test Ruleset",
            latest_version = 1,
            status = "ACTIVE",
            is_default = false,
            is_locked_by_session = false
        }
    };

    private static readonly List<NarafinPlayerSummary> OfflinePlayers = new List<NarafinPlayerSummary>
    {
        new NarafinPlayerSummary
        {
            user_id = "offline-player-1",
            display_name = "Player 1",
            username = "player1",
            role = "PLAYER"
        },
        new NarafinPlayerSummary
        {
            user_id = "offline-player-2",
            display_name = "Player 2",
            username = "player2",
            role = "PLAYER"
        },
        new NarafinPlayerSummary
        {
            user_id = "offline-player-3",
            display_name = "Player 3",
            username = "player3",
            role = "PLAYER"
        },
        new NarafinPlayerSummary
        {
            user_id = "offline-player-4",
            display_name = "Player 4",
            username = "player4",
            role = "PLAYER"
        }
    };

    private static readonly object OfflineDataLock = new object();

    public Task<NarafinAuthResult> LoginAsync(string username, string password)
    {
        if (NarafinRuntimeConfig.UseOfflineMode)
        {
            return Task.FromResult(CreateOfflineAuthSuccess(username, "INSTRUCTOR"));
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return Task.FromResult(CreateFailureResult(NetworkOfflineCode, "Koneksi internet belum tersambung."));
        }

        NarafinLoginRequest request = new NarafinLoginRequest
        {
            username = username,
            password = password
        };

        return SendAuthRequestAsync(LoginPath, request, "login");
    }

    public Task<NarafinAuthResult> RegisterAsync(string username, string password)
    {
        return RegisterAsync(username, password, "INSTRUCTOR");
    }

    public Task<NarafinAuthResult> RegisterAsync(string username, string password, string role)
    {
        if (NarafinRuntimeConfig.UseOfflineMode)
        {
            string resolvedRole = string.IsNullOrWhiteSpace(role) ? "INSTRUCTOR" : role;
            if (string.Equals(resolvedRole, "PLAYER", StringComparison.OrdinalIgnoreCase))
            {
                AddOfflinePlayer(username);
            }

            return Task.FromResult(CreateOfflineAuthSuccess(username, resolvedRole));
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return Task.FromResult(CreateFailureResult(NetworkOfflineCode, "Koneksi internet belum tersambung."));
        }

        NarafinRegisterRequest request = new NarafinRegisterRequest
        {
            username = username,
            password = password,
            role = string.IsNullOrWhiteSpace(role) ? "INSTRUCTOR" : role,
            display_name = username
        };

        return SendAuthRequestAsync(RegisterPath, request, "register");
    }

    public Task<NarafinRulesetListResult> GetRulesetsAsync(string accessToken)
    {
        if (NarafinRuntimeConfig.UseOfflineMode)
        {
            return Task.FromResult(new NarafinRulesetListResult
            {
                Success = true,
                Items = CloneOfflineRulesets()
            });
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult(new NarafinRulesetListResult
            {
                Success = false,
                ErrorCode = AuthRequiredCode,
                ErrorMessage = "Access token tidak tersedia."
            });
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return Task.FromResult(new NarafinRulesetListResult
            {
                Success = false,
                ErrorCode = NetworkOfflineCode,
                ErrorMessage = "Koneksi internet belum tersambung."
            });
        }

        return GetRulesetsInternalAsync(accessToken);
    }

    public Task<NarafinRulesetDetailResult> GetRulesetDetailAsync(string accessToken, string rulesetId)
    {
        if (NarafinRuntimeConfig.UseOfflineMode)
        {
            NarafinRulesetSummary offlineRuleset = null;
            lock (OfflineDataLock)
            {
                for (int i = 0; i < OfflineRulesets.Count; i++)
                {
                    if (OfflineRulesets[i] != null && string.Equals(OfflineRulesets[i].ruleset_id, rulesetId, StringComparison.OrdinalIgnoreCase))
                    {
                        offlineRuleset = OfflineRulesets[i];
                        break;
                    }
                }
            }

            return Task.FromResult(new NarafinRulesetDetailResult
            {
                Success = offlineRuleset != null,
                RulesetId = offlineRuleset != null ? offlineRuleset.ruleset_id : string.Empty,
                RulesetVersionId = offlineRuleset != null ? offlineRuleset.ruleset_id + "_v" + offlineRuleset.latest_version : string.Empty,
                Version = offlineRuleset != null ? offlineRuleset.latest_version : 0,
                Name = offlineRuleset != null ? offlineRuleset.name : string.Empty,
                ErrorCode = offlineRuleset != null ? string.Empty : "RULESET_NOT_FOUND",
                ErrorMessage = offlineRuleset != null ? string.Empty : "Ruleset tidak ditemukan."
            });
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult(new NarafinRulesetDetailResult
            {
                Success = false,
                ErrorCode = AuthRequiredCode,
                ErrorMessage = "Access token tidak tersedia."
            });
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return Task.FromResult(new NarafinRulesetDetailResult
            {
                Success = false,
                ErrorCode = NetworkOfflineCode,
                ErrorMessage = "Koneksi internet belum tersambung."
            });
        }

        return GetRulesetDetailInternalAsync(accessToken, rulesetId);
    }

    public Task<NarafinPlayerListResult> GetPlayersAsync(string accessToken)
    {
        if (NarafinRuntimeConfig.UseOfflineMode)
        {
            return Task.FromResult(new NarafinPlayerListResult
            {
                Success = true,
                Items = CloneOfflinePlayers()
            });
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult(new NarafinPlayerListResult
            {
                Success = false,
                ErrorCode = AuthRequiredCode,
                ErrorMessage = "Access token tidak tersedia."
            });
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return Task.FromResult(new NarafinPlayerListResult
            {
                Success = false,
                ErrorCode = NetworkOfflineCode,
                ErrorMessage = "Koneksi internet belum tersambung."
            });
        }

        return GetPlayersInternalAsync(accessToken);
    }

    public Task<NarafinSessionCreateResult> CreateSessionAsync(string accessToken, string sessionName, string mode, string rulesetVersionId)
    {
        if (NarafinRuntimeConfig.UseOfflineMode)
        {
            return Task.FromResult(new NarafinSessionCreateResult
            {
                Success = true,
                SessionId = "offline_session_" + Guid.NewGuid().ToString("N"),
                RulesetId = "offline-ruleset-01",
                RulesetVersionId = string.IsNullOrWhiteSpace(rulesetVersionId) ? "1" : rulesetVersionId
            });
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult(CreateSessionFailureResult(AuthRequiredCode, "Access token tidak tersedia."));
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return Task.FromResult(CreateSessionFailureResult(NetworkOfflineCode, "Koneksi internet belum tersambung."));
        }

        return CreateSessionInternalAsync(accessToken, sessionName, mode, rulesetVersionId);
    }

    public Task<NarafinRulesetCreateResult> CreateRulesetAsync(string accessToken, string rawJsonBody)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult(CreateRulesetFailureResult(AuthRequiredCode, "Access token tidak tersedia."));
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return Task.FromResult(CreateRulesetFailureResult(NetworkOfflineCode, "Koneksi internet belum tersambung."));
        }

        if (string.IsNullOrWhiteSpace(rawJsonBody))
        {
            return Task.FromResult(CreateRulesetFailureResult("VALIDATION_ERROR", "Payload ruleset tidak boleh kosong."));
        }

        return CreateRulesetInternalAsync(accessToken, rawJsonBody);
    }

    public Task<NarafinSessionOperationResult> AddPlayerToSessionAsync(string accessToken, string sessionId, string userId, int playerOrderNo)
    {
        if (NarafinRuntimeConfig.UseOfflineMode)
        {
            return Task.FromResult(new NarafinSessionOperationResult
            {
                Success = true
            });
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult(CreateSessionOperationFailureResult(AuthRequiredCode, "Access token tidak tersedia."));
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return Task.FromResult(CreateSessionOperationFailureResult(NetworkOfflineCode, "Koneksi internet belum tersambung."));
        }

        return AddPlayerToSessionInternalAsync(accessToken, sessionId, userId, playerOrderNo);
    }

    public Task<NarafinSessionOperationResult> StartSessionAsync(string accessToken, string sessionId)
    {
        if (NarafinRuntimeConfig.UseOfflineMode)
        {
            return Task.FromResult(new NarafinSessionOperationResult
            {
                Success = true
            });
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult(CreateSessionOperationFailureResult(AuthRequiredCode, "Access token tidak tersedia."));
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return Task.FromResult(CreateSessionOperationFailureResult(NetworkOfflineCode, "Koneksi internet belum tersambung."));
        }

        return StartSessionInternalAsync(accessToken, sessionId);
    }

    public Task<NarafinSessionStateResult> GetSessionStateAsync(string accessToken, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult(CreateSessionStateFailureResult(AuthRequiredCode, "Access token tidak tersedia."));
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return Task.FromResult(CreateSessionStateFailureResult(NetworkOfflineCode, "Koneksi internet belum tersambung."));
        }

        return GetSessionStateInternalAsync(accessToken, sessionId);
    }

    public Task<NarafinSessionEventSequenceResult> GetLastSessionEventSequenceAsync(string accessToken, string sessionId)
    {
        if (NarafinRuntimeConfig.UseOfflineMode)
        {
            return Task.FromResult(new NarafinSessionEventSequenceResult { Success = true, LastSequenceNumber = 0 });
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult(CreateSessionEventSequenceFailureResult(AuthRequiredCode, "Access token tidak tersedia."));
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return Task.FromResult(CreateSessionEventSequenceFailureResult(NetworkOfflineCode, "Koneksi internet belum tersambung."));
        }

        return GetLastSessionEventSequenceInternalAsync(accessToken, sessionId);
    }

    public Task<NarafinRulesetSetupCatalogResult> GetRulesetSetupCatalogAsync(string accessToken, string rulesetId, int version)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult(CreateSetupCatalogFailureResult(AuthRequiredCode, "Access token tidak tersedia."));
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return Task.FromResult(CreateSetupCatalogFailureResult(NetworkOfflineCode, "Koneksi internet belum tersambung."));
        }

        return GetRulesetSetupCatalogInternalAsync(accessToken, rulesetId, version);
    }

    public Task<NarafinSessionOperationResult> ValidateSessionSetupAsync(string accessToken, string sessionId, string rawJsonBody)
    {
        return SendSessionSetupAsync(accessToken, sessionId, rawJsonBody, true);
    }

    public Task<NarafinSessionOperationResult> SaveSessionSetupAsync(string accessToken, string sessionId, string rawJsonBody)
    {
        return SendSessionSetupAsync(accessToken, sessionId, rawJsonBody, false);
    }

    public Task<NarafinSessionOperationResult> PostEventAsync(string accessToken, string rawJsonBody)
    {
        if (NarafinRuntimeConfig.UseOfflineMode)
        {
            return Task.FromResult(new NarafinSessionOperationResult
            {
                Success = true
            });
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult(CreateSessionOperationFailureResult(AuthRequiredCode, "Access token tidak tersedia."));
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return Task.FromResult(CreateSessionOperationFailureResult(NetworkOfflineCode, "Koneksi internet belum tersambung."));
        }

        if (string.IsNullOrWhiteSpace(rawJsonBody))
        {
            return Task.FromResult(CreateSessionOperationFailureResult("VALIDATION_ERROR", "Payload event tidak boleh kosong."));
        }

        return PostEventInternalAsync(accessToken, rawJsonBody);
    }

    private async Task<NarafinAuthResult> SendAuthRequestAsync<TRequest>(string path, TRequest requestBody, string actionName)
        where TRequest : class
    {
        string url = BaseUrl + path;
        string jsonBody = JsonUtility.ToJson(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = RequestTimeoutSeconds;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            await SendRequestAsync(request);

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

            if (request.result == UnityWebRequest.Result.Success)
            {
                return BuildSuccessResult(responseText, actionName);
            }

            return BuildFailureResult(responseText, request.responseCode, request.error, actionName);
        }
    }

    private async Task<NarafinRulesetListResult> GetRulesetsInternalAsync(string accessToken)
    {
        string url = BaseUrl + RulesetsPath;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = RequestTimeoutSeconds;
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);

            await SendRequestAsync(request);

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

            if (request.result == UnityWebRequest.Result.Success)
            {
                return BuildRulesetListSuccessResult(responseText);
            }

            return BuildRulesetListFailureResult(responseText, request.responseCode, request.error);
        }
    }

    private async Task<NarafinPlayerListResult> GetPlayersInternalAsync(string accessToken)
    {
        string url = BaseUrl + PlayersPath;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = RequestTimeoutSeconds;
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);

            await SendRequestAsync(request);

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

            if (request.result == UnityWebRequest.Result.Success)
            {
                return BuildPlayerListSuccessResult(responseText);
            }

            return BuildPlayerListFailureResult(responseText, request.responseCode, request.error);
        }
    }

    private async Task<NarafinRulesetDetailResult> GetRulesetDetailInternalAsync(string accessToken, string rulesetId)
    {
        string url = BaseUrl + RulesetDetailPathPrefix + rulesetId;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = RequestTimeoutSeconds;
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);

            await SendRequestAsync(request);

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

            if (request.result == UnityWebRequest.Result.Success)
            {
                return BuildRulesetDetailSuccessResult(responseText);
            }

            return BuildRulesetDetailFailureResult(responseText, request.responseCode, request.error);
        }
    }

    private async Task<NarafinSessionCreateResult> CreateSessionInternalAsync(string accessToken, string sessionName, string mode, string rulesetVersionId)
    {
        string url = BaseUrl + SessionsPath;
        NarafinCreateSessionRequest requestBody = new NarafinCreateSessionRequest
        {
            session_name = sessionName,
            mode = string.IsNullOrWhiteSpace(mode) ? "MAHIR" : mode.Trim().ToUpperInvariant(),
            ruleset_version_id = rulesetVersionId
        };

        using (UnityWebRequest request = CreateJsonPostRequest(url, JsonUtility.ToJson(requestBody)))
        {
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);

            await SendRequestAsync(request);

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            Debug.Log("Narafin create session response => code: " + request.responseCode + ", result: " + request.result + ", error: " + request.error + ", body: " + responseText);

            if (request.result == UnityWebRequest.Result.Success)
            {
                return BuildSessionCreateSuccessResult(responseText);
            }

            return BuildSessionCreateFailureResult(responseText, request.responseCode, request.error);
        }
    }

    private async Task<NarafinRulesetCreateResult> CreateRulesetInternalAsync(string accessToken, string rawJsonBody)
    {
        string url = BaseUrl + RulesetsPath;

        using (UnityWebRequest request = CreateJsonPostRequest(url, rawJsonBody))
        {
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);

            await SendRequestAsync(request);

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            Debug.Log("Narafin create ruleset response => code: " + request.responseCode + ", result: " + request.result + ", error: " + request.error + ", body: " + responseText);

            if (request.result == UnityWebRequest.Result.Success)
            {
                return BuildRulesetCreateSuccessResult(responseText);
            }

            return BuildRulesetCreateFailureResult(responseText, request.responseCode, request.error);
        }
    }

    private async Task<NarafinSessionOperationResult> AddPlayerToSessionInternalAsync(string accessToken, string sessionId, string userId, int playerOrderNo)
    {
        string url = BaseUrl + SessionsPath + "/" + sessionId + "/players";
        NarafinAddSessionPlayerRequest requestBody = new NarafinAddSessionPlayerRequest
        {
            user_id = userId,
            player_order_no = playerOrderNo
        };

        using (UnityWebRequest request = CreateJsonPostRequest(url, JsonUtility.ToJson(requestBody)))
        {
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);

            await SendRequestAsync(request);

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

            if (request.result == UnityWebRequest.Result.Success)
            {
                return new NarafinSessionOperationResult
                {
                    Success = true
                };
            }

            return BuildSessionOperationFailureResult(responseText, request.responseCode, request.error, "menambahkan player");
        }
    }

    private async Task<NarafinSessionOperationResult> StartSessionInternalAsync(string accessToken, string sessionId)
    {
        string url = BaseUrl + SessionsPath + "/" + sessionId + "/start";

        using (UnityWebRequest request = CreateJsonPostRequest(url, "{}"))
        {
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);

            await SendRequestAsync(request);

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

            if (request.result == UnityWebRequest.Result.Success)
            {
                return new NarafinSessionOperationResult
                {
                    Success = true
                };
            }

            return BuildSessionOperationFailureResult(responseText, request.responseCode, request.error, "memulai");
        }
    }

    private async Task<NarafinSessionStateResult> GetSessionStateInternalAsync(string accessToken, string sessionId)
    {
        string url = BaseUrl + SessionsPath + "/" + sessionId + "/state";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = RequestTimeoutSeconds;
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);

            await SendRequestAsync(request);
            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

            if (request.result != UnityWebRequest.Result.Success)
            {
                return BuildSessionStateFailureResult(responseText, request.responseCode, request.error);
            }

            try
            {
                NarafinSessionStateResponse response = JsonUtility.FromJson<NarafinSessionStateResponse>(responseText);
                if (response == null || response.players == null)
                {
                    return CreateSessionStateFailureResult("INVALID_RESPONSE", "Narafin API tidak mengembalikan daftar peserta session.");
                }

                return new NarafinSessionStateResult
                {
                    Success = true,
                    Players = response.players
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Gagal membaca state session Narafin: " + ex.Message);
                return CreateSessionStateFailureResult("INVALID_RESPONSE", "State session Narafin tidak valid.");
            }
        }
    }

    private async Task<NarafinSessionEventSequenceResult> GetLastSessionEventSequenceInternalAsync(string accessToken, string sessionId)
    {
        int lastSequenceNumber = 0;
        string cursor = string.Empty;
        bool hasMore = false;

        do
        {
            string url = BaseUrl + SessionsPath + "/" + sessionId + "/events?limit=100";
            if (!string.IsNullOrWhiteSpace(cursor))
            {
                url += "&cursor=" + UnityWebRequest.EscapeURL(cursor);
            }

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = RequestTimeoutSeconds;
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + accessToken);

                await SendRequestAsync(request);
                string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    return BuildSessionEventSequenceFailureResult(responseText, request.responseCode, request.error);
                }

                try
                {
                    NarafinSessionEventsResponse response = JsonUtility.FromJson<NarafinSessionEventsResponse>(responseText);
                    if (response == null)
                    {
                        return CreateSessionEventSequenceFailureResult("INVALID_RESPONSE", "Daftar event session Narafin tidak valid.");
                    }

                    if (response.items != null)
                    {
                        foreach (NarafinSessionEventSummary item in response.items)
                        {
                            if (item != null && item.sequence_number > lastSequenceNumber)
                            {
                                if (item.sequence_number > int.MaxValue)
                                {
                                    return CreateSessionEventSequenceFailureResult("SEQUENCE_OUT_OF_RANGE", "Sequence event Narafin melebihi batas aplikasi.");
                                }

                                lastSequenceNumber = (int)item.sequence_number;
                            }
                        }
                    }

                    hasMore = response.has_more;
                    cursor = response.next_cursor;
                    if (hasMore && string.IsNullOrWhiteSpace(cursor))
                    {
                        return CreateSessionEventSequenceFailureResult("INVALID_RESPONSE", "Cursor event Narafin tidak tersedia.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Gagal membaca daftar event session Narafin: " + ex.Message);
                    return CreateSessionEventSequenceFailureResult("INVALID_RESPONSE", "Daftar event session Narafin tidak valid.");
                }
            }
        }
        while (hasMore);

        return new NarafinSessionEventSequenceResult
        {
            Success = true,
            LastSequenceNumber = lastSequenceNumber
        };
    }

    private async Task<NarafinRulesetSetupCatalogResult> GetRulesetSetupCatalogInternalAsync(string accessToken, string rulesetId, int version)
    {
        string url = BaseUrl + RulesetDetailPathPrefix + rulesetId + "/components?version=" + version;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = RequestTimeoutSeconds;
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);

            await SendRequestAsync(request);
            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

            if (request.result != UnityWebRequest.Result.Success)
            {
                return BuildSetupCatalogFailureResult(responseText, request.responseCode, request.error);
            }

            try
            {
                NarafinRulesetComponentsResponse response = JsonUtility.FromJson<NarafinRulesetComponentsResponse>(responseText);
                if (response == null || response.definition == null)
                {
                    return CreateSetupCatalogFailureResult("INVALID_RESPONSE", "Komponen ruleset Narafin tidak valid.");
                }

                return new NarafinRulesetSetupCatalogResult
                {
                    Success = true,
                    Definition = response.definition
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Gagal membaca komponen ruleset Narafin: " + ex.Message);
                return CreateSetupCatalogFailureResult("INVALID_RESPONSE", "Komponen ruleset Narafin tidak valid.");
            }
        }
    }

    private Task<NarafinSessionOperationResult> SendSessionSetupAsync(string accessToken, string sessionId, string rawJsonBody, bool validateOnly)
    {
        if (NarafinRuntimeConfig.UseOfflineMode)
        {
            return Task.FromResult(new NarafinSessionOperationResult { Success = true });
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult(CreateSessionOperationFailureResult(AuthRequiredCode, "Access token tidak tersedia."));
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return Task.FromResult(CreateSessionOperationFailureResult(NetworkOfflineCode, "Koneksi internet belum tersambung."));
        }

        if (string.IsNullOrWhiteSpace(rawJsonBody))
        {
            return Task.FromResult(CreateSessionOperationFailureResult("VALIDATION_ERROR", "Payload setup session tidak boleh kosong."));
        }

        return SendSessionSetupInternalAsync(accessToken, sessionId, rawJsonBody, validateOnly);
    }

    private async Task<NarafinSessionOperationResult> SendSessionSetupInternalAsync(string accessToken, string sessionId, string rawJsonBody, bool validateOnly)
    {
        string path = validateOnly ? "/setup/validate" : "/setup";
        string actionName = validateOnly ? "memvalidasi pembagian awal" : "menyimpan pembagian awal";
        string url = BaseUrl + SessionsPath + "/" + sessionId + path;

        using (UnityWebRequest request = CreateJsonPostRequest(url, rawJsonBody))
        {
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            await SendRequestAsync(request);

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            Debug.Log("Narafin session setup response => code: " + request.responseCode + ", result: " + request.result + ", body: " + responseText);

            if (request.result == UnityWebRequest.Result.Success)
            {
                return new NarafinSessionOperationResult { Success = true };
            }

            return BuildSessionOperationFailureResult(responseText, request.responseCode, request.error, actionName);
        }
    }

    private async Task<NarafinSessionOperationResult> PostEventInternalAsync(string accessToken, string rawJsonBody)
    {
        string url = BaseUrl + EventsPath;

        using (UnityWebRequest request = CreateJsonPostRequest(url, rawJsonBody))
        {
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);

            await SendRequestAsync(request);

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            Debug.Log("Narafin post event response => code: " + request.responseCode + ", result: " + request.result + ", error: " + request.error + ", body: " + responseText);

            if (request.result == UnityWebRequest.Result.Success)
            {
                return new NarafinSessionOperationResult
                {
                    Success = true
                };
            }

            return BuildSessionOperationFailureResult(responseText, request.responseCode, request.error, "mengirim event");
        }
    }

    private static UnityWebRequest CreateJsonPostRequest(string url, string jsonBody)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(jsonBody) ? "{}" : jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = RequestTimeoutSeconds;
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");
        return request;
    }

    private static async Task SendRequestAsync(UnityWebRequest request)
    {
        TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();

        if (operation.isDone)
        {
            completionSource.TrySetResult(true);
        }
        else
        {
            operation.completed += _ =>
            {
                completionSource.TrySetResult(true);
            };
        }

        await completionSource.Task;
    }

    private static NarafinAuthResult BuildSuccessResult(string responseText, string actionName)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return new NarafinAuthResult
            {
                Success = false,
                ErrorCode = "EMPTY_RESPONSE",
                ErrorMessage = "Narafin API mengembalikan respons kosong saat " + actionName + "."
            };
        }

        NarafinAuthResponse response = JsonUtility.FromJson<NarafinAuthResponse>(responseText);

        if (response == null || string.IsNullOrWhiteSpace(response.access_token))
        {
            return new NarafinAuthResult
            {
                Success = false,
                ErrorCode = "INVALID_RESPONSE",
                ErrorMessage = "Narafin API mengembalikan data autentikasi yang tidak valid saat " + actionName + "."
            };
        }

        return new NarafinAuthResult
        {
            Success = true,
            Session = new NarafinAuthSession
            {
                user_id = response.user_id,
                username = response.username,
                role = response.role,
                display_name = response.display_name,
                access_token = response.access_token,
                expires_at = response.expires_at
            }
        };
    }

    private static NarafinAuthResult BuildFailureResult(string responseText, long responseCode, string requestError, string actionName)
    {
        string normalizedRequestError = requestError ?? string.Empty;
        string errorCode = "HTTP_" + responseCode;
        string errorMessage = "Narafin API gagal saat " + actionName + ".";

        if (IsTimeout(responseCode, normalizedRequestError))
        {
            return CreateFailureResult(RequestTimeoutCode, "Connection Timeout.");
        }

        if (responseCode == 401 || responseCode == 403)
        {
            return CreateFailureResult(InvalidCredentialsCode, "Username atau password salah.");
        }

        if (responseCode == 409 && string.Equals(actionName, "register", StringComparison.OrdinalIgnoreCase))
        {
            return CreateFailureResult(UsernameTakenCode, "Username sudah digunakan.");
        }

        if (responseCode >= 500)
        {
            return CreateFailureResult(ServerDownCode, "Server sedang bermasalah.");
        }

        if (responseCode == 0)
        {
            return CreateFailureResult(ServerUnreachableCode, "Server tidak bisa dihubungi.");
        }

        if (!string.IsNullOrWhiteSpace(responseText))
        {
            try
            {
                NarafinErrorResponse errorResponse = JsonUtility.FromJson<NarafinErrorResponse>(responseText);
                if (errorResponse != null)
                {
                    if (!string.IsNullOrWhiteSpace(errorResponse.error_code))
                    {
                        errorCode = errorResponse.error_code;
                    }

                    if (!string.IsNullOrWhiteSpace(errorResponse.message))
                    {
                        errorMessage = errorResponse.message;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Gagal membaca respons error Narafin API: " + ex.Message);
            }
        }

        if (!string.IsNullOrWhiteSpace(normalizedRequestError))
        {
            errorMessage = errorMessage + " (" + normalizedRequestError + ")";
        }

        return new NarafinAuthResult
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    private static NarafinRulesetListResult BuildRulesetListSuccessResult(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return new NarafinRulesetListResult
            {
                Success = false,
                ErrorCode = "EMPTY_RESPONSE",
                ErrorMessage = "Narafin API mengembalikan respons kosong saat mengambil daftar ruleset."
            };
        }

        try
        {
            NarafinRulesetListResponse response = JsonUtility.FromJson<NarafinRulesetListResponse>(responseText);
            List<NarafinRulesetSummary> items = response != null && response.items != null
                ? response.items
                : new List<NarafinRulesetSummary>();

            return new NarafinRulesetListResult
            {
                Success = true,
                Items = items
            };
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal membaca daftar ruleset: " + ex.Message);
            return new NarafinRulesetListResult
            {
                Success = false,
                ErrorCode = "INVALID_RESPONSE",
                ErrorMessage = "Narafin API mengembalikan data ruleset yang tidak valid."
            };
        }
    }

    private static NarafinPlayerListResult BuildPlayerListSuccessResult(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return new NarafinPlayerListResult
            {
                Success = false,
                ErrorCode = "EMPTY_RESPONSE",
                ErrorMessage = "Narafin API mengembalikan respons kosong saat mengambil daftar player."
            };
        }

        try
        {
            NarafinPlayerListResponse response = JsonUtility.FromJson<NarafinPlayerListResponse>(responseText);
            List<NarafinPlayerSummary> items = response != null && response.items != null
                ? response.items
                : new List<NarafinPlayerSummary>();

            return new NarafinPlayerListResult
            {
                Success = true,
                Items = items
            };
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal membaca daftar player: " + ex.Message);
            return new NarafinPlayerListResult
            {
                Success = false,
                ErrorCode = "INVALID_RESPONSE",
                ErrorMessage = "Narafin API mengembalikan data player yang tidak valid."
            };
        }
    }

    private static NarafinRulesetDetailResult BuildRulesetDetailSuccessResult(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return new NarafinRulesetDetailResult
            {
                Success = false,
                ErrorCode = "EMPTY_RESPONSE",
                ErrorMessage = "Narafin API mengembalikan respons kosong saat mengambil detail ruleset."
            };
        }

        try
        {
            NarafinRulesetDetailResponse response = JsonUtility.FromJson<NarafinRulesetDetailResponse>(responseText);
            if (response == null)
            {
                return new NarafinRulesetDetailResult
                {
                    Success = false,
                    ErrorCode = "INVALID_RESPONSE",
                    ErrorMessage = "Narafin API mengembalikan data ruleset yang tidak valid."
                };
            }

            string resolvedRulesetVersionId = response.ruleset_version_id;
            int resolvedVersion = response.version;

            if (string.IsNullOrWhiteSpace(resolvedRulesetVersionId) && response.versions != null && response.versions.Count > 0)
            {
                resolvedRulesetVersionId = response.versions[0] != null ? response.versions[0].ruleset_version_id : string.Empty;
                resolvedVersion = response.versions[0] != null ? response.versions[0].version : 0;
            }

            if (string.IsNullOrWhiteSpace(resolvedRulesetVersionId))
            {
                return new NarafinRulesetDetailResult
                {
                    Success = false,
                    ErrorCode = "INVALID_RESPONSE",
                    ErrorMessage = "Narafin API tidak mengembalikan ruleset_version_id."
                };
            }

            return new NarafinRulesetDetailResult
            {
                Success = true,
                RulesetId = response.ruleset_id,
                RulesetVersionId = resolvedRulesetVersionId,
                Version = resolvedVersion,
                Name = response.name,
                Mode = response.mode
            };
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal membaca detail ruleset: " + ex.Message);
            return new NarafinRulesetDetailResult
            {
                Success = false,
                ErrorCode = "INVALID_RESPONSE",
                ErrorMessage = "Narafin API mengembalikan data ruleset yang tidak valid."
            };
        }
    }

    private static NarafinRulesetListResult BuildRulesetListFailureResult(string responseText, long responseCode, string requestError)
    {
        return BuildCommonListFailureResult<NarafinRulesetListResult>(responseText, responseCode, requestError, "ruleset");
    }

    private static NarafinRulesetCreateResult BuildRulesetCreateSuccessResult(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return CreateRulesetFailureResult("EMPTY_RESPONSE", "Narafin API mengembalikan respons kosong saat membuat ruleset.");
        }

        try
        {
            NarafinRulesetCreateResponse response = JsonUtility.FromJson<NarafinRulesetCreateResponse>(responseText);
            if (response == null || string.IsNullOrWhiteSpace(response.ruleset_id) || string.IsNullOrWhiteSpace(response.ruleset_version_id))
            {
                return CreateRulesetFailureResult("INVALID_RESPONSE", "Narafin API mengembalikan data ruleset yang tidak valid.");
            }

            return new NarafinRulesetCreateResult
            {
                Success = true,
                RulesetId = response.ruleset_id,
                RulesetVersionId = response.ruleset_version_id,
                Version = response.version
            };
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal membaca respons create ruleset: " + ex.Message);
            return CreateRulesetFailureResult("INVALID_RESPONSE", "Narafin API mengembalikan data ruleset yang tidak valid.");
        }
    }

    private static NarafinRulesetDetailResult BuildRulesetDetailFailureResult(string responseText, long responseCode, string requestError)
    {
        string normalizedRequestError = requestError ?? string.Empty;
        string errorCode = "HTTP_" + responseCode;
        string errorMessage = "Narafin API gagal saat mengambil detail ruleset.";

        if (IsTimeout(responseCode, normalizedRequestError))
        {
            return new NarafinRulesetDetailResult
            {
                Success = false,
                ErrorCode = RequestTimeoutCode,
                ErrorMessage = "Connection Timeout."
            };
        }

        if (responseCode == 401 || responseCode == 403)
        {
            return new NarafinRulesetDetailResult
            {
                Success = false,
                ErrorCode = AuthForbiddenCode,
                ErrorMessage = "Akses tidak diizinkan."
            };
        }

        if (responseCode >= 500)
        {
            return new NarafinRulesetDetailResult
            {
                Success = false,
                ErrorCode = ServerDownCode,
                ErrorMessage = "Server sedang bermasalah."
            };
        }

        if (responseCode == 0)
        {
            return new NarafinRulesetDetailResult
            {
                Success = false,
                ErrorCode = ServerUnreachableCode,
                ErrorMessage = "Server tidak bisa dihubungi."
            };
        }

        if (!string.IsNullOrWhiteSpace(responseText))
        {
            try
            {
                NarafinErrorResponse errorResponse = JsonUtility.FromJson<NarafinErrorResponse>(responseText);
                if (errorResponse != null)
                {
                    if (!string.IsNullOrWhiteSpace(errorResponse.error_code))
                    {
                        errorCode = errorResponse.error_code;
                    }

                    if (!string.IsNullOrWhiteSpace(errorResponse.message))
                    {
                        errorMessage = errorResponse.message;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Gagal membaca respons error ruleset detail: " + ex.Message);
            }
        }

        if (!string.IsNullOrWhiteSpace(normalizedRequestError))
        {
            errorMessage = errorMessage + " (" + normalizedRequestError + ")";
        }

        return new NarafinRulesetDetailResult
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    private static NarafinRulesetCreateResult BuildRulesetCreateFailureResult(string responseText, long responseCode, string requestError)
    {
        string normalizedRequestError = requestError ?? string.Empty;
        string errorCode = "HTTP_" + responseCode;
        string errorMessage = "Narafin API gagal saat membuat ruleset.";

        if (IsTimeout(responseCode, normalizedRequestError))
        {
            return CreateRulesetFailureResult(RequestTimeoutCode, "Connection Timeout.");
        }

        if (responseCode == 401 || responseCode == 403)
        {
            return CreateRulesetFailureResult(AuthForbiddenCode, "Akses tidak diizinkan.");
        }

        if (responseCode >= 500)
        {
            return CreateRulesetFailureResult(ServerDownCode, "Server sedang bermasalah.");
        }

        if (responseCode == 0)
        {
            return CreateRulesetFailureResult(ServerUnreachableCode, "Server tidak bisa dihubungi.");
        }

        if (!string.IsNullOrWhiteSpace(responseText))
        {
            try
            {
                NarafinErrorResponse errorResponse = JsonUtility.FromJson<NarafinErrorResponse>(responseText);
                if (errorResponse != null)
                {
                    if (!string.IsNullOrWhiteSpace(errorResponse.error_code))
                    {
                        errorCode = errorResponse.error_code;
                    }

                    if (!string.IsNullOrWhiteSpace(errorResponse.message))
                    {
                        errorMessage = errorResponse.message;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Gagal membaca respons error create ruleset: " + ex.Message);
            }
        }

        if (!string.IsNullOrWhiteSpace(normalizedRequestError))
        {
            errorMessage = errorMessage + " (" + normalizedRequestError + ")";
        }

        return CreateRulesetFailureResult(errorCode, errorMessage);
    }

    private static NarafinPlayerListResult BuildPlayerListFailureResult(string responseText, long responseCode, string requestError)
    {
        return BuildCommonListFailureResult<NarafinPlayerListResult>(responseText, responseCode, requestError, "player");
    }

    private static NarafinSessionCreateResult BuildSessionCreateSuccessResult(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return CreateSessionFailureResult("EMPTY_RESPONSE", "Narafin API mengembalikan respons kosong saat membuat session.");
        }

        try
        {
            NarafinSessionCreateResponse response = JsonUtility.FromJson<NarafinSessionCreateResponse>(responseText);
            if (response == null || string.IsNullOrWhiteSpace(response.session_id))
            {
                return CreateSessionFailureResult("INVALID_RESPONSE", "Narafin API mengembalikan data session yang tidak valid.");
            }

            return new NarafinSessionCreateResult
            {
                Success = true,
                SessionId = response.session_id,
                RulesetId = response.ruleset_id,
                RulesetVersionId = response.ruleset_version_id
            };
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal membaca respons create session: " + ex.Message);
            return CreateSessionFailureResult("INVALID_RESPONSE", "Narafin API mengembalikan data session yang tidak valid.");
        }
    }

    private static NarafinSessionCreateResult BuildSessionCreateFailureResult(string responseText, long responseCode, string requestError)
    {
        string normalizedRequestError = requestError ?? string.Empty;
        string errorCode = "HTTP_" + responseCode;
        string errorMessage = "Narafin API gagal saat membuat session.";

        if (IsTimeout(responseCode, normalizedRequestError))
        {
            return CreateSessionFailureResult(RequestTimeoutCode, "Connection Timeout.");
        }

        if (responseCode == 401 || responseCode == 403)
        {
            return CreateSessionFailureResult(AuthForbiddenCode, "Akses tidak diizinkan.");
        }

        if (responseCode >= 500)
        {
            return CreateSessionFailureResult(ServerDownCode, "Server sedang bermasalah.");
        }

        if (responseCode == 0)
        {
            return CreateSessionFailureResult(ServerUnreachableCode, "Server tidak bisa dihubungi.");
        }

        if (!string.IsNullOrWhiteSpace(responseText))
        {
            try
            {
                NarafinErrorResponse errorResponse = JsonUtility.FromJson<NarafinErrorResponse>(responseText);
                if (errorResponse != null)
                {
                    if (!string.IsNullOrWhiteSpace(errorResponse.error_code))
                    {
                        errorCode = errorResponse.error_code;
                    }

                    if (!string.IsNullOrWhiteSpace(errorResponse.message))
                    {
                        errorMessage = errorResponse.message;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Gagal membaca respons error create session: " + ex.Message);
            }
        }

        if (!string.IsNullOrWhiteSpace(normalizedRequestError))
        {
            errorMessage = errorMessage + " (" + normalizedRequestError + ")";
        }

        return CreateSessionFailureResult(errorCode, errorMessage);
    }

    private static NarafinSessionOperationResult BuildSessionOperationFailureResult(string responseText, long responseCode, string requestError, string actionName)
    {
        string normalizedRequestError = requestError ?? string.Empty;
        string errorCode = "HTTP_" + responseCode;
        string errorMessage = "Narafin API gagal saat " + actionName + " session.";

        if (IsTimeout(responseCode, normalizedRequestError))
        {
            return CreateSessionOperationFailureResult(RequestTimeoutCode, "Connection Timeout.");
        }

        if (responseCode == 401 || responseCode == 403)
        {
            return CreateSessionOperationFailureResult(AuthForbiddenCode, "Akses tidak diizinkan.");
        }

        if (responseCode >= 500)
        {
            return CreateSessionOperationFailureResult(ServerDownCode, "Server sedang bermasalah.");
        }

        if (responseCode == 0)
        {
            return CreateSessionOperationFailureResult(ServerUnreachableCode, "Server tidak bisa dihubungi.");
        }

        if (!string.IsNullOrWhiteSpace(responseText))
        {
            try
            {
                NarafinErrorResponse errorResponse = JsonUtility.FromJson<NarafinErrorResponse>(responseText);
                if (errorResponse != null)
                {
                    if (!string.IsNullOrWhiteSpace(errorResponse.error_code))
                    {
                        errorCode = errorResponse.error_code;
                    }

                    if (!string.IsNullOrWhiteSpace(errorResponse.message))
                    {
                        errorMessage = errorResponse.message;
                    }

                    string detailText = errorResponse.FormatDetails();
                    if (!string.IsNullOrWhiteSpace(detailText))
                    {
                        errorMessage = errorMessage + " (" + detailText + ")";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Gagal membaca respons error session: " + ex.Message);
            }
        }

        if (!string.IsNullOrWhiteSpace(normalizedRequestError))
        {
            errorMessage = errorMessage + " (" + normalizedRequestError + ")";
        }

        return CreateSessionOperationFailureResult(errorCode, errorMessage);
    }

    private static NarafinSessionStateResult BuildSessionStateFailureResult(string responseText, long responseCode, string requestError)
    {
        NarafinSessionOperationResult failure = BuildSessionOperationFailureResult(responseText, responseCode, requestError, "membaca state");
        return CreateSessionStateFailureResult(failure.ErrorCode, failure.ErrorMessage);
    }

    private static NarafinRulesetSetupCatalogResult BuildSetupCatalogFailureResult(string responseText, long responseCode, string requestError)
    {
        NarafinSessionOperationResult failure = BuildSessionOperationFailureResult(responseText, responseCode, requestError, "membaca komponen ruleset");
        return CreateSetupCatalogFailureResult(failure.ErrorCode, failure.ErrorMessage);
    }

    private static NarafinSessionEventSequenceResult BuildSessionEventSequenceFailureResult(string responseText, long responseCode, string requestError)
    {
        NarafinSessionOperationResult failure = BuildSessionOperationFailureResult(responseText, responseCode, requestError, "membaca event session");
        return CreateSessionEventSequenceFailureResult(failure.ErrorCode, failure.ErrorMessage);
    }

    private static TResult BuildCommonListFailureResult<TResult>(string responseText, long responseCode, string requestError, string actionName)
        where TResult : class
    {
        string normalizedRequestError = requestError ?? string.Empty;

        if (IsTimeout(responseCode, normalizedRequestError))
        {
            return CreateTypedFailureResult<TResult>(RequestTimeoutCode, "Connection Timeout.");
        }

        if (responseCode == 401 || responseCode == 403)
        {
            return CreateTypedFailureResult<TResult>(InvalidCredentialsCode, "Akses tidak diizinkan.");
        }

        if (responseCode >= 500)
        {
            return CreateTypedFailureResult<TResult>(ServerDownCode, "Server sedang bermasalah.");
        }

        if (responseCode == 0)
        {
            return CreateTypedFailureResult<TResult>(ServerUnreachableCode, "Server tidak bisa dihubungi.");
        }

        if (!string.IsNullOrWhiteSpace(responseText))
        {
            try
            {
                NarafinErrorResponse errorResponse = JsonUtility.FromJson<NarafinErrorResponse>(responseText);
                if (errorResponse != null && !string.IsNullOrWhiteSpace(errorResponse.message))
                {
                    return CreateTypedFailureResult<TResult>(errorResponse.error_code, errorResponse.message);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Gagal membaca respons error Narafin API saat mengambil " + actionName + ": " + ex.Message);
            }
        }

        return CreateTypedFailureResult<TResult>("HTTP_" + responseCode, "Narafin API gagal saat mengambil " + actionName + ".");
    }

    private static TResult CreateTypedFailureResult<TResult>(string errorCode, string errorMessage)
        where TResult : class
    {
        object result;

        if (typeof(TResult) == typeof(NarafinRulesetListResult))
        {
            result = new NarafinRulesetListResult
            {
                Success = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }
        else if (typeof(TResult) == typeof(NarafinPlayerListResult))
        {
            result = new NarafinPlayerListResult
            {
                Success = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }
        else
        {
            result = null;
        }

        return result as TResult;
    }

    private static NarafinSessionCreateResult CreateSessionFailureResult(string errorCode, string errorMessage)
    {
        return new NarafinSessionCreateResult
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    private static NarafinRulesetCreateResult CreateRulesetFailureResult(string errorCode, string errorMessage)
    {
        return new NarafinRulesetCreateResult
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    private static NarafinSessionOperationResult CreateSessionOperationFailureResult(string errorCode, string errorMessage)
    {
        return new NarafinSessionOperationResult
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    private static NarafinSessionStateResult CreateSessionStateFailureResult(string errorCode, string errorMessage)
    {
        return new NarafinSessionStateResult
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            Players = new List<NarafinSessionStatePlayer>()
        };
    }

    private static NarafinRulesetSetupCatalogResult CreateSetupCatalogFailureResult(string errorCode, string errorMessage)
    {
        return new NarafinRulesetSetupCatalogResult
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    private static NarafinSessionEventSequenceResult CreateSessionEventSequenceFailureResult(string errorCode, string errorMessage)
    {
        return new NarafinSessionEventSequenceResult
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    private static bool IsTimeout(long responseCode, string requestError)
    {
        if (responseCode == 408)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(requestError) &&
               requestError.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static NarafinAuthResult CreateFailureResult(string errorCode, string errorMessage)
    {
        return new NarafinAuthResult
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    private static NarafinAuthResult CreateOfflineAuthSuccess(string username, string role)
    {
        string cleanUsername = string.IsNullOrWhiteSpace(username) ? "offline_user" : username.Trim();
        string cleanRole = string.IsNullOrWhiteSpace(role) ? "INSTRUCTOR" : role.Trim().ToUpperInvariant();

        return new NarafinAuthResult
        {
            Success = true,
            Session = new NarafinAuthSession
            {
                user_id = "offline_" + cleanUsername,
                username = cleanUsername,
                role = cleanRole,
                display_name = cleanUsername,
                access_token = "offline_token_" + cleanUsername,
                expires_at = string.Empty
            }
        };
    }

    private static List<NarafinRulesetSummary> CloneOfflineRulesets()
    {
        lock (OfflineDataLock)
        {
            return new List<NarafinRulesetSummary>(OfflineRulesets);
        }
    }

    private static List<NarafinPlayerSummary> CloneOfflinePlayers()
    {
        lock (OfflineDataLock)
        {
            return new List<NarafinPlayerSummary>(OfflinePlayers);
        }
    }

    private static void AddOfflinePlayer(string username)
    {
        string cleanUsername = string.IsNullOrWhiteSpace(username) ? "offline_player" : username.Trim();

        lock (OfflineDataLock)
        {
            string generatedId = "offline_player_" + cleanUsername;
            for (int i = 0; i < OfflinePlayers.Count; i++)
            {
                NarafinPlayerSummary existing = OfflinePlayers[i];
                if (existing == null)
                {
                    continue;
                }

                if (string.Equals(existing.username, cleanUsername, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            OfflinePlayers.Add(new NarafinPlayerSummary
            {
                user_id = generatedId,
                display_name = cleanUsername,
                username = cleanUsername,
                role = "PLAYER"
            });
        }
    }
}
