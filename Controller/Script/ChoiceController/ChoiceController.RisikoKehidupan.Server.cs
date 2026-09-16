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

    // Mode MAHIR dengan katalog life_risks: kartu dipilih pemain, efeknya dihitung server dari katalog.
    private static bool UseRisikoCatalog => NarafinActiveSession.IsMahir
        && NarafinActiveSession.GetLifeRisks(NarafinActiveSession.Catalog).Count > 0;

    private void StartRisikoKehidupanServer(string sourceOrderEventId)
    {
        risikoServerActive = true;
        risikoServerRisk = null;
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

                _ = KirimRisikoKehidupanAsync(riskCode);
                return true;
            }

            if (risikoServerPayer != 0)
            {
                _ = HandleRisikoServerDecisionNextAsync();
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
        ApplyRisikoServerEffect();
    }

    private void ApplyRisikoServerEffect()
    {
        NarafinSetupLifeRisk risk = risikoServerRisk;
        string riskName = GetRisikoDisplayName(risk);
        int amount = Mathf.Max(0, risk.amount);
        bool isIncome = string.Equals(risk.direction, "IN", StringComparison.OrdinalIgnoreCase);

        switch (risk.effect_type)
        {
            case RisikoEffectCoin:
                if (isIncome)
                {
                    GameState.Instance.ChangeCoins(risikoServerActorPlayer, amount);
                    FinishRisikoServer(riskName + ": " + GetPlayerName(risikoServerActorPlayer) + " mendapat " + amount + " koin.\n");
                    return;
                }

                StartRisikoServerPayment(new List<int> { risikoServerActorPlayer });
                return;
            case RisikoEffectAllPlayersCoin:
                List<int> allPlayers = GetRisikoPlayers(false);
                if (isIncome)
                {
                    foreach (int player in allPlayers)
                    {
                        GameState.Instance.ChangeCoins(player, amount);
                    }

                    FinishRisikoServer(riskName + ": semua pemain mendapat " + amount + " koin.\n");
                    return;
                }

                StartRisikoServerPayment(allPlayers);
                return;
            case RisikoEffectTransfer:
                StartRisikoServerPayment(GetRisikoPlayers(true));
                return;
            case RisikoEffectIngredientPrice:
                int delta = risk.value_delta;
                int durationDays = Mathf.Max(1, risk.duration_days);
                GameState.Instance.SetPerubahanHargaBahan(delta, durationDays);
                FinishRisikoServer(riskName + ": harga bahan " + (delta >= 0 ? "naik " : "turun ") + Mathf.Abs(delta)
                    + " koin selama " + durationDays + " hari.\n");
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
        bool canJualEmas = !canPay && GameState.Instance.GetEmas(player) > 0 && GameState.Instance.GoldTradeAllowSell;
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
            ShowRisikoServerDecision("Pembayaran ditolak: " + result.ErrorMessage);
            return;
        }

        GameState.Instance.ChangeCoins(player, -amount);
        if (risikoServerRisk.effect_type == RisikoEffectTransfer)
        {
            GameState.Instance.ChangeCoins(risikoServerActorPlayer, amount);
        }

        CompleteRisikoServerPayer(GetPlayerName(player) + " membayar " + amount + " koin.");
    }

    // Opsi darurat hanya diterima server saat saldo tidak cukup; nominalnya dihitung server, jadi koin dibaca ulang.
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

        GameState.Instance.RemoveKebutuhan(player, cardId);
        await SyncCoinsFromServerAsync();
        if (this == null)
        {
            return;
        }

        ShowRisikoServerDecision(GetPlayerName(player) + " menjual " + GameState.Instance.GetKebutuhanDisplayName(cardId) + ".");
    }

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
            BuildOpsiDaruratPayload(risikoServerEventId, "SELL_GOLD", "\"qty\":" + qty));
        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            ShowRisikoServerEmergencyRejected(player, result);
            return;
        }

        GameState.Instance.ChangeEmas(player, -qty);
        await SyncCoinsFromServerAsync();
        if (this == null)
        {
            return;
        }

        ShowRisikoServerDecision(GetPlayerName(player) + " menjual " + qty + " emas.");
    }

    private async Task UseRisikoEmergencyLoanAsync()
    {
        int player = risikoServerPayer;
        NarafinSetupLoan loan = GetPinjamanSyariahProduct();
        string itemName = string.IsNullOrWhiteSpace(loan.item_name) ? "Pinjaman Syariah" : loan.item_name;
        string loanId = loan.loan_code + ":unity:" + Guid.NewGuid().ToString("N");
        string loanPayload = BuildPinjamanSyariahPayload(loanId, loan);
        string loanFields = loanPayload.Substring(1, loanPayload.Length - 2);

        NarafinSessionOperationResult result = await SendRisikoEventAsync(
            player,
            "GunakanOpsiDarurat",
            BuildOpsiDaruratPayload(risikoServerEventId, "TAKE_SHARIA_LOAN", loanFields));
        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            ShowRisikoServerEmergencyRejected(player, result);
            return;
        }

        GameState.Instance.AddPinjamanSyariah(player, new PinjamanSyariahHolding
        {
            LoanId = loanId,
            LoanCode = loan.loan_code,
            ItemName = itemName,
            Principal = loan.principal,
            RepaymentAmount = loan.repayment_amount
        });
        await SyncCoinsFromServerAsync();
        if (this == null)
        {
            return;
        }

        ShowRisikoServerDecision(GetPlayerName(player) + " mengambil " + itemName + ".");
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
