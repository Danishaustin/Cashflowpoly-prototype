using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    private static readonly Queue<NarafinQueuedEvent> PendingNarafinEvents = new Queue<NarafinQueuedEvent>();
    private static bool isSendingNarafinEvents;

    private static string BuildBahanMasakanPayload(string cardId, string ingredientName, int amount)
    {
        return "{"
            + JsonStringField("card_id", cardId)
            + "," + JsonStringField("ingredient_name", ingredientName)
            + ",\"amount\":" + amount.ToString(CultureInfo.InvariantCulture)
            + "}";
    }

    // Mengirim event pemain langsung dan menunggu respons server, setelah antrean event lain selesai
    // agar sequence_number tetap berurutan. Pemanggil baru mengubah state lokal bila hasilnya sukses.
    private async Task<NarafinSessionOperationResult> SendPlayerEventNowAsync(int player, string actionType, string payloadJson, int actionSlot)
    {
        string userId = NarafinPlayerPrefs.GetApiPlayerUserId(player);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return CreateEventFailure("PLAYER_NOT_FOUND", "user_id player " + player + " tidak tersedia.");
        }

        if (LoginManager.Instance == null)
        {
            return CreateEventFailure("LOGIN_NOT_READY", "Sistem login belum siap.");
        }

        if (string.IsNullOrWhiteSpace(NarafinPlayerPrefs.ApiSessionId))
        {
            return CreateEventFailure("SESSION_MISSING", "Session belum tersedia.");
        }

        string eventJson = BuildEventJson("PLAYER", actionType, payloadJson, player, actionSlot, userId);

        while (isSendingNarafinEvents)
        {
            await Task.Yield();
        }

        isSendingNarafinEvents = true;
        try
        {
            int sequenceNumber = NarafinPlayerPrefs.GetNextEventSequenceNumber();
            string sequencedJson = eventJson.Replace("\"sequence_number\":0", "\"sequence_number\":" + sequenceNumber);
            NarafinSessionOperationResult result = await LoginManager.Instance.PostEventAsync(sequencedJson);
            if (result.Success)
            {
                NarafinPlayerPrefs.ConfirmEventSequenceNumber(sequenceNumber);
            }
            else
            {
                Debug.LogWarning("Narafin event ditolak. action_type: " + actionType + ", error: " + result.ErrorCode + " - " + result.ErrorMessage);
            }

            return result;
        }
        finally
        {
            isSendingNarafinEvents = false;
            if (PendingNarafinEvents.Count > 0)
            {
                _ = ProcessEventQueueAsync();
            }
        }
    }

    private static NarafinSessionOperationResult CreateEventFailure(string errorCode, string errorMessage)
    {
        return new NarafinSessionOperationResult
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    private void PostJualMasakanEvent(int player, string resepName, IEnumerable<string> requiredBahanNames, int income)
    {
        string payload = "{"
            + JsonStringField("order_card_id", NarafinEventCardIdResolver.ResolveResepCardId(resepName))
            + "}";
        PostPlayerEvent(player, "JualMasakan", payload);
    }

    private void PostJumatBerkahEvent(int player, int amount)
    {
        string payload = "{\"amount\":" + amount.ToString(CultureInfo.InvariantCulture) + "}";
        PostPlayerEvent(player, "JumatBerkah", payload, 0);
    }

    private void PostInvestasiEmasEvent(int player, string tradeType, int qty, int unitPrice, int amount)
    {
        string payload = "{"
            + JsonStringField("trade_type", tradeType)
            + ",\"qty\":" + qty.ToString(CultureInfo.InvariantCulture)
            + ",\"unit_price\":" + unitPrice.ToString(CultureInfo.InvariantCulture)
            + ",\"amount\":" + amount.ToString(CultureInfo.InvariantCulture)
            + "}";
        PostPlayerEvent(player, "InvestasiEmas", payload, 0);
    }

    private void PostKerjaLepasEvent(int player, int income)
    {
        string payload = "{\"amount\":" + income.ToString(CultureInfo.InvariantCulture) + "}";
        PostPlayerEvent(player, "KerjaLepas", payload);
    }

    private void PostKebutuhanEvent(int player, string kebutuhanName, int amount, int happiness)
    {
        string payload = "{"
            + JsonStringField("card_id", NarafinEventCardIdResolver.ResolveKebutuhanCardId(kebutuhanName))
            + ",\"amount\":" + amount.ToString(CultureInfo.InvariantCulture)
            + ",\"happiness\":" + happiness.ToString(CultureInfo.InvariantCulture)
            + "}";
        PostPlayerEvent(player, "Kebutuhan", payload);
    }

    private void PostMenabungEvent(int player, int amount)
    {
        string payload = "{\"amount\":" + amount.ToString(CultureInfo.InvariantCulture) + "}";
        PostPlayerEvent(player, "Menabung", payload);
    }

    private void PostTujuanFinansialEvent(int player, string tujuanName, int cost, int happiness)
    {
        string payload = "{"
            + JsonStringField("goal_id", NarafinEventCardIdResolver.ResolveTujuanFinansialCardId(tujuanName))
            + ",\"amount\":" + cost.ToString(CultureInfo.InvariantCulture)
            + ",\"happiness\":" + happiness.ToString(CultureInfo.InvariantCulture)
            + "}";
        PostPlayerEvent(player, "TujuanFinansial", payload);
    }

    private void PostPinjamanSyariahEvent(int player, string loanAction, int amount)
    {
        if (loanAction == "RETURN")
        {
            string returnPayload = "{"
                + JsonStringField("loan_id", "LOAN-001")
                + ",\"amount\":" + amount.ToString(CultureInfo.InvariantCulture)
                + "}";
            PostPlayerEvent(player, "BayarPinjaman", returnPayload);
            return;
        }

        string payload = "{"
            + JsonStringField("loan_code", "LOAN-001")
            + "," + JsonStringField("loan_action", loanAction)
            + ",\"amount\":" + amount.ToString(CultureInfo.InvariantCulture)
            + "}";
        PostPlayerEvent(player, "PinjamanSyariah", payload);
    }

    private void PostAsuransiEvent(int player, int premium)
    {
        string payload = "{"
            + JsonStringField("product_code", "INS-001")
            + ",\"amount\":" + premium.ToString(CultureInfo.InvariantCulture)
            + "}";
        PostPlayerEvent(player, "Asuransi", payload);
    }

    private void PostRisikoKehidupanEvent(int player, bool investasiEmasSelected, bool bayarBankSelected, bool dapatCoinSelected, bool perubahanHargaSelected, int bankAmount, int rewardAmount, int priceDelta)
    {
        string payload = "{"
            + "\"investasi_emas\":" + BoolJson(investasiEmasSelected)
            + ",\"bayar_bank\":" + BoolJson(bayarBankSelected)
            + ",\"dapat_coin\":" + BoolJson(dapatCoinSelected)
            + ",\"perubahan_harga\":" + BoolJson(perubahanHargaSelected)
            + ",\"bank_amount\":" + bankAmount.ToString(CultureInfo.InvariantCulture)
            + ",\"reward_amount\":" + rewardAmount.ToString(CultureInfo.InvariantCulture)
            + ",\"price_delta\":" + priceDelta.ToString(CultureInfo.InvariantCulture)
            + "}";
        PostPlayerEvent(player, "RisikoKehidupan", payload);
    }

    private void PostAkhirGiliranEvent(int usedActions, int remainingActions)
    {
        string payload = "{"
            + "\"used\":" + usedActions.ToString(CultureInfo.InvariantCulture)
            + ",\"remaining\":" + remainingActions.ToString(CultureInfo.InvariantCulture)
            + "}";
        PostSystemEvent("AkhirGiliran", payload, 0, 0);
    }

    private void PostAkhirGiliranIfDayWillAdvance(int currentTurn)
    {
        if (GameState.Instance == null ||
            GameState.Instance.movesLeft > 1 ||
            !GameState.Instance.IsLastPlayerInTurnOrder(currentTurn))
        {
            return;
        }

        PostAkhirGiliranForDayEnd();
    }

    private void PostAkhirGiliranForDayEndIfLastPlayer(int currentTurn)
    {
        if (GameState.Instance == null || !GameState.Instance.IsLastPlayerInTurnOrder(currentTurn))
        {
            return;
        }

        PostAkhirGiliranForDayEnd();
    }

    private void PostAkhirGiliranForDayEnd()
    {
        int usedActions = Mathf.Max(0, GameState.Instance.playerCount) * Mathf.Max(1, GameState.Instance.ActionsPerTurn);
        PostAkhirGiliranEvent(usedActions, 0);
    }

    private void PostPlayerEvent(int player, string actionType, string payloadJson)
    {
        PostPlayerEvent(player, actionType, payloadJson, GetCurrentActionSlot());
    }

    private void PostPlayerEvent(int player, string actionType, string payloadJson, int actionSlot)
    {
        string userId = NarafinPlayerPrefs.GetApiPlayerUserId(player);
        if (string.IsNullOrWhiteSpace(userId))
        {
            Debug.LogWarning("Narafin event dilewati karena user_id player " + player + " tidak tersedia. action_type: " + actionType);
            return;
        }

        string eventJson = BuildEventJson("PLAYER", actionType, payloadJson, player, actionSlot, userId);
        EnqueueEvent(eventJson, actionType);
    }

    private void PostSystemEvent(string actionType, string payloadJson, int turnNumber, int actionSlot)
    {
        string eventJson = BuildEventJson("SYSTEM", actionType, payloadJson, turnNumber, actionSlot, string.Empty);
        EnqueueEvent(eventJson, actionType);
    }

    private string BuildEventJson(string actorType, string actionType, string payloadJson, int turnNumber, int actionSlot, string userId)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("{");
        AppendStringField(builder, "event_id", Guid.NewGuid().ToString(), false);
        AppendStringField(builder, "session_id", NarafinPlayerPrefs.ApiSessionId, true);
        if (!string.Equals(actorType, "SYSTEM", StringComparison.OrdinalIgnoreCase))
        {
            AppendStringField(builder, "user_id", userId, true);
        }

        AppendStringField(builder, "actor_type", actorType, true);
        AppendStringField(builder, "timestamp", DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture), true);
        AppendNumberField(builder, "day_index", GameState.Instance != null ? GameState.Instance.day : 0);
        AppendStringField(builder, "weekday", GetWeekdayCode(GameState.Instance != null ? GameState.Instance.day : 1), true);
        AppendNumberField(builder, "turn_number", turnNumber);
        AppendNumberField(builder, "action_slot", actionSlot);
        AppendNumberField(builder, "sequence_number", 0);
        AppendStringField(builder, "action_type", actionType, true);
        AppendStringField(builder, "ruleset_version_id", NarafinPlayerPrefs.ApiRulesetVersionId, true);
        builder.Append(",\"payload\":");
        builder.Append(string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson);
        builder.Append("}");
        return builder.ToString();
    }

    private void EnqueueEvent(string eventJson, string actionType)
    {
        if (LoginManager.Instance == null)
        {
            Debug.LogWarning("Narafin event dilewati karena LoginManager tidak tersedia. action_type: " + actionType);
            return;
        }

        if (string.IsNullOrWhiteSpace(NarafinPlayerPrefs.ApiSessionId))
        {
            Debug.LogWarning("Narafin event dilewati karena session_id tidak tersedia. action_type: " + actionType);
            return;
        }

        PendingNarafinEvents.Enqueue(new NarafinQueuedEvent
        {
            Json = eventJson,
            ActionType = actionType
        });

        if (!isSendingNarafinEvents)
        {
            _ = ProcessEventQueueAsync();
        }
    }

    private static async Task ProcessEventQueueAsync()
    {
        isSendingNarafinEvents = true;

        while (PendingNarafinEvents.Count > 0)
        {
            NarafinQueuedEvent queuedEvent = PendingNarafinEvents.Dequeue();

            if (LoginManager.Instance == null)
            {
                Debug.LogWarning("Narafin event dilewati karena LoginManager tidak tersedia. action_type: " + queuedEvent.ActionType);
                continue;
            }

            int sequenceNumber = NarafinPlayerPrefs.GetNextEventSequenceNumber();
            string eventJson = queuedEvent.Json.Replace("\"sequence_number\":0", "\"sequence_number\":" + sequenceNumber);
            NarafinSessionOperationResult result = await LoginManager.Instance.PostEventAsync(eventJson);
            if (!result.Success)
            {
                Debug.LogWarning("Narafin post event gagal. action_type: " + queuedEvent.ActionType + ", error: " + result.ErrorCode + " - " + result.ErrorMessage);
                continue;
            }

            NarafinPlayerPrefs.ConfirmEventSequenceNumber(sequenceNumber);
        }

        isSendingNarafinEvents = false;
    }

    private int GetCurrentActionSlot()
    {
        if (GameState.Instance == null)
        {
            return 0;
        }

        int slot = GameState.Instance.ActionsPerTurn - GameState.Instance.movesLeft + 1;
        return Mathf.Clamp(slot, 1, Mathf.Max(1, GameState.Instance.ActionsPerTurn));
    }

    private static string GetWeekdayCode(int dayIndex)
    {
        int normalizedDay = ((Mathf.Max(1, dayIndex) - 1) % 7) + 1;
        switch (normalizedDay)
        {
            case 1:
                return "MON";
            case 2:
                return "TUE";
            case 3:
                return "WED";
            case 4:
                return "THU";
            case 5:
                return "FRI";
            case 6:
                return "SAT";
            default:
                return "SUN";
        }
    }

    private static void AppendStringField(StringBuilder builder, string name, string value, bool prependComma)
    {
        if (prependComma)
        {
            builder.Append(",");
        }

        builder.Append(JsonStringField(name, value));
    }

    private static void AppendNumberField(StringBuilder builder, string name, int value)
    {
        builder.Append(",\"").Append(name).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));
    }

    private static string JsonStringField(string name, string value)
    {
        return "\"" + EscapeJson(name) + "\":\"" + EscapeJson(value) + "\"";
    }

    private static string BuildStringArrayJson(IEnumerable<string> values)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("[");
        bool hasValue = false;
        if (values != null)
        {
            foreach (string value in values)
            {
                if (hasValue)
                {
                    builder.Append(",");
                }

                builder.Append("\"").Append(EscapeJson(value)).Append("\"");
                hasValue = true;
            }
        }

        builder.Append("]");
        return builder.ToString();
    }

    private static string BoolJson(bool value)
    {
        return value ? "true" : "false";
    }

    private static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private sealed class NarafinQueuedEvent
    {
        public string Json;
        public string ActionType;
    }
}
