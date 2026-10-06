using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    private const string RisikoEffectCoin = "COIN_EFFECT";
    private const string RisikoEffectAllPlayersCoin = "ALL_PLAYERS_COIN_EFFECT";
    private const string RisikoEffectTransfer = "PLAYER_TO_PLAYER_TRANSFER";
    private const string RisikoEffectIngredientPrice = "INGREDIENT_PRICE_MODIFIER";
    private const string RisikoEffectGoldTrade = "GOLD_TRADE";

    private bool risikoServerActive;
    private bool isSubmittingRisiko;
    private NarafinSetupLifeRisk risikoServerRisk;
    private string risikoServerSourceOrderEventId = string.Empty;
    private string risikoServerEventId = string.Empty;
    private int risikoServerActorPlayer;
    private int risikoServerOriginalTurn;
    private int risikoServerOriginalMovesLeft;
    private int risikoServerPayer;
    private readonly List<int> risikoServerPayerQueue = new List<int>();
    private readonly HashSet<int> risikoServerReserveOverride = new HashSet<int>();

    // Server menolak semua bentuk payload opsi darurat SELL_GOLD, jadi pilihannya disembunyikan setelah sekali ditolak.
    private bool isRisikoSellGoldUnsupported;

    // Mode MAHIR dengan katalog life_risks: kartu dipilih pemain, efeknya dihitung server dari katalog.
    private static bool UseRisikoCatalog => NarafinActiveSession.IsMahir
        && NarafinActiveSession.GetLifeRisks(NarafinActiveSession.Catalog).Count > 0;

    private void StartRisikoKehidupanServer(string sourceOrderEventId)
    {
        risikoServerActive = true;
        risikoServerRisk = null;
        // Penolakan jual emas hanya berlaku untuk kartu risiko yang sedang ditangani; tanpa reset ini satu
        // kegagalan menyembunyikan opsinya sampai sesi berakhir.
        isRisikoSellGoldUnsupported = false;
        risikoServerEventId = string.Empty;
        risikoServerSourceOrderEventId = sourceOrderEventId ?? string.Empty;
        risikoServerActorPlayer = GameState.Instance.turn;
        risikoServerOriginalTurn = GameState.Instance.turn;
        risikoServerOriginalMovesLeft = GameState.Instance.movesLeft;
        risikoServerPayer = 0;
        risikoServerPayerQueue.Clear();
        risikoServerReserveOverride.Clear();

        view.ShowRisikoKartuContent();
        view.ShowChoice("RisikoKehidupan");
    }

    // true bila pilihan ditangani alur risiko katalog; tombol jumlah jual emas tetap memakai handler lama.
    private bool HandleChoiceRisikoKehidupanServer(string selectedChoice)
    {
        if (!risikoServerActive)
        {
            return false;
        }

        if (isSubmittingRisiko)
        {
            return true;
        }

        if (selectedChoice == "NextButtonRisikoKehidupan")
        {
            // Sebelum kartu dikirim, Next berarti mengonfirmasi pilihan dropdown kartu risiko.
            if (risikoServerRisk == null)
            {
                string riskCode = view.GetSelectedRisikoKartuCode();
                if (string.IsNullOrWhiteSpace(riskCode))
                {
                    view.ShowRisikoKehidupanWarning("Pilih kartu risiko terlebih dahulu.");
                    return true;
                }

                NarafinSetupLifeRisk selectedRisk = NarafinActiveSession.FindLifeRisk(NarafinActiveSession.Catalog, riskCode);
                if (selectedRisk == null)
                {
                    view.ShowRisikoKehidupanWarning("Kartu risiko tidak ditemukan.");
                    return true;
                }

                view.HideRisikoKehidupanWarning();
                AskConfirmation(
                    BuildRisikoKartuConfirmMessage(selectedRisk),
                    () => _ = KirimRisikoKehidupanAsync(riskCode),
                    null);
                return true;
            }

            if (risikoServerPayer != 0)
            {
                ConfirmRisikoServerDecision();
            }

            return true;
        }

        return false;
    }

    private async Task KirimRisikoKehidupanAsync(string riskCode)
    {
        NarafinSetupLifeRisk risk = NarafinActiveSession.FindLifeRisk(NarafinActiveSession.Catalog, riskCode);
        if (risk == null)
        {
            view.ShowRisikoKehidupanWarning("Kartu risiko tidak ditemukan.");
            return;
        }

        NarafinSessionOperationResult result;
        isSubmittingRisiko = true;
        try
        {
            result = await SendRisikoEventAsync(
                risikoServerActorPlayer,
                "RisikoKehidupan",
                BuildRisikoKehidupanPayload(risk.risk_code, risikoServerSourceOrderEventId));
        }
        finally
        {
            isSubmittingRisiko = false;
        }

        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            view.ShowRisikoKehidupanWarning("Risiko ditolak: " + result.ErrorMessage);
            return;
        }

        risikoServerRisk = risk;
        risikoServerEventId = result.EventId;

        isSubmittingRisiko = true;
        try
        {
            await ApplyRisikoServerEffectAsync();
        }
        finally
        {
            isSubmittingRisiko = false;
        }
    }

    // Efek kartu mengikuti perilaku server yang teramati (uji 17 Sep 2026). Hanya risiko pengeluaran pribadi yang
    // menunggu pembayaran; efek koin lainnya langsung diterapkan server tanpa pembayaran per pemain, jadi koin
    // klien cukup dibaca ulang dari server agar tidak ada panel pembayaran yang selalu ditolak.
    private async Task ApplyRisikoServerEffectAsync()
    {
        if (NarafinActiveSession.IsServerOffline)
        {
            ApplyRisikoOfflineEffect();
            return;
        }

        NarafinSetupLifeRisk risk = risikoServerRisk;
        string riskName = GetRisikoDisplayName(risk);
        bool isIncome = string.Equals(risk.direction, "IN", StringComparison.OrdinalIgnoreCase);

        switch (risk.effect_type)
        {
            case RisikoEffectCoin when !isIncome:
                StartRisikoServerPayment(new List<int> { risikoServerActorPlayer });
                return;
            // Risiko massal pengeluaran diselesaikan tiap pemain sendiri dengan risk_event_id yang sama.
            case RisikoEffectAllPlayersCoin when !isIncome:
                StartRisikoServerPayment(GetRisikoPlayers(false));
                return;
            case RisikoEffectCoin:
            case RisikoEffectAllPlayersCoin:
            case RisikoEffectTransfer:
                string changes = await SyncCoinsAndDescribeChangesAsync();
                if (this == null)
                {
                    return;
                }

                FinishRisikoServer(riskName + ": " + (string.IsNullOrEmpty(changes) ? "tidak ada perubahan koin." : changes + ".") + "\n");
                return;
            case RisikoEffectIngredientPrice:
                // Server memakai harga katalog termodifikasi setelah kartu ini ditarik, jadi harga lokal ikut diubah
                // supaya pembelian bahan berikutnya memakai nilai yang sama dengan server.
                GameState.Instance.SetPerubahanHargaBahan(risk.value_delta, risk.duration_days);
                FinishRisikoServer(riskName + ": harga bahan " + (risk.value_delta >= 0 ? "+" : string.Empty) + risk.value_delta
                    + " koin selama " + risk.duration_days + " hari.\n");
                return;
            case RisikoEffectGoldTrade:
                view.HideChoiceContainer("ChoiceRisikoKehidupan");
                ShowInvestasiEmasHargaInputFromRisikoServer(risikoServerEventId);
                return;
            default:
                FinishRisikoServer(riskName + " dicatat.\n");
                return;
        }
    }

    // Mode offline: efek kartu dihitung klien sesuai katalog life_risks karena server tidak bisa dihubungi.
    private void ApplyRisikoOfflineEffect()
    {
        NarafinSetupLifeRisk risk = risikoServerRisk;
        string riskName = GetRisikoDisplayName(risk);
        bool isIncome = string.Equals(risk.direction, "IN", StringComparison.OrdinalIgnoreCase);
        int amount = Mathf.Max(0, risk.amount);

        switch (risk.effect_type)
        {
            case RisikoEffectCoin when isIncome:
                GameState.Instance.ChangeCoins(risikoServerActorPlayer, amount);
                FinishRisikoServer(riskName + ": " + GetPlayerName(risikoServerActorPlayer) + " +" + amount + " koin.\n");
                return;
            case RisikoEffectCoin:
                StartRisikoServerPayment(new List<int> { risikoServerActorPlayer });
                return;
            case RisikoEffectAllPlayersCoin when isIncome:
                foreach (int player in GetRisikoPlayers(false))
                {
                    GameState.Instance.ChangeCoins(player, amount);
                }

                FinishRisikoServer(riskName + ": semua pemain +" + amount + " koin.\n");
                return;
            case RisikoEffectAllPlayersCoin:
                StartRisikoServerPayment(GetRisikoPlayers(false));
                return;
            case RisikoEffectTransfer:
                StartRisikoServerPayment(GetRisikoPlayers(true));
                return;
            case RisikoEffectIngredientPrice:
                GameState.Instance.SetPerubahanHargaBahan(risk.value_delta, risk.duration_days);
                FinishRisikoServer(riskName + ": harga bahan " + (risk.value_delta >= 0 ? "+" : string.Empty) + risk.value_delta
                    + " koin selama " + risk.duration_days + " hari.\n");
                return;
            case RisikoEffectGoldTrade:
                view.HideChoiceContainer("ChoiceRisikoKehidupan");
                ShowInvestasiEmasHargaInputFromRisikoServer(risikoServerEventId);
                return;
            default:
                FinishRisikoServer(riskName + " dicatat.\n");
                return;
        }
    }

    // Membaca ulang koin semua pemain dari server lalu merangkum perubahannya, mis. "Marcello -12 koin".
    private async Task<string> SyncCoinsAndDescribeChangesAsync()
    {
        int playerCount = GameState.Instance.playerCount;
        Dictionary<int, int> coinsBefore = new Dictionary<int, int>();
        for (int player = 1; player <= playerCount; player++)
        {
            coinsBefore[player] = GameState.Instance.GetCoins(player);
        }

        await SyncCoinsFromServerAsync();
        if (this == null)
        {
            return string.Empty;
        }

        List<string> changes = new List<string>();
        for (int player = 1; player <= playerCount; player++)
        {
            int delta = GameState.Instance.GetCoins(player) - coinsBefore[player];
            if (delta != 0)
            {
                changes.Add(GetPlayerName(player) + (delta > 0 ? " +" : " ") + delta + " koin");
            }
        }

        return string.Join(", ", changes);
    }

    // Semua pemain berurutan; untuk transfer antarpemain, pemain aktif tidak ikut membayar.
    private List<int> GetRisikoPlayers(bool excludeActor)
    {
        List<int> players = new List<int>();
        for (int player = 1; player <= GameState.Instance.playerCount; player++)
        {
            if (!excludeActor || player != risikoServerActorPlayer)
            {
                players.Add(player);
            }
        }

        return players;
    }

    private void StartRisikoServerPayment(List<int> payers)
    {
        risikoServerPayerQueue.Clear();
        risikoServerPayerQueue.AddRange(payers);
        if (risikoServerPayerQueue.Count == 0)
        {
            FinishRisikoServer(GetRisikoDisplayName(risikoServerRisk) + " selesai.\n");
            return;
        }

        risikoServerPayer = risikoServerPayerQueue[0];
        ShowRisikoServerDecision(null);
    }

    private void ShowRisikoServerDecision(string infoText)
    {
        int player = risikoServerPayer;
        int amount = Mathf.Max(0, risikoServerRisk.amount);
        bool isTransfer = risikoServerRisk.effect_type == RisikoEffectTransfer;
        string playerName = GetPlayerName(player);

        GameState.Instance.SetTurnAndMoves(player, GameState.Instance.movesLeft);
        view.UpdatePlayerTurn(player);
        view.UpdatePlayerStats();

        // Cadangan donasi Jumat tetap dijaga; opsi darurat dibuka bila koin tidak cukup atau terkena cadangan.
        string spendBlockedMessage = GameState.Instance.GetSpendBlockedMessage(player, amount);
        bool blockedOnlyByReserve = spendBlockedMessage != null && GameState.Instance.GetCoins(player) >= amount;
        bool canPay = spendBlockedMessage == null || (blockedOnlyByReserve && risikoServerReserveOverride.Contains(player));
        bool canUseAsuransi = !isTransfer && GameState.Instance.InsuranceEnabled && GameState.Instance.GetAsuransiDimiliki(player);
        bool canJualKebutuhan = !canPay && !string.IsNullOrEmpty(GameState.Instance.GetKebutuhanUntukDijual(player));
        bool canJualEmas = !canPay && !isRisikoSellGoldUnsupported && HasHargaEmasHariIni
            && GameState.Instance.GetEmas(player) > 0 && GameState.Instance.GoldTradeAllowSell;
        bool canPinjamanSyariah = !canPay && GameState.Instance.LoanEnabled;
        // Tanpa pilihan lain, pembayaran tetap dibuka dan server yang memutuskan.
        if (!canPay && !canUseAsuransi && !canJualKebutuhan && !canJualEmas && !canPinjamanSyariah)
        {
            canPay = true;
        }

        view.ShowRisikoCoinDecisionContent(
            playerName,
            canUseAsuransi,
            canPay,
            canJualKebutuhan,
            canJualEmas,
            canPinjamanSyariah,
            GameState.Instance.GetEmas(player));
        view.SetRisikoDecisionTitle(playerName + ": " + GetRisikoDisplayName(risikoServerRisk) + " (" + amount + " koin"
            + (isTransfer ? " untuk " + GetPlayerName(risikoServerActorPlayer) : string.Empty) + ")");

        string warningText = infoText ?? (canPay ? null : spendBlockedMessage);
        if (!string.IsNullOrEmpty(warningText))
        {
            view.ShowRisikoKehidupanWarning(warningText);
        }
    }

    // Kartu risiko tidak bisa dibatalkan setelah terkirim, jadi pilihannya dikonfirmasi lebih dulu.
    private string BuildRisikoKartuConfirmMessage(NarafinSetupLifeRisk risk)
    {
        string name = GetRisikoDisplayName(risk);
        int amount = Mathf.Max(0, risk.amount);
        bool isIncome = string.Equals(risk.direction, "IN", StringComparison.OrdinalIgnoreCase);

        switch (risk.effect_type)
        {
            case RisikoEffectCoin:
                return "Tarik kartu risiko " + name + "?\n"
                    + GetPlayerName(risikoServerActorPlayer)
                    + (isIncome ? " menerima " : " membayar ") + amount + " koin.";
            case RisikoEffectAllPlayersCoin:
                return "Tarik kartu risiko " + name + "?\n"
                    + "Semua pemain " + (isIncome ? "menerima " : "membayar ") + amount + " koin.";
            case RisikoEffectTransfer:
                return "Tarik kartu risiko " + name + "?\n"
                    + "Setiap pemain lain membayar " + amount + " koin ke " + GetPlayerName(risikoServerActorPlayer) + ".";
            case RisikoEffectIngredientPrice:
                return "Tarik kartu risiko " + name + "?\nHarga bahan masakan berubah.";
            case RisikoEffectGoldTrade:
                return "Tarik kartu risiko " + name + "?\nTransaksi emas dibuka.";
            default:
                return "Tarik kartu risiko " + name + "?";
        }
    }

    // Cara bayar dikonfirmasi per pemain karena tiap pilihan langsung mengubah koin, kartu, atau pinjaman.
    private void ConfirmRisikoServerDecision()
    {
        if (!view.IsRisikoCoinDecisionInputValid(out string warningText))
        {
            view.ShowRisikoKehidupanWarning(warningText);
            return;
        }

        view.HideRisikoKehidupanWarning();
        AskConfirmation(
            BuildRisikoDecisionConfirmMessage(),
            () => _ = HandleRisikoServerDecisionNextAsync(),
            null);
    }

    private string BuildRisikoDecisionConfirmMessage()
    {
        int player = risikoServerPayer;
        string playerName = GetPlayerName(player);
        string riskName = GetRisikoDisplayName(risikoServerRisk);
        int amount = Mathf.Max(0, risikoServerRisk.amount);

        if (view.IsRisikoUseAsuransiSelected())
        {
            return playerName + " memakai asuransi untuk " + riskName + "?";
        }

        if (view.IsRisikoBayarBankSelected())
        {
            return playerName + " membayar " + amount + " koin untuk " + riskName + "?";
        }

        if (view.IsRisikoJualKebutuhanSelected())
        {
            string cardId = GameState.Instance.GetKebutuhanUntukDijual(player);
            return playerName + " menjual kebutuhan " + GameState.Instance.GetKebutuhanDisplayName(cardId)
                + " untuk membayar " + riskName + "?";
        }

        if (view.IsRisikoJualEmasSelected())
        {
            return playerName + " menjual " + view.GetRisikoJualEmasAmount() + " emas untuk membayar " + riskName + "?";
        }

        if (view.IsRisikoPinjamanSyariahSelected())
        {
            return playerName + " mengambil pinjaman syariah untuk membayar " + riskName + "?";
        }

        return "Lanjutkan cara bayar ini untuk " + riskName + "?";
    }

    private async Task HandleRisikoServerDecisionNextAsync()
    {
        if (!view.IsRisikoCoinDecisionInputValid(out string warningText))
        {
            view.ShowRisikoKehidupanWarning(warningText);
            return;
        }

        isSubmittingRisiko = true;
        try
        {
            if (view.IsRisikoUseAsuransiSelected())
            {
                await PayRisikoWithAsuransiAsync();
            }
            else if (view.IsRisikoBayarBankSelected())
            {
                await PayRisikoWithCoinsAsync();
            }
            else if (view.IsRisikoJualKebutuhanSelected())
            {
                await UseRisikoEmergencySellNeedAsync();
            }
            else if (view.IsRisikoJualEmasSelected())
            {
                await UseRisikoEmergencySellGoldAsync();
            }
            else if (view.IsRisikoPinjamanSyariahSelected())
            {
                await UseRisikoEmergencyLoanAsync();
            }
        }
        finally
        {
            isSubmittingRisiko = false;
        }
    }

    private async Task PayRisikoWithAsuransiAsync()
    {
        int player = risikoServerPayer;
        NarafinSessionOperationResult result = await SendRisikoEventAsync(player, "Asuransi", BuildRiskEventPayload(risikoServerEventId));
        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            // Bug server yang diketahui: polis baru yang dibeli setelah asuransi terpakai tetap dianggap habis.
            if (IsAsuransiPolicyInactive(result))
            {
                GameState.Instance.SetAsuransiPolicy(player, string.Empty, 0);
                ShowRisikoServerDecision("Asuransi tidak bisa dipakai lagi karena polis tidak aktif di server. Pilih cara bayar lain.");
                return;
            }

            ShowRisikoServerDecision("Asuransi ditolak: " + result.ErrorMessage);
            return;
        }

        GameState.Instance.UseAsuransiPolicy(player);
        CompleteRisikoServerPayer(GetPlayerName(player) + " memakai asuransi untuk " + GetRisikoDisplayName(risikoServerRisk) + ".");
    }

    private async Task PayRisikoWithCoinsAsync()
    {
        int player = risikoServerPayer;
        int amount = Mathf.Max(0, risikoServerRisk.amount);
        NarafinSessionOperationResult result = await SendRisikoEventAsync(player, "BayarRisiko", BuildRiskEventPayload(risikoServerEventId));
        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            // Opsi darurat bisa melunasi risiko secara otomatis di server; pembayaran berikutnya lalu ditolak.
            if (IsRisikoAlreadySettled(result))
            {
                await SyncCoinsFromServerAsync();
                if (this == null)
                {
                    return;
                }

                CompleteRisikoServerPayer(GetRisikoDisplayName(risikoServerRisk) + " sudah terbayar.");
                return;
            }

            ShowRisikoServerDecision("Pembayaran ditolak: " + result.ErrorMessage);
            return;
        }

        GameState.Instance.ChangeCoins(player, -amount);
        if (NarafinActiveSession.IsServerOffline && risikoServerRisk.effect_type == RisikoEffectTransfer)
        {
            GameState.Instance.ChangeCoins(risikoServerActorPlayer, amount);
            CompleteRisikoServerPayer(GetPlayerName(player) + " membayar " + amount + " koin ke " + GetPlayerName(risikoServerActorPlayer) + ".");
            return;
        }

        CompleteRisikoServerPayer(GetPlayerName(player) + " membayar " + amount + " koin.");
    }

    private static bool IsAsuransiPolicyInactive(NarafinSessionOperationResult result)
    {
        return result != null
            && !string.IsNullOrEmpty(result.ErrorMessage)
            && result.ErrorMessage.IndexOf("polis asuransi tidak aktif", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsRisikoAlreadySettled(NarafinSessionOperationResult result)
    {
        return result != null
            && !string.IsNullOrEmpty(result.ErrorMessage)
            && result.ErrorMessage.IndexOf("sudah diselesaikan", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // Payload SELL_NEED yang diterima server: {risk_event_id, option_type, card_id}; harga jual dihitung server.
    private async Task UseRisikoEmergencySellNeedAsync()
    {
        int player = risikoServerPayer;
        string cardId = GameState.Instance.GetKebutuhanUntukDijual(player);
        NarafinSessionOperationResult result = await SendRisikoEventAsync(
            player,
            "GunakanOpsiDarurat",
            BuildOpsiDaruratPayload(risikoServerEventId, "SELL_NEED", JsonStringField("card_id", cardId)));
        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            ShowRisikoServerEmergencyRejected(player, result);
            return;
        }

        NarafinSetupNeed soldNeed = NarafinActiveSession.FindNeed(NarafinActiveSession.Catalog, cardId);
        int offlineNeedGain = Mathf.Max(1, (soldNeed != null ? soldNeed.hargaBeli : 0) / 2);
        GameState.Instance.RemoveKebutuhan(player, cardId);
        await ContinueAfterRisikoEmergencyAsync(player, "SELL_NEED", offlineNeedGain,
            GetPlayerName(player) + " menjual " + GameState.Instance.GetKebutuhanDisplayName(cardId) + ".");
    }

    // Payload SELL_GOLD yang diterima server: {risk_event_id, option_type, qty, gold_price_event_id}.
    // gold_price_event_id wajib menunjuk event BukaHargaEmas pada hari yang sama; server menghitung
    // unit_price dan amount sendiri, jadi keduanya tidak dikirim.
    private async Task UseRisikoEmergencySellGoldAsync()
    {
        int player = risikoServerPayer;
        if (!view.IsRisikoJualEmasInputValid(GameState.Instance.GetEmas(player), out string jualEmasWarning))
        {
            view.ShowRisikoKehidupanWarning(jualEmasWarning);
            return;
        }

        int qty = view.GetRisikoJualEmasAmount();
        NarafinSessionOperationResult result = await SendRisikoEventAsync(
            player,
            "GunakanOpsiDarurat",
            BuildOpsiDaruratPayload(risikoServerEventId, "SELL_GOLD",
                "\"qty\":" + qty + "," + JsonStringField("gold_price_event_id", hargaEmasEventId)));
        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            // Penolakan di sini berarti ada syarat emas lain yang tidak terpenuhi (mis. harga emas sudah
            // kedaluwarsa oleh risiko emas baru). Opsinya disembunyikan agar pemain memilih cara lain.
            isRisikoSellGoldUnsupported = true;
            ShowRisikoServerDecision("Jual emas ditolak: " + result.ErrorMessage + " Pilih cara bayar lain.");
            return;
        }

        GameState.Instance.ChangeEmas(player, -qty);
        await ContinueAfterRisikoEmergencyAsync(player, "SELL_GOLD", qty * GetHargaEmasAcuanRisiko(), GetPlayerName(player) + " menjual " + qty + " emas.");
    }

    // Payload TAKE_SHARIA_LOAN yang diterima server: {risk_event_id, option_type, loan_code}. Server membuat loan_id
    // sendiri dan mengisi detail pinjaman dari katalog, jadi loan_id dibaca dari event agar pinjaman bisa dikembalikan.
    private async Task UseRisikoEmergencyLoanAsync()
    {
        int player = risikoServerPayer;
        NarafinSetupLoan loan = GetPinjamanSyariahProduct();
        string itemName = string.IsNullOrWhiteSpace(loan.item_name) ? "Pinjaman Syariah" : loan.item_name;

        NarafinSessionOperationResult result = await SendRisikoEventAsync(
            player,
            "GunakanOpsiDarurat",
            BuildOpsiDaruratPayload(risikoServerEventId, "TAKE_SHARIA_LOAN", JsonStringField("loan_code", loan.loan_code)));
        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            ShowRisikoServerEmergencyRejected(player, result);
            return;
        }

        await ContinueAfterRisikoEmergencyAsync(player, "TAKE_SHARIA_LOAN", loan.principal, GetPlayerName(player) + " mengambil " + itemName + ".", loan, itemName);
    }

    // Server menambah koin dari opsi darurat lalu otomatis melunasi risiko bila saldonya sudah cukup.
    private async Task ContinueAfterRisikoEmergencyAsync(int player, string optionType, int offlineGain, string resultText, NarafinSetupLoan loan = null, string loanItemName = null)
    {
        if (NarafinActiveSession.IsServerOffline)
        {
            ContinueAfterRisikoEmergencyOffline(player, offlineGain, resultText, loan, loanItemName);
            return;
        }

        int coinsBefore = GameState.Instance.GetCoins(player);
        int riskAmount = Mathf.Max(0, risikoServerRisk.amount);
        NarafinSessionEventPayload serverPayload = await FindRisikoEmergencyPayloadAsync(player, optionType);
        if (this == null)
        {
            return;
        }

        if (loan != null)
        {
            if (serverPayload == null || string.IsNullOrWhiteSpace(serverPayload.loan_id))
            {
                Debug.LogWarning("loan_id pinjaman darurat tidak ditemukan pada event server; pinjaman belum bisa dikembalikan dari klien.");
            }

            GameState.Instance.AddPinjamanSyariah(player, new PinjamanSyariahHolding
            {
                LoanId = serverPayload?.loan_id ?? string.Empty,
                LoanCode = loan.loan_code,
                ItemName = loanItemName,
                Principal = serverPayload != null && serverPayload.principal > 0 ? serverPayload.principal : loan.principal,
                RepaymentAmount = serverPayload != null && serverPayload.repayment_amount > 0 ? serverPayload.repayment_amount : loan.repayment_amount
            });
        }

        await SyncCoinsFromServerAsync();
        if (this == null)
        {
            return;
        }

        bool isSettledByServer = serverPayload != null && coinsBefore + serverPayload.amount >= riskAmount;
        if (isSettledByServer)
        {
            CompleteRisikoServerPayer(resultText + " " + GetRisikoDisplayName(risikoServerRisk) + " langsung terbayar.");
            return;
        }

        ShowRisikoServerDecision(resultText);
    }

    // Mode offline: koin opsi darurat ditambahkan klien, lalu risiko langsung dilunasi bila koinnya sudah cukup.
    private void ContinueAfterRisikoEmergencyOffline(int player, int gain, string resultText, NarafinSetupLoan loan, string loanItemName)
    {
        if (loan != null)
        {
            GameState.Instance.AddPinjamanSyariah(player, new PinjamanSyariahHolding
            {
                LoanId = loan.loan_code + ":offline:" + Guid.NewGuid().ToString("N"),
                LoanCode = loan.loan_code,
                ItemName = loanItemName,
                Principal = loan.principal,
                RepaymentAmount = loan.repayment_amount
            });
        }

        GameState.Instance.ChangeCoins(player, Mathf.Max(0, gain));
        view.UpdatePlayerStats();

        int riskAmount = Mathf.Max(0, risikoServerRisk.amount);
        if (GameState.Instance.GetCoins(player) >= riskAmount)
        {
            GameState.Instance.ChangeCoins(player, -riskAmount);
            if (risikoServerRisk.effect_type == RisikoEffectTransfer)
            {
                GameState.Instance.ChangeCoins(risikoServerActorPlayer, riskAmount);
            }

            CompleteRisikoServerPayer(resultText + " " + GetRisikoDisplayName(risikoServerRisk) + " langsung terbayar.");
            return;
        }

        ShowRisikoServerDecision(resultText);
    }

    private async Task<NarafinSessionEventPayload> FindRisikoEmergencyPayloadAsync(int player, string optionType)
    {
        if (LoginManager.Instance == null)
        {
            return null;
        }

        NarafinSessionEventSequenceResult eventsResult;
        try
        {
            eventsResult = await LoginManager.Instance.GetActiveSessionEventsAsync();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal membaca event opsi darurat: " + ex.Message);
            return null;
        }

        if (!eventsResult.Success || eventsResult.Events == null)
        {
            return null;
        }

        string userId = NarafinPlayerPrefs.GetApiPlayerUserId(player);
        NarafinSessionEventPayload latestPayload = null;
        long latestSequence = -1;
        foreach (NarafinSessionEventSummary serverEvent in eventsResult.Events)
        {
            if (serverEvent?.payload == null
                || serverEvent.action_type != "GunakanOpsiDarurat"
                || !string.Equals(serverEvent.user_id, userId, StringComparison.OrdinalIgnoreCase)
                || serverEvent.payload.risk_event_id != risikoServerEventId
                || serverEvent.payload.option_type != optionType
                || serverEvent.sequence_number <= latestSequence)
            {
                continue;
            }

            latestSequence = serverEvent.sequence_number;
            latestPayload = serverEvent.payload;
        }

        return latestPayload;
    }

    // Server tidak mengenal cadangan donasi: bila opsi darurat ditolak padahal koin cukup, pembayaran dibuka.
    private void ShowRisikoServerEmergencyRejected(int player, NarafinSessionOperationResult result)
    {
        string warningText = "Opsi darurat ditolak: " + result.ErrorMessage;
        if (GameState.Instance.GetCoins(player) >= Mathf.Max(0, risikoServerRisk.amount) && risikoServerReserveOverride.Add(player))
        {
            warningText += " Pembayaran tetap bisa dipilih.";
        }

        ShowRisikoServerDecision(warningText);
    }

    private void CompleteRisikoServerPayer(string resultText)
    {
        if (risikoServerPayerQueue.Count > 0)
        {
            risikoServerPayerQueue.RemoveAt(0);
        }

        if (risikoServerPayerQueue.Count == 0)
        {
            FinishRisikoServer(resultText + "\n");
            return;
        }

        risikoServerPayer = risikoServerPayerQueue[0];
        ShowRisikoServerDecision(resultText);
    }

    private void FinishRisikoServer(string resultText)
    {
        risikoServerActive = false;
        risikoServerRisk = null;
        risikoServerEventId = string.Empty;
        risikoServerPayer = 0;
        risikoServerPayerQueue.Clear();
        risikoServerReserveOverride.Clear();

        GameState.Instance.SetTurnAndMoves(risikoServerOriginalTurn, risikoServerOriginalMovesLeft);
        view.HideRisikoKehidupanWarning();
        view.HideChoiceContainer("ChoiceRisikoKehidupan");
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();
        PlayNarasiThen("RisikoKehidupan", resultText, UpdateMove);
    }

    private async Task<NarafinSessionOperationResult> SendRisikoEventAsync(int player, string actionType, string payloadJson)
    {
        view.HideRisikoKehidupanWarning();
        try
        {
            return await SendPlayerEventNowAsync(player, actionType, payloadJson, 0);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal mengirim event risiko: " + ex.Message);
            return CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
        }
    }

    private async Task SyncCoinsFromServerAsync()
    {
        if (LoginManager.Instance == null)
        {
            return;
        }

        NarafinSessionStateResult stateResult;
        try
        {
            stateResult = await LoginManager.Instance.GetActiveSessionStateAsync();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal membaca koin dari server: " + ex.Message);
            return;
        }

        if (this == null || !stateResult.Success || stateResult.Players == null)
        {
            return;
        }

        foreach (NarafinSessionStatePlayer serverPlayer in stateResult.Players)
        {
            if (serverPlayer != null && serverPlayer.player_order_no >= 1 && serverPlayer.player_order_no <= GameState.Instance.playerCount)
            {
                GameState.Instance.SetCoins(serverPlayer.player_order_no, serverPlayer.coins);
            }
        }

        view.UpdatePlayerStats();
    }

    private static string GetRisikoDisplayName(NarafinSetupLifeRisk risk)
    {
        if (risk == null)
        {
            return "Risiko Kehidupan";
        }

        return string.IsNullOrWhiteSpace(risk.item_name) ? risk.risk_code : risk.item_name;
    }
}
