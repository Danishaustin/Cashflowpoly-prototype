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
    private bool isSendingAkhirGiliran;

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
        return await SendEventJsonNowAsync(eventJson, actionType);
    }

    // Event sistem (mis. BukaHargaEmas) dikirim langsung dan menunggu respons server.
    private async Task<NarafinSessionOperationResult> SendSystemEventNowAsync(string actionType, string payloadJson)
    {
        if (LoginManager.Instance == null)
        {
            return CreateEventFailure("LOGIN_NOT_READY", "Sistem login belum siap.");
        }

        if (string.IsNullOrWhiteSpace(NarafinPlayerPrefs.ApiSessionId))
        {
            return CreateEventFailure("SESSION_MISSING", "Session belum tersedia.");
        }

        string eventJson = BuildEventJson("SYSTEM", actionType, payloadJson, 0, 0, string.Empty);
        return await SendEventJsonNowAsync(eventJson, actionType);
    }

    // Menunggu antrean event lain selesai agar sequence_number tetap berurutan, lalu mengirim event dan menunggu respons.
    private static async Task<NarafinSessionOperationResult> SendEventJsonNowAsync(string eventJson, string actionType)
    {
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
                result.EventId = ExtractEventId(sequencedJson);
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

    // Donasi Jumat dikirim di slot 0 dan wajib tepat satu kali per pemain sesuai urutan.
    private static string BuildJumatBerkahPayload(int amount)
    {
        return "{\"amount\":" + amount.ToString(CultureInfo.InvariantCulture) + "}";
    }

    // Harga emas Sabtu wajib dibuka sistem dengan nilai dari Kartu Harga Emas ruleset.
    private static string BuildBukaHargaEmasPayload(int goldPrice)
    {
        return "{\"gold_price\":" + goldPrice.ToString(CultureInfo.InvariantCulture) + "}";
    }

    // unit_price wajib sama dengan harga yang dibuka hari itu dan amount = unit_price x qty;
    // di luar hari Sabtu transaksi wajib merujuk kartu risiko GOLD_TRADE lewat risk_event_id.
    private static string BuildGoldTradePayload(string tradeType, int unitPrice, int qty, string riskEventId = null)
    {
        return "{"
            + JsonStringField("trade_type", tradeType)
            + ",\"unit_price\":" + unitPrice.ToString(CultureInfo.InvariantCulture)
            + ",\"qty\":" + qty.ToString(CultureInfo.InvariantCulture)
            + ",\"amount\":" + (unitPrice * qty).ToString(CultureInfo.InvariantCulture)
            + (string.IsNullOrEmpty(riskEventId) ? string.Empty : "," + JsonStringField("risk_event_id", riskEventId))
            + "}";
    }

    // Satu card_id per kartu bahan yang dipakai; income wajib lebih dari 0.
    private static string BuildJualMasakanPayload(string orderCardId, IEnumerable<string> ingredientCardIds, int income)
    {
        return "{"
            + JsonStringField("order_card_id", orderCardId)
            + ",\"required_ingredient_card_ids\":" + BuildStringArrayJson(ingredientCardIds)
            + ",\"income\":" + income.ToString(CultureInfo.InvariantCulture)
            + "}";
    }

    // Efek risiko diambil server dari katalog; klien hanya menyebut kartu dan event JualMasakan sumbernya.
    private static string BuildRisikoKehidupanPayload(string riskCode, string sourceOrderEventId)
    {
        return "{"
            + JsonStringField("risk_id", riskCode)
            + "," + JsonStringField("source_order_event_id", sourceOrderEventId)
            + "," + JsonStringField("note", "Risiko Kehidupan")
            + "}";
    }

    private static string BuildRiskEventPayload(string riskEventId)
    {
        return "{" + JsonStringField("risk_event_id", riskEventId) + "}";
    }

    // Server menghitung direction/amount opsi darurat; extraFieldsJson berisi detail opsi tanpa kurung kurawal.
    private static string BuildOpsiDaruratPayload(string riskEventId, string optionType, string extraFieldsJson)
    {
        return "{"
            + JsonStringField("risk_event_id", riskEventId)
            + "," + JsonStringField("option_type", optionType)
            + (string.IsNullOrEmpty(extraFieldsJson) ? string.Empty : "," + extraFieldsJson)
            + "}";
    }

    private static string ExtractEventId(string eventJson)
    {
        const string marker = "\"event_id\":\"";
        int start = string.IsNullOrEmpty(eventJson) ? -1 : eventJson.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }

        start += marker.Length;
        int end = eventJson.IndexOf('"', start);
        return end > start ? eventJson.Substring(start, end - start) : string.Empty;
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

    private static string BuildKerjaLepasPayload(int income)
    {
        return "{\"amount\":" + income.ToString(CultureInfo.InvariantCulture) + "}";
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

    // Harga dan poin wajib sama dengan katalog; need_tier wajib agar kartu diterima server.
    private static string BuildKebutuhanPayload(NarafinSetupNeed need)
    {
        return "{"
            + JsonStringField("card_id", need.id)
            + ",\"amount\":" + need.hargaBeli.ToString(CultureInfo.InvariantCulture)
            + ",\"points\":" + need.poinKebahagiaan.ToString(CultureInfo.InvariantCulture)
            + "," + JsonStringField("need_tier", NarafinActiveSession.GetNeedTierCode(need.tipe))
            + "}";
    }

    private static string BuildMenabungPayload(string goalId, int amount)
    {
        return "{"
            + JsonStringField("goal_id", goalId)
            + ",\"amount\":" + amount.ToString(CultureInfo.InvariantCulture)
            + "}";
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

    // Detail pinjaman wajib sama dengan katalog ruleset; loan_id unik per kartu dibuat oleh klien.
    private static string BuildPinjamanSyariahPayload(string loanId, NarafinSetupLoan loan)
    {
        return "{"
            + JsonStringField("loan_id", loanId)
            + "," + JsonStringField("loan_code", loan.loan_code)
            + ",\"principal\":" + loan.principal.ToString(CultureInfo.InvariantCulture)
            + ",\"repayment_amount\":" + loan.repayment_amount.ToString(CultureInfo.InvariantCulture)
            + ",\"duration_days\":" + loan.duration_days.ToString(CultureInfo.InvariantCulture)
            + ",\"penalty_points\":" + loan.penalty_points.ToString(CultureInfo.InvariantCulture)
            + "}";
    }

    private static string BuildBayarPinjamanPayload(string loanId, int amount)
    {
        return "{"
            + JsonStringField("loan_id", loanId)
            + ",\"amount\":" + amount.ToString(CultureInfo.InvariantCulture)
            + "}";
    }

    // premium wajib sama dengan katalog; policy_id unik per polis dibuat oleh klien.
    private static string BuildAsuransiPayload(string policyId, string productCode, int premium, string coverageType)
    {
        return "{"
            + JsonStringField("policy_id", policyId)
            + "," + JsonStringField("product_code", productCode)
            + ",\"premium\":" + premium.ToString(CultureInfo.InvariantCulture)
            + "," + JsonStringField("coverage_type", coverageType)
            + "}";
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

    // Akhir hari ditunggu agar kegagalannya terlihat pemain; server menghitung sendiri pemakaian slot
    // dari riwayat aksi, jadi payload hanya berisi catatan.
    private async Task PostAkhirGiliranForDayEndAsync()
    {
        if (isSendingAkhirGiliran)
        {
            return;
        }

        string note = "Akhir giliran hari " + (GameState.Instance != null ? GameState.Instance.day : 0);

        NarafinSessionOperationResult result;
        isSendingAkhirGiliran = true;
        try
        {
            result = await SendSystemEventNowAsync("AkhirGiliran", "{" + JsonStringField("note", note) + "}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal mengirim akhir giliran: " + ex.Message);
            result = CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
        }
        finally
        {
            isSendingAkhirGiliran = false;
        }

        if (this == null || result.Success)
        {
            return;
        }

        view.AddSystemTextToDialog("Hari gagal ditutup di server: " + result.ErrorMessage);
    }

    private Task PostAkhirGiliranIfDayWillAdvanceAsync(int currentTurn)
    {
        if (GameState.Instance == null ||
            GameState.Instance.movesLeft > 1 ||
            !GameState.Instance.IsLastPlayerInTurnOrder(currentTurn))
        {
            return Task.CompletedTask;
        }

        return PostAkhirGiliranForDayEndAsync();
    }

    private Task PostAkhirGiliranForDayEndIfLastPlayerAsync(int currentTurn)
    {
        if (GameState.Instance == null || !GameState.Instance.IsLastPlayerInTurnOrder(currentTurn))
        {
            return Task.CompletedTask;
        }

        return PostAkhirGiliranForDayEndAsync();
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
