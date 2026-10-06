using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    private const string RateLimitedErrorCode = "RATE_LIMITED";
    private const string StateVersionConflictErrorCode = "STATE_VERSION_CONFLICT";
    private const int MaxRateLimitRetries = 3;
    private const int RateLimitRetryDelayMs = 1000;

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
        if (NarafinActiveSession.IsSessionEnded)
        {
            return CreateEventFailure("SESSION_ENDED", "Session sudah diakhiri.");
        }

        if (NarafinActiveSession.IsSessionClosedByServer)
        {
            return CreateEventFailure("SESSION_CLOSED", NarafinActiveSession.SessionClosedMessage);
        }

        if (NarafinActiveSession.IsServerOffline)
        {
            return CreateOfflineEventSuccess();
        }

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

    // Event SYSTEM yang tetap menyebut pemain penerima, mis. pembelian kartu TujuanFinansial.
    private async Task<NarafinSessionOperationResult> SendSystemEventForPlayerNowAsync(int player, string actionType, string payloadJson)
    {
        if (NarafinActiveSession.IsSessionEnded)
        {
            return CreateEventFailure("SESSION_ENDED", "Session sudah diakhiri.");
        }

        if (NarafinActiveSession.IsSessionClosedByServer)
        {
            return CreateEventFailure("SESSION_CLOSED", NarafinActiveSession.SessionClosedMessage);
        }

        if (NarafinActiveSession.IsServerOffline)
        {
            return CreateOfflineEventSuccess();
        }

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

        string eventJson = BuildEventJson("SYSTEM", actionType, payloadJson, 0, 0, userId);
        return await SendEventJsonNowAsync(eventJson, actionType);
    }

    // Event sistem (mis. BukaHargaEmas) dikirim langsung dan menunggu respons server.
    private async Task<NarafinSessionOperationResult> SendSystemEventNowAsync(string actionType, string payloadJson)
    {
        if (NarafinActiveSession.IsSessionEnded)
        {
            return CreateEventFailure("SESSION_ENDED", "Session sudah diakhiri.");
        }

        if (NarafinActiveSession.IsSessionClosedByServer)
        {
            return CreateEventFailure("SESSION_CLOSED", NarafinActiveSession.SessionClosedMessage);
        }

        if (NarafinActiveSession.IsServerOffline)
        {
            return CreateOfflineEventSuccess();
        }

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
        // Sinkronisasi setelah aplikasi dibuka kembali menahan antrean sampai heartbeat dan state selesai.
        while (isSendingNarafinEvents || NarafinActiveSession.IsEventSendingHeld)
        {
            await Task.Yield();
        }

        if (NarafinActiveSession.IsSessionClosedByServer)
        {
            return CreateEventFailure("SESSION_CLOSED", NarafinActiveSession.SessionClosedMessage);
        }

        if (NarafinActiveSession.IsServerOffline)
        {
            return CreateOfflineEventSuccess();
        }

        isSendingNarafinEvents = true;
        try
        {
            NarafinSequencedEventResult attempt = await SendWithSequenceAsync(eventJson, actionType);
            NarafinSessionOperationResult result = attempt.Result;
            if (result.Success)
            {
                result.EventId = ExtractEventId(attempt.SequencedJson);
            }
            else if (IsServerConnectionFailure(result))
            {
                // Error koneksi yang tetap gagal setelah nomor urut disegarkan: sisa sesi dijalankan tanpa server.
                NarafinActiveSession.SwitchToServerOffline(actionType + " - " + result.ErrorCode + " - " + result.ErrorMessage);
                return new NarafinSessionOperationResult
                {
                    Success = true,
                    EventId = ExtractEventId(attempt.SequencedJson)
                };
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

    private struct NarafinSequencedEventResult
    {
        public NarafinSessionOperationResult Result;
        public string SequencedJson;
    }

    // Nomor urut diambil dari cache lokal yang bersumber dari server. Saat server menolak nomor urut,
    // nomor diambil ulang dari /state lalu event dikirim sekali lagi. 429 ditunggu dengan jeda bertahap.
    private static async Task<NarafinSequencedEventResult> SendWithSequenceAsync(string eventJson, string actionType)
    {
        bool hasRefreshedSequence = false;
        int rateLimitRetryCount = 0;

        while (true)
        {
            int sequenceNumber = NarafinPlayerPrefs.GetNextEventSequenceNumber();
            string sequencedJson = eventJson.Replace("\"sequence_number\":0", "\"sequence_number\":" + sequenceNumber);
            NarafinSessionOperationResult result = await LoginManager.Instance.PostEventAsync(sequencedJson);

            if (result.Success)
            {
                NarafinPlayerPrefs.ConfirmEventSequenceNumber(sequenceNumber);
                return new NarafinSequencedEventResult { Result = result, SequencedJson = sequencedJson };
            }

            if (result.ErrorCode == RateLimitedErrorCode && rateLimitRetryCount < MaxRateLimitRetries)
            {
                rateLimitRetryCount++;
                await Task.Delay(RateLimitRetryDelayMs * (1 << (rateLimitRetryCount - 1)));
                continue;
            }

            if (IsSequenceMismatch(result) && !hasRefreshedSequence)
            {
                hasRefreshedSequence = true;
                Debug.LogWarning("Nomor urut event ditolak server, mengambil ulang dari /state. action_type: " + actionType);
                if (await LoginManager.Instance.RefreshEventSequenceFromServerAsync())
                {
                    continue;
                }
            }

            // Konflik versi state hanya perlu penyegaran; pengiriman tidak diulang otomatis.
            if (result.ErrorCode == StateVersionConflictErrorCode)
            {
                await LoginManager.Instance.RefreshEventSequenceFromServerAsync();
            }

            return new NarafinSequencedEventResult { Result = result, SequencedJson = sequencedJson };
        }
    }

    // Nomor urut event, bukan urutan action_slot yang juga memakai kata OUT_OF_SEQUENCE.
    private static bool IsSequenceMismatch(NarafinSessionOperationResult result)
    {
        if (result == null || result.Success)
        {
            return false;
        }

        string text = (result.ErrorCode ?? string.Empty) + " " + (result.ErrorMessage ?? string.Empty);
        return ContainsIgnoreCase(text, "sequence") && !ContainsIgnoreCase(text, "action_slot");
    }

    private static NarafinSessionOperationResult CreateOfflineEventSuccess()
    {
        return new NarafinSessionOperationResult
        {
            Success = true,
            EventId = Guid.NewGuid().ToString()
        };
    }

    // Hanya kegagalan koneksi dan nomor urut yang memindahkan sesi ke mode offline;
    // pelanggaran aturan permainan (mis. saldo tidak cukup) tetap ditolak seperti biasa.
    private static bool IsServerConnectionFailure(NarafinSessionOperationResult result)
    {
        if (result == null || result.Success)
        {
            return false;
        }

        switch (result.ErrorCode)
        {
            case "REQUEST_TIMEOUT":
            case "SERVER_DOWN":
            case "SERVER_UNREACHABLE":
            case "NETWORK_OFFLINE":
                return true;
        }

        // Nomor urut yang tetap ditolak setelah disegarkan dari server dianggap sudah tidak bisa dipulihkan.
        return IsSequenceMismatch(result);
    }

    private static bool ContainsIgnoreCase(string text, string value)
    {
        return !string.IsNullOrEmpty(text) && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
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

    private static string BuildKerjaLepasPayload(int income)
    {
        return "{\"amount\":" + income.ToString(CultureInfo.InvariantCulture) + "}";
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

    // Menabung hanya menambah satu saldo tabungan pemain; goal_id opsional dan tidak memesan kartu.
    // Juara donasi diumumkan klien: server tidak menghitung peringkat sendiri, dan poin wajib sama dengan katalog.
    private static string BuildUmumkanJuaraDonasiPayload(IReadOnlyList<int> ranking)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("{\"winners\":[");

        int winnerCount = Mathf.Min(3, ranking.Count);
        for (int i = 0; i < winnerCount; i++)
        {
            int player = ranking[i];
            int rank = i + 1;
            if (i > 0)
            {
                builder.Append(",");
            }

            builder.Append("{");
            builder.Append(JsonStringField("player_name", GetJuaraDonasiPlayerName(player)));
            builder.Append(",\"rank\":").Append(rank.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"points\":").Append(NarafinActiveSession.GetDonationRankPoints(NarafinActiveSession.Catalog, rank).ToString(CultureInfo.InvariantCulture));
            builder.Append("}");
        }

        builder.Append("]}");
        return builder.ToString();
    }

    // Poin juara donasi dicatat per pemain sebagai event SYSTEM; server yang menambahkan kebahagiaannya.
    private static string BuildPoinPeringkatDonasiPayload(int rank, int points)
    {
        return "{\"rank\":" + rank.ToString(CultureInfo.InvariantCulture)
            + ",\"points\":" + points.ToString(CultureInfo.InvariantCulture)
            + "}";
    }

    // Nama yang dikirim harus nama pemain di server; nama lokal hanya dipakai bila server tidak punya datanya.
    private static string GetJuaraDonasiPlayerName(int player)
    {
        string serverName = NarafinActiveSession.GetServerPlayerName(player);
        return string.IsNullOrWhiteSpace(serverName)
            ? PlayerPrefs.GetString("PlayerName_" + player, "Player " + player)
            : serverName;
    }

    private static string BuildMenabungPayload(int amount)
    {
        return "{\"amount\":" + amount.ToString(CultureInfo.InvariantCulture) + "}";
    }

    // Pembelian kartu tujuan dicatat sebagai event SYSTEM; server memeriksa cost dan points terhadap katalog.
    private static string BuildTujuanFinansialPayload(NarafinSetupFinancialGoal goal)
    {
        return "{"
            + JsonStringField("goal_id", goal.id)
            + ",\"cost\":" + goal.hargaBeli.ToString(CultureInfo.InvariantCulture)
            + ",\"points\":" + goal.poinKebahagiaan.ToString(CultureInfo.InvariantCulture)
            + "}";
    }

    private void PostMenabungEvent(int player, int amount)
    {
        string payload = "{\"amount\":" + amount.ToString(CultureInfo.InvariantCulture) + "}";
        PostPlayerEvent(player, "Menabung", payload);
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

    // premium wajib sama dengan katalog. policy_id TIDAK dikirim: proyeksi C# server membaca policy_id
    // lebih dulu sebagai kode produk, jadi id komposit membuat pencarian produk gagal dan polis tidak
    // pernah dibuat walau event dijawab 201. Dengan hanya product_code, kedua mesin proyeksi server sepakat.
    private static string BuildAsuransiPayload(string productCode, int premium, string coverageType)
    {
        return "{"
            + JsonStringField("product_code", productCode)
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

        // Hari terakhir tidak ditutup dengan AkhirGiliran: server menolaknya dan meminta finalisasi
        // lewat POST /end, yang memang dipanggil klien saat permainan berakhir.
        if (GameState.Instance != null && GameState.Instance.day >= GameState.Instance.finishDay)
        {
            Debug.Log("AkhirGiliran dilewati pada hari terakhir; sesi ditutup lewat POST /end.");
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

    // Hari berganti setelah aksi terakhir pemain terakhir.
    private bool WillDayAdvanceAfterAction(int currentTurn)
    {
        return GameState.Instance != null
            && GameState.Instance.movesLeft <= 1
            && GameState.Instance.IsLastPlayerInTurnOrder(currentTurn);
    }

    private Task PostAkhirGiliranIfDayWillAdvanceAsync(int currentTurn)
    {
        if (!WillDayAdvanceAfterAction(currentTurn))
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
        if (!string.Equals(actorType, "SYSTEM", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(userId))
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
        if (NarafinActiveSession.IsServerOffline)
        {
            return;
        }

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
            if (NarafinActiveSession.IsServerOffline || NarafinActiveSession.IsSessionClosedByServer || NarafinActiveSession.IsSessionEnded)
            {
                continue;
            }

            if (LoginManager.Instance == null)
            {
                Debug.LogWarning("Narafin event dilewati karena LoginManager tidak tersedia. action_type: " + queuedEvent.ActionType);
                continue;
            }

            NarafinSessionOperationResult result = (await SendWithSequenceAsync(queuedEvent.Json, queuedEvent.ActionType)).Result;
            if (!result.Success)
            {
                if (IsServerConnectionFailure(result))
                {
                    NarafinActiveSession.SwitchToServerOffline(queuedEvent.ActionType + " - " + result.ErrorCode + " - " + result.ErrorMessage);
                    continue;
                }

                Debug.LogWarning("Narafin post event gagal. action_type: " + queuedEvent.ActionType + ", error: " + result.ErrorCode + " - " + result.ErrorMessage);
            }
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
