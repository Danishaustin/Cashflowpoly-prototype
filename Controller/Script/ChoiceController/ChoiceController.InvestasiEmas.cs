using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    private const int HargaEmasMaxInput = 100;
    private const int JumlahEmasMaxInput = 99;

    private string investasiEmasMode = "";
    private bool investasiEmasDariRisiko;
    private int investasiEmasRisikoOriginalTurn;
    private int investasiEmasRisikoOriginalMovesLeft;
    private readonly List<int> hargaEmasOptions = new List<int>();
    private int hargaEmasOptionIndex;
    private bool isSubmittingInvestasiEmas;

    // risk_event_id kartu risiko GOLD_TRADE; kosong berarti transaksi emas hari Sabtu.
    private string goldTradeRiskEventId = string.Empty;

    // Sabtu (atau kartu risiko GOLD_TRADE): Instruktur membuka harga dari Kartu Harga Emas ruleset, lalu setiap
    // pemain berurutan memilih beli, jual, atau lewati. Alur emas dari panel risiko lama tetap memakai alur lama.
    private bool UseGoldTradeServerFlow => !investasiEmasDariRisiko && hargaEmasOptions.Count > 0;

    private bool IsGoldTradeFromRisikoServer => !string.IsNullOrEmpty(goldTradeRiskEventId);

    private void ShowInvestasiEmasHargaInput()
    {
        investasiEmasDariRisiko = false;
        goldTradeRiskEventId = string.Empty;
        hargaEmasOptions.Clear();
        hargaEmasOptions.AddRange(NarafinActiveSession.GetGoldPrices(NarafinActiveSession.Catalog));
        hargaEmasOptionIndex = 0;

        int initialPrice = hargaEmasOptions.Count > 0 ? hargaEmasOptions[0] : Mathf.Max(1, GameState.Instance.HargaEmasSaatIni);
        GameState.Instance.SetHargaEmasText(initialPrice);
        view.UpdateHargaEmasText(GameState.Instance.HargaEmasText);
        view.ShowChoice("HargaEmas");
    }

    private void ShowInvestasiEmasHargaInputFromRisikoServer(string riskEventId)
    {
        if (NarafinActiveSession.GetGoldPrices(NarafinActiveSession.Catalog).Count == 0)
        {
            FinishRisikoServer("Kartu Harga Emas tidak tersedia pada ruleset ini. Investasi emas dilewati.\n");
            return;
        }

        ShowInvestasiEmasHargaInput();
        goldTradeRiskEventId = riskEventId ?? string.Empty;
        GameState.Instance.SetTurnAndMoves(1, GameState.Instance.movesLeft);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();
    }

    private void ShowInvestasiEmasHargaInputFromRisiko()
    {
        investasiEmasDariRisiko = true;
        investasiEmasRisikoOriginalTurn = GameState.Instance.turn;
        investasiEmasRisikoOriginalMovesLeft = GameState.Instance.movesLeft;
        GameState.Instance.SetTurnAndMoves(1, GameState.Instance.ActionsPerTurn);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();
        GameState.Instance.SetHargaEmasText(Mathf.Max(1, GameState.Instance.HargaEmasSaatIni));
        view.UpdateHargaEmasText(GameState.Instance.HargaEmasText);
        view.ShowChoice("HargaEmas");
    }

    private void ShowInvestasiEmasActionChoice()
    {
        view.ShowChoice("EmasAction");
    }

    private void HandleChoiceHargaEmas(string selectedChoice)
    {
        if (isSubmittingInvestasiEmas)
        {
            return;
        }

        if (!UseGoldTradeServerFlow)
        {
            HandleChoiceHargaEmasLegacy(selectedChoice);
            return;
        }

        switch (selectedChoice)
        {
            case "MinButtonHargaEmas":
                hargaEmasOptionIndex = 0;
                break;
            case "MaxButtonHargaEmas":
                hargaEmasOptionIndex = hargaEmasOptions.Count - 1;
                break;
            case "IncreaseButtonHargaEmas":
                hargaEmasOptionIndex = Mathf.Min(hargaEmasOptions.Count - 1, hargaEmasOptionIndex + 1);
                break;
            case "DecreaseButtonHargaEmas":
                hargaEmasOptionIndex = Mathf.Max(0, hargaEmasOptionIndex - 1);
                break;
            case "ConfirmButtonHargaEmas":
                _ = BukaHargaEmasAsync(hargaEmasOptions[hargaEmasOptionIndex]);
                return;
            default:
                Debug.Log("Pilihan harga emas tidak valid");
                break;
        }

        GameState.Instance.SetHargaEmasText(hargaEmasOptions[hargaEmasOptionIndex]);
        view.UpdateHargaEmasText(GameState.Instance.HargaEmasText);
    }

    private async Task BukaHargaEmasAsync(int goldPrice)
    {
        NarafinSessionOperationResult result;
        isSubmittingInvestasiEmas = true;
        view.AddSystemTextToDialog("Membuka harga emas...");
        try
        {
            result = await SendSystemEventNowAsync("BukaHargaEmas", BuildBukaHargaEmasPayload(goldPrice));
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal membuka harga emas: " + ex.Message);
            result = CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
        }
        finally
        {
            isSubmittingInvestasiEmas = false;
        }

        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            // Kartu risiko sudah tercatat, jadi transaksi emas yang tidak bisa dibuka dilewati agar hari tetap berjalan.
            if (IsGoldTradeFromRisikoServer)
            {
                goldTradeRiskEventId = string.Empty;
                view.HideDialog();
                FinishRisikoServer("Harga emas ditolak: " + result.ErrorMessage + " Investasi emas dilewati.\n");
                return;
            }

            ShowInvestasiEmasDialogThen("Harga emas ditolak: " + result.ErrorMessage + "\n", () => view.ShowChoice("HargaEmas"));
            return;
        }

        GameState.Instance.SetHargaEmasSaatIni(goldPrice);
        ShowInvestasiEmasDialogThen("Harga emas hari ini adalah " + goldPrice + " koin.\n", ShowInvestasiEmasActionChoice);
    }

    private void HandleChoiceEmasAction(string selectedChoice)
    {
        if (isSubmittingInvestasiEmas)
        {
            return;
        }

        switch (selectedChoice)
        {
            case "BeliEmasInvestasi":
                if (GameState.Instance != null && !GameState.Instance.GoldTradeAllowBuy)
                {
                    ShowInvestasiEmasDialogThen("Ruleset ini tidak mengizinkan beli emas.\n", ShowInvestasiEmasActionChoice);
                    return;
                }

                investasiEmasMode = "Beli";
                ShowJumlahEmasInput();
                break;
            case "JualEmasInvestasi":
                if (GameState.Instance != null && !GameState.Instance.GoldTradeAllowSell)
                {
                    ShowInvestasiEmasDialogThen("Ruleset ini tidak mengizinkan jual emas.\n", ShowInvestasiEmasActionChoice);
                    return;
                }

                investasiEmasMode = "Jual";
                ShowJumlahEmasInput();
                break;
            case "LewatiEmasInvestasi":
                // Transaksi emas dari kartu risiko bersifat pilihan, jadi melewatinya tidak perlu dicatat.
                if (investasiEmasDariRisiko || IsGoldTradeFromRisikoServer)
                {
                    AdvanceInvestasiEmas();
                    return;
                }

                _ = LewatiTransaksiEmasAsync();
                break;
            default:
                Debug.Log("Pilihan aksi emas tidak valid");
                ShowInvestasiEmasActionChoice();
                break;
        }
    }

    private void ShowJumlahEmasInput()
    {
        GameState.Instance.SetJumlahEmasText(UseGoldTradeServerFlow ? Mathf.Min(1, GetMaxJumlahEmas()) : 0);
        view.UpdateJumlahEmasText(GameState.Instance.JumlahEmasText);
        view.ShowChoice("JumlahEmas");
    }

    // Beli dibatasi koin yang dimiliki, jual dibatasi emas yang dimiliki.
    private int GetMaxJumlahEmas()
    {
        int player = GameState.Instance.turn;
        int harga = Mathf.Max(1, GameState.Instance.HargaEmasSaatIni);
        int max = investasiEmasMode == "Beli"
            ? GameState.Instance.GetCoins(player) / harga
            : GameState.Instance.GetEmas(player);
        return Mathf.Clamp(max, 0, JumlahEmasMaxInput);
    }

    private void HandleChoiceJumlahEmas(string selectedChoice)
    {
        if (isSubmittingInvestasiEmas)
        {
            return;
        }

        int minAmount = UseGoldTradeServerFlow ? Mathf.Min(1, GetMaxJumlahEmas()) : 0;
        int maxAmount = UseGoldTradeServerFlow ? GetMaxJumlahEmas() : JumlahEmasMaxInput;

        switch (selectedChoice)
        {
            case "MinButtonJumlahEmas":
                GameState.Instance.SetJumlahEmasText(minAmount);
                break;
            case "MaxButtonJumlahEmas":
                GameState.Instance.SetJumlahEmasText(maxAmount);
                break;
            case "IncreaseButtonJumlahEmas":
                if (GameState.Instance.JumlahEmasText < maxAmount)
                {
                    GameState.Instance.ChangeJumlahEmasText(1);
                }
                break;
            case "DecreaseButtonJumlahEmas":
                if (GameState.Instance.JumlahEmasText > minAmount)
                {
                    GameState.Instance.ChangeJumlahEmasText(-1);
                }
                break;
            case "ConfirmButtonJumlahEmas":
                ConfirmInvestasiEmasAmount();
                return;
            default:
                Debug.Log("Pilihan jumlah emas tidak valid");
                break;
        }

        view.UpdateJumlahEmasText(GameState.Instance.JumlahEmasText);
    }

    private void ConfirmInvestasiEmasAmount()
    {
        if (GameState.Instance.JumlahEmasText <= 0)
        {
            ShowInvestasiEmasDialogThen("Jumlah emas harus lebih dari 0.\n", ShowInvestasiEmasActionChoice);
            return;
        }

        if (investasiEmasMode == "Beli")
        {
            BuyInvestasiEmas();
            return;
        }

        if (investasiEmasMode == "Jual")
        {
            SellInvestasiEmas();
            return;
        }

        ShowInvestasiEmasActionChoice();
    }

    private void BuyInvestasiEmas()
    {
        int amount = GameState.Instance.JumlahEmasText;
        int cost = GameState.Instance.HargaEmasSaatIni * amount;

        if (GameState.Instance.Coins < cost)
        {
            ShowInvestasiEmasDialogThen(
                "Coin tidak cukup untuk membeli " + amount + " emas.\n",
                ShowInvestasiEmasActionChoice);
            return;
        }

        if (!UseGoldTradeServerFlow)
        {
            BuyInvestasiEmasLegacy(amount, cost);
            return;
        }

        // Kartu risiko emas bisa keluar pada hari Kamis/Jumat, jadi cadangan donasi tetap dijaga.
        string spendBlockedMessage = GameState.Instance.GetSpendBlockedMessage(GameState.Instance.turn, cost);
        if (spendBlockedMessage != null)
        {
            ShowInvestasiEmasDialogThen("Tidak bisa membeli emas. " + spendBlockedMessage + "\n", ShowInvestasiEmasActionChoice);
            return;
        }

        _ = TradeInvestasiEmasAsync("InvestasiEmas", "BUY", amount);
    }

    private void SellInvestasiEmas()
    {
        int amount = GameState.Instance.JumlahEmasText;

        if (GameState.Instance.Emas < amount)
        {
            ShowInvestasiEmasDialogThen(
                "Emas tidak cukup untuk menjual " + amount + " emas.\n",
                ShowInvestasiEmasActionChoice);
            return;
        }

        if (!UseGoldTradeServerFlow)
        {
            SellInvestasiEmasLegacy(amount);
            return;
        }

        _ = TradeInvestasiEmasAsync("JualEmas", "SELL", amount);
    }

    private async Task TradeInvestasiEmasAsync(string actionType, string tradeType, int qty)
    {
        int player = GameState.Instance.turn;
        int unitPrice = GameState.Instance.HargaEmasSaatIni;
        int total = unitPrice * qty;
        bool isBuy = tradeType == "BUY";
        string playerName = GetPlayerName(player);

        NarafinSessionOperationResult result = await SendInvestasiEmasEventAsync(
            player,
            actionType,
            BuildGoldTradePayload(tradeType, unitPrice, qty, goldTradeRiskEventId),
            (isBuy ? "Mencatat pembelian emas " : "Mencatat penjualan emas ") + playerName + "...");

        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            ShowInvestasiEmasDialogThen(
                (isBuy ? "Pembelian emas ditolak: " : "Penjualan emas ditolak: ") + result.ErrorMessage + "\n",
                ShowInvestasiEmasActionChoice);
            return;
        }

        GameState.Instance.ChangeCoins(player, isBuy ? -total : total);
        GameState.Instance.ChangeEmas(player, isBuy ? qty : -qty);
        view.UpdateCoins(GameState.Instance.GetCoins(player));

        string resultText = isBuy
            ? playerName + " membeli " + qty + " emas seharga " + total + " koin. Emas saat ini: " + GameState.Instance.GetEmas(player) + "\n"
            : playerName + " menjual " + qty + " emas dan mendapatkan " + total + " koin. Emas tersisa: " + GameState.Instance.GetEmas(player) + "\n";
        ShowInvestasiEmasDialogThen(resultText, AdvanceInvestasiEmas);
    }

    private async Task LewatiTransaksiEmasAsync()
    {
        int player = GameState.Instance.turn;
        string playerName = GetPlayerName(player);
        NarafinSessionOperationResult result = await SendInvestasiEmasEventAsync(
            player,
            "LewatiTransaksiEmas",
            "{}",
            "Mencatat pilihan emas " + playerName + "...");

        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            ShowInvestasiEmasDialogThen("Pilihan lewati ditolak: " + result.ErrorMessage + "\n", ShowInvestasiEmasActionChoice);
            return;
        }

        ShowInvestasiEmasDialogThen(playerName + " tidak bertransaksi emas hari ini.\n", AdvanceInvestasiEmas);
    }

    private async Task<NarafinSessionOperationResult> SendInvestasiEmasEventAsync(int player, string actionType, string payloadJson, string progressText)
    {
        isSubmittingInvestasiEmas = true;
        view.AddSystemTextToDialog(progressText);
        try
        {
            return await SendPlayerEventNowAsync(player, actionType, payloadJson, 0);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal mengirim event emas: " + ex.Message);
            return CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
        }
        finally
        {
            isSubmittingInvestasiEmas = false;
        }
    }

    private void ShowInvestasiEmasDialogThen(string text, Action onComplete)
    {
        StartCoroutine(ShowInvestasiEmasDialogThenRoutine(text, onComplete));
    }

    private IEnumerator ShowInvestasiEmasDialogThenRoutine(string text, Action onComplete)
    {
        yield return view.PlaySystemDialogSteps(new List<string> { text });
        view.HideDialog();
        onComplete?.Invoke();
    }

    private void AdvanceInvestasiEmas()
    {
        if (IsGoldTradeFromRisikoServer)
        {
            AdvanceGoldTradeFromRisikoServer();
            return;
        }

        if (investasiEmasDariRisiko)
        {
            AdvanceInvestasiEmasFromRisiko();
            return;
        }

        _ = AdvanceInvestasiEmasSabtuAsync();
    }

    private async Task AdvanceInvestasiEmasSabtuAsync()
    {
        await PostAkhirGiliranForDayEndIfLastPlayerAsync(GameState.Instance.turn);
        if (this == null)
        {
            return;
        }

        bool isInvestasiEmasSelesai = GameState.Instance.AdvanceInvestasiEmasTurn();
        view.UpdateDay(GameState.Instance.day);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();

        if (isInvestasiEmasSelesai)
        {
            ShowNextScheduledChoice();
            return;
        }

        ShowInvestasiEmasActionChoice();
    }

    // Setiap pemain berurutan boleh bertransaksi sekali, lalu kembali ke pemain yang menjual masakan.
    private void AdvanceGoldTradeFromRisikoServer()
    {
        if (GameState.Instance.turn < GameState.Instance.playerCount)
        {
            GameState.Instance.SetTurnAndMoves(GameState.Instance.turn + 1, GameState.Instance.movesLeft);
            view.UpdatePlayerTurn(GameState.Instance.turn);
            view.UpdatePlayerStats();
            ShowInvestasiEmasActionChoice();
            return;
        }

        goldTradeRiskEventId = string.Empty;
        FinishRisikoServer("Investasi emas dari Risiko Kehidupan selesai.\n");
    }

    private void AdvanceInvestasiEmasFromRisiko()
    {
        if (GameState.Instance.turn < GameState.Instance.playerCount)
        {
            GameState.Instance.SetTurnAndMoves(GameState.Instance.turn + 1, GameState.Instance.ActionsPerTurn);
            view.UpdatePlayerTurn(GameState.Instance.turn);
            view.UpdatePlayerStats();
            ShowInvestasiEmasActionChoice();
            return;
        }

        investasiEmasDariRisiko = false;
        PostCurrentRisikoKehidupanEvent(risikoOriginalTurn);
        GameState.Instance.SetTurnAndMoves(investasiEmasRisikoOriginalTurn, investasiEmasRisikoOriginalMovesLeft);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();
        UpdateMove();
    }

    // Alur lama (tanpa Kartu Harga Emas di katalog, atau emas dari panel risiko lama): harga bebas dan event tidak ditunggu.
    private void HandleChoiceHargaEmasLegacy(string selectedChoice)
    {
        switch (selectedChoice)
        {
            case "MinButtonHargaEmas":
                GameState.Instance.SetHargaEmasText(1);
                break;
            case "MaxButtonHargaEmas":
                GameState.Instance.SetHargaEmasText(HargaEmasMaxInput);
                break;
            case "IncreaseButtonHargaEmas":
                if (GameState.Instance.HargaEmasText < HargaEmasMaxInput)
                {
                    GameState.Instance.ChangeHargaEmasText(1);
                }
                break;
            case "DecreaseButtonHargaEmas":
                if (GameState.Instance.HargaEmasText > 1)
                {
                    GameState.Instance.ChangeHargaEmasText(-1);
                }
                break;
            case "ConfirmButtonHargaEmas":
                if (GameState.Instance.HargaEmasText <= 0)
                {
                    ShowInvestasiEmasDialogThen("Harga emas harus lebih dari 0.\n", () => view.ShowChoice("HargaEmas"));
                    return;
                }

                GameState.Instance.SetHargaEmasSaatIni(GameState.Instance.HargaEmasText);
                ShowInvestasiEmasDialogThen(
                    "Harga emas hari ini adalah " + GameState.Instance.HargaEmasSaatIni + " koin.\n",
                    ShowInvestasiEmasActionChoice);
                return;
            default:
                Debug.Log("Pilihan harga emas tidak valid");
                break;
        }

        view.UpdateHargaEmasText(GameState.Instance.HargaEmasText);
    }

    private void BuyInvestasiEmasLegacy(int amount, int cost)
    {
        int player = GameState.Instance.turn;
        GameState.Instance.ChangeCoins(-cost);
        GameState.Instance.ChangeEmas(amount);
        view.UpdateCoins(GameState.Instance.Coins);
        PostInvestasiEmasEvent(player, "BUY", amount, GameState.Instance.HargaEmasSaatIni, cost);

        ShowInvestasiEmasDialogThen(
            GetPlayerName(player) + " membeli " + amount + " emas seharga "
            + cost + " koin. Emas saat ini: " + GameState.Instance.Emas + "\n",
            AdvanceInvestasiEmas);
    }

    private void SellInvestasiEmasLegacy(int amount)
    {
        int player = GameState.Instance.turn;
        int income = GameState.Instance.HargaEmasSaatIni * amount;
        GameState.Instance.ChangeCoins(income);
        GameState.Instance.ChangeEmas(-amount);
        view.UpdateCoins(GameState.Instance.Coins);
        PostInvestasiEmasEvent(player, "SELL", amount, GameState.Instance.HargaEmasSaatIni, income);

        ShowInvestasiEmasDialogThen(
            GetPlayerName(player) + " menjual " + amount + " emas dan mendapatkan "
            + income + " koin. Emas tersisa: " + GameState.Instance.Emas + "\n",
            AdvanceInvestasiEmas);
    }
}
