using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
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

    // event_id BukaHargaEmas yang terakhir diterima server, beserta harinya. Opsi darurat SELL_GOLD wajib
    // menyertakan gold_price_event_id yang menunjuk event harga pada day_index yang sama.
    private string hargaEmasEventId = string.Empty;
    private int hargaEmasEventDay;

    // Sabtu (atau kartu risiko GOLD_TRADE): Instruktur membuka harga dari Kartu Harga Emas ruleset, lalu setiap
    // pemain berurutan memilih beli, jual, atau lewati. Alur emas dari panel risiko lama tetap memakai alur lama.

    private bool IsGoldTradeFromRisikoServer => !string.IsNullOrEmpty(goldTradeRiskEventId);

    // Server hanya mengizinkan BukaHargaEmas pada hari Sabtu atau saat ada risiko emas aktif, jadi pada hari
    // lain harga emas tidak pernah terbuka dan jual emas darurat memang tidak tersedia.
    private bool HasHargaEmasHariIni =>
        !string.IsNullOrEmpty(hargaEmasEventId)
        && GameState.Instance != null
        && hargaEmasEventDay == GameState.Instance.day;

    // Harga emas selalu berasal dari Kartu Harga Emas ruleset; tanpa kartu itu transaksi emas tidak tersedia.
    private bool LoadHargaEmasOptionsFromCatalog()
    {
        hargaEmasOptions.Clear();
        hargaEmasOptions.AddRange(NarafinActiveSession.GetGoldPrices(NarafinActiveSession.Catalog));
        hargaEmasOptionIndex = 0;
        return hargaEmasOptions.Count > 0;
    }

    private void ShowHargaEmasPanel()
    {
        GameState.Instance.SetHargaEmasText(hargaEmasOptions[hargaEmasOptionIndex]);
        view.UpdateHargaEmasText(GameState.Instance.HargaEmasText);
        view.ShowChoice("HargaEmas");
    }

    private void ShowInvestasiEmasHargaInput()
    {
        investasiEmasDariRisiko = false;
        goldTradeRiskEventId = string.Empty;

        // Ruleset yang mengaktifkan Sabtu tetapi tanpa Kartu Harga Emas: harinya ditutup, bukan memakai harga bebas.
        if (!LoadHargaEmasOptionsFromCatalog())
        {
            ShowInvestasiEmasDialogThen(
                "Kartu Harga Emas tidak tersedia pada ruleset ini. Hari investasi emas dilewati.\n",
                () => _ = LewatiHariSabtuAsync());
            return;
        }

        ShowHargaEmasPanel();
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

    // Panel risiko tanpa katalog life_risks tidak punya risk_event_id, sedangkan server hanya mengizinkan
    // BukaHargaEmas di luar hari Sabtu bila ada efek Risiko Kehidupan emas yang aktif (diuji 3 Okt 2026:
    // 422 "Harga emas hanya dibuka pada Sabtu atau saat efek Risiko Kehidupan emas aktif", dan transaksinya
    // 400 "Weekday harus SAT" bila tanpa risk_event_id). Jadi transaksinya tidak mungkin tercatat; dilewati
    // dengan keterangan, bukan dicoba lalu gagal berulang di panel harga.
    private void ShowInvestasiEmasHargaInputFromRisiko()
    {
        investasiEmasDariRisiko = true;
        investasiEmasRisikoOriginalTurn = GameState.Instance.turn;
        investasiEmasRisikoOriginalMovesLeft = GameState.Instance.movesLeft;

        ShowInvestasiEmasDialogThen(
            "Transaksi emas dari kartu risiko hanya tersedia pada ruleset yang memuat katalog Risiko Kehidupan. Investasi emas dilewati.\n",
            FinishInvestasiEmasFromRisiko);
    }

    public void BackToInvestasiEmasAction()
    {
        ShowInvestasiEmasActionChoice();
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
                // Harga ini berlaku untuk seluruh pemain hari itu dan tidak bisa dibuka ulang.
                int hargaDipilih = hargaEmasOptions[hargaEmasOptionIndex];
                AskConfirmation(
                    "Buka harga emas hari ini sebesar " + hargaDipilih + " koin?",
                    () => _ = BukaHargaEmasAsync(hargaDipilih),
                    ShowHargaEmasPanel);
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
        BeginServerWait("Membuka harga emas...");
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

        await EndServerWaitAsync();

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
        hargaEmasEventId = result.EventId ?? string.Empty;
        hargaEmasEventDay = GameState.Instance.day;
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
                // Putaran emas dari kartu risiko WAJIB dicatat: server menuntut tepat satu keputusan per
                // pemain per putaran, dan selama belum lengkap seluruh aktivitas sesudahnya ditolak 422.
                // Jadi keputusan ini tidak bisa ditarik kembali: sesudah terkirim pemain tersebut
                // tidak bisa lagi beli atau jual pada putaran yang sama.
                AskConfirmation(
                    "Lewati transaksi emas untuk " + GetPlayerName(GameState.Instance.turn) + "?",
                    LewatiTransaksiEmas,
                    ShowInvestasiEmasActionChoice);
                return;
            default:
                Debug.Log("Pilihan aksi emas tidak valid");
                ShowInvestasiEmasActionChoice();
                break;
        }
    }

    // Jalur investasiEmasDariRisiko adalah cadangan tanpa katalog yang tidak pernah membuka harga
    // emas, jadi di sana tidak ada putaran server yang perlu ditutup.
    private void LewatiTransaksiEmas()
    {
        if (investasiEmasDariRisiko)
        {
            AdvanceInvestasiEmas();
            return;
        }

        _ = LewatiTransaksiEmasAsync();
    }

    private void ShowJumlahEmasInput()
    {
        GameState.Instance.SetJumlahEmasText(Mathf.Min(1, GetMaxJumlahEmas()));
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

        int minAmount = Mathf.Min(1, GetMaxJumlahEmas());
        int maxAmount = GetMaxJumlahEmas();

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

        int amount = GameState.Instance.JumlahEmasText;
        int hargaSatuan = GameState.Instance.HargaEmasSaatIni;

        if (investasiEmasMode == "Beli")
        {
            AskConfirmation(
                "Beli " + amount + " emas seharga " + (amount * hargaSatuan) + " koin?",
                BuyInvestasiEmas,
                ShowInvestasiEmasActionChoice);
            return;
        }

        if (investasiEmasMode == "Jual")
        {
            AskConfirmation(
                "Jual " + amount + " emas seharga " + (amount * hargaSatuan) + " koin?",
                SellInvestasiEmas,
                ShowInvestasiEmasActionChoice);
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
        PlayNarasiThen(actionType, resultText, AdvanceInvestasiEmas);
    }

    private async Task LewatiTransaksiEmasAsync()
    {
        int player = GameState.Instance.turn;
        string playerName = GetPlayerName(player);
        // Putaran emas akibat kartu risiko wajib merujuk risk_event_id, sama seperti beli/jual: tanpa itu
        // trigger server menolak dan putaran tidak pernah lengkap, sehingga aktivitas hari itu terhalang.
        // Pada hari Sabtu tidak ada risiko tertunda, dan menyertakan risk_event_id di sana justru ditolak.
        string lewatiPayload = string.IsNullOrEmpty(goldTradeRiskEventId)
            ? "{}"
            : BuildRiskEventPayload(goldTradeRiskEventId);

        NarafinSessionOperationResult result = await SendInvestasiEmasEventAsync(
            player,
            "LewatiTransaksiEmas",
            lewatiPayload,
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
        BeginServerWait(progressText);

        NarafinSessionOperationResult result;
        try
        {
            result = await SendPlayerEventNowAsync(player, actionType, payloadJson, 0);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal mengirim event emas: " + ex.Message);
            result = CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
        }
        finally
        {
            isSubmittingInvestasiEmas = false;
        }

        await EndServerWaitAsync();
        return result;
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
        bool isHariBerganti = GameState.Instance.IsLastPlayerInTurnOrder(GameState.Instance.turn);
        await PostAkhirGiliranForDayEndIfLastPlayerAsync(GameState.Instance.turn);
        if (this == null)
        {
            return;
        }

        if (isHariBerganti)
        {
            await PlayEndingHariRollingAsync();
            if (this == null)
            {
                return;
            }
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

        FinishInvestasiEmasFromRisiko();
    }

    private void FinishInvestasiEmasFromRisiko()
    {
        investasiEmasDariRisiko = false;
        PostCurrentRisikoKehidupanEvent(risikoOriginalTurn);
        GameState.Instance.SetTurnAndMoves(investasiEmasRisikoOriginalTurn, investasiEmasRisikoOriginalMovesLeft);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();
        UpdateMove();
    }
}
