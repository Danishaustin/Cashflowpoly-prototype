using System;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    private const int MaxMenabungPerAksi = 15;

    private bool isPendingTujuanFinansialConfirmation;
    private bool isSubmittingMenabung;

    // Hari yang seharusnya ditutup oleh aksi menabung, tapi penutupannya ditahan sampai pemain
    // selesai dengan tawaran kartu tujuan. Tanpa penundaan ini server sudah berpindah hari saat
    // pembelian dikirim dan menolaknya dengan day_index MISMATCH.
    private bool isDayEndPendingAfterTujuanFinansial;

    private static bool UseTujuanFinansialCatalog => NarafinActiveSession.GetFinancialGoals(NarafinActiveSession.Catalog).Count > 0;

    // Tabungan mengikuti backend: satu saldo per pemain. Aksi ini hanya menabung; kartu tujuan dibeli
    // setelahnya lewat konfirmasi, dan tidak pernah diberikan otomatis saat tabungan mencukupi.
    private void StartMenabung()
    {
        int player = GameState.Instance.turn;
        int maxAmount = GetMaxMenabung();
        if (maxAmount <= 0)
        {
            string spendBlockedMessage = GameState.Instance.GetSpendBlockedMessage(player, 1);
            ShowSystemDialogThen("Tidak bisa menabung. " + (spendBlockedMessage ?? "Koin tidak mencukupi.") + "\n", () => view.ShowChoice("Choice1"));
            return;
        }

        GameState.Instance.SetSavingText(Mathf.Min(1, maxAmount));
        view.ShowChoice("Menabung");
        UpdateMenabungView();
    }

    private void HandleChoiceMenabung(string selectedChoice)
    {
        if (!UseTujuanFinansialCatalog)
        {
            ShowSystemDialogThen(
                "Katalog tujuan finansial tidak tersedia pada ruleset ini, jadi menabung tidak bisa dicatat.\n",
                () => view.ShowChoice("Choice1"));
            return;
        }

        if (isSubmittingMenabung)
        {
            return;
        }

        int maxAmount = GetMaxMenabung();
        switch (selectedChoice)
        {
            case "MaxButton":
                GameState.Instance.SetSavingText(maxAmount);
                break;
            case "MinButton":
                GameState.Instance.SetSavingText(Mathf.Min(1, maxAmount));
                break;
            case "IncreaseButton":
                if (GameState.Instance.SavingText < maxAmount)
                {
                    GameState.Instance.ChangeSavingText(1);
                }
                break;
            case "DecreaseButton":
                if (GameState.Instance.SavingText > 1)
                {
                    GameState.Instance.ChangeSavingText(-1);
                }
                break;
            case "ConfirmButton":
                int amount = GameState.Instance.SavingText;
                if (amount <= 0 || amount > maxAmount)
                {
                    ShowSystemDialogThen("Jumlah tabungan harus 1 sampai " + maxAmount + " koin.\n", () =>
                    {
                        view.ShowChoice("Menabung");
                        UpdateMenabungView();
                    });
                    return;
                }

                AskConfirmation(
                    "Menabung " + amount + " koin?",
                    () => _ = MenabungAsync(amount),
                    () =>
                    {
                        view.ShowChoice("Menabung");
                        UpdateMenabungView();
                    });
                return;
            default:
                Debug.Log("Pilihan tidak valid");
                break;
        }

        UpdateMenabungView();
    }

    // Maksimal 15 koin per aksi dan tidak melebihi koin yang boleh dibelanjakan setelah cadangan donasi Jumat.
    private int GetMaxMenabung()
    {
        int player = GameState.Instance.turn;
        return Mathf.Max(0, Mathf.Min(MaxMenabungPerAksi, GameState.Instance.GetSpendableCoins(player)));
    }

    private void UpdateMenabungView()
    {
        int player = GameState.Instance.turn;
        view.UpdateSavingText(GameState.Instance.SavingText);
        view.UpdateMenabungTitle("Jumlah Menabung (tabungan: " + GameState.Instance.GetSaving(player) + " koin)");
    }

    private async Task MenabungAsync(int amount)
    {
        int player = GameState.Instance.turn;

        NarafinSessionOperationResult result;
        isSubmittingMenabung = true;
        BeginServerWait("Mencatat tabungan...");
        try
        {
            result = await SendPlayerEventNowAsync(player, "Menabung", BuildMenabungPayload(amount), GetCurrentActionSlot());
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal mengirim tabungan: " + ex.Message);
            result = CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
        }

        await EndServerWaitAsync();

        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            isSubmittingMenabung = false;
            ShowSystemDialogThen("Menabung ditolak: " + result.ErrorMessage + "\n", () => view.ShowChoice("Choice1"));
            return;
        }

        GameState.Instance.ChangeCoins(player, -amount);
        GameState.Instance.ChangeSaving(player, amount);
        await SyncTujuanFinansialFromServerAsync(player);
        isSubmittingMenabung = false;

        if (this == null)
        {
            return;
        }

        view.UpdatePlayerStats();
        string resultText = "Menabung " + amount + " koin. Tabungan: " + GameState.Instance.GetSaving(player) + " koin\n";
        PlayNarasiThen("Menabung", resultText, ContinueAfterMenabung);
    }

    // Kartu tujuan dibeli dari tabungan setelah pemain mengonfirmasi; server mencatatnya sebagai event SYSTEM.
    private void HandleChoiceTF(string selectedChoice)
    {
        if (!UseTujuanFinansialCatalog)
        {
            ShowSystemDialogThen(
                "Katalog tujuan finansial tidak tersedia pada ruleset ini, jadi kartu tujuan tidak bisa dicatat.\n",
                () => view.ShowChoice("Choice1"));
            return;
        }

        if (isSubmittingMenabung)
        {
            return;
        }

        NarafinSetupFinancialGoal goal = NarafinActiveSession.FindFinancialGoal(NarafinActiveSession.Catalog, selectedChoice);
        if (goal == null)
        {
            ShowSystemDialogThen("Tujuan finansial tidak ada di ruleset session.\n", CompleteTujuanFinansialFlow);
            return;
        }

        int player = GameState.Instance.turn;
        if (GameState.Instance.IsTujuanFinansialDimiliki(player, goal.id))
        {
            ShowSystemDialogThen("Kartu tujuan " + goal.nama + " sudah dimiliki.\n", () => view.ShowTujuanFinansialPurchasableOnly());
            return;
        }

        if (GameState.Instance.GetSaving(player) < goal.hargaBeli)
        {
            ShowSystemDialogThen("Tabungan belum cukup untuk " + goal.nama + " (" + goal.hargaBeli + " koin).\n", () => view.ShowTujuanFinansialPurchasableOnly());
            return;
        }

        AskConfirmation(
            "Beli kartu tujuan " + goal.nama + " seharga " + goal.hargaBeli + " koin tabungan (+"
                + goal.poinKebahagiaan + " kebahagiaan)?",
            () => _ = BeliTujuanFinansialAsync(goal),
            () => view.ShowTujuanFinansialPurchasableOnly());
    }

    private async Task BeliTujuanFinansialAsync(NarafinSetupFinancialGoal goal)
    {
        int player = GameState.Instance.turn;

        NarafinSessionOperationResult result;
        isSubmittingMenabung = true;
        BeginServerWait("Mencatat pembelian " + goal.nama + "...");
        try
        {
            result = await SendSystemEventForPlayerNowAsync(player, "TujuanFinansial", BuildTujuanFinansialPayload(goal));
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal mengirim pembelian tujuan finansial: " + ex.Message);
            result = CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
        }

        await EndServerWaitAsync();
        isSubmittingMenabung = false;

        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            ShowSystemDialogThen("Pembelian tujuan ditolak: " + result.ErrorMessage + "\n", () => view.ShowTujuanFinansialPurchasableOnly());
            return;
        }

        GameState.Instance.BeliTujuanFinansial(player, goal);
        await SyncTujuanFinansialFromServerAsync(player);

        if (this == null)
        {
            return;
        }

        view.UpdatePlayerStats();
        string resultText = "Membeli kartu tujuan " + goal.nama + " seharga " + goal.hargaBeli + " koin tabungan (+"
            + goal.poinKebahagiaan + " kebahagiaan). Sisa tabungan: " + GameState.Instance.GetSaving(player) + " koin\n";
        PlayNarasiThen("TujuanFinansial", resultText, CompleteTujuanFinansialFlow);
    }

    private async Task SyncTujuanFinansialFromServerAsync(int player)
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
            Debug.LogWarning("Gagal membaca state tabungan dari server: " + ex.Message);
            return;
        }

        if (!stateResult.Success || stateResult.Players == null || GameState.Instance == null)
        {
            return;
        }

        NarafinActiveSession.SetStateVersion(stateResult.StateVersion);
        foreach (NarafinSessionStatePlayer serverPlayer in stateResult.Players)
        {
            if (serverPlayer != null && serverPlayer.player_order_no == player)
            {
                GameState.Instance.ApplyServerTujuanFinansial(player, serverPlayer);
                return;
            }
        }
    }

    // Alur lama tanpa katalog ruleset session (mode offline): menabung ke tabungan gabungan lalu membeli tujuan.
    private void HandleChoiceTFConfirm(string selectedChoice)
    {
        if (!isPendingTujuanFinansialConfirmation)
        {
            view.ShowChoice("Choice1");
            return;
        }

        if (selectedChoice == "TFConfirmYesButton")
        {
            view.ShowTujuanFinansialPurchasableOnly();
            return;
        }

        if (selectedChoice == "TFConfirmNoButton")
        {
            CompleteTujuanFinansialFlow();
        }
    }

    // Tidak lagi async: satu-satunya penantian di sini dulu adalah penutupan hari, dan itu kini
    // ditunda sampai CompleteTujuanFinansialFlowAsync.
    private void ContinueAfterMenabung()
    {
        int previousTurn = GameState.Instance.turn;

        // Dihitung SEBELUM jatah aksi dikurangi, lalu ditahan. Penutupan harinya sendiri menunggu
        // CompleteTujuanFinansialFlow, supaya pembelian kartu tujuan masih jatuh pada hari yang
        // sama menurut server.
        isDayEndPendingAfterTujuanFinansial = WillDayAdvanceAfterAction(previousTurn);

        GameState.Instance.ConsumeMoveWithoutTurnProgress();
        view.UpdateDay(GameState.Instance.day);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();

        if (!HasAffordableTujuanFinansial())
        {
            CompleteTujuanFinansialFlow();
            return;
        }

        isPendingTujuanFinansialConfirmation = true;
        view.ShowTujuanFinansialConfirmation();
    }

    private bool HasAffordableTujuanFinansial()
    {
        if (UseTujuanFinansialCatalog)
        {
            int player = GameState.Instance.turn;
            foreach (NarafinSetupFinancialGoal goal in NarafinActiveSession.GetFinancialGoals(NarafinActiveSession.Catalog))
            {
                if (!GameState.Instance.IsTujuanFinansialDimiliki(player, goal.id)
                    && GameState.Instance.GetSaving(player) >= goal.hargaBeli)
                {
                    return true;
                }
            }

            return false;
        }

        return false;
    }

    // Satu-satunya jalan keluar alur tujuan finansial: dipakai sesudah membeli, sesudah menolak
    // tawaran, dan saat tidak ada kartu yang terjangkau. Karena itu penutupan hari yang ditunda
    // diselesaikan di sini.
    private void CompleteTujuanFinansialFlow()
    {
        _ = CompleteTujuanFinansialFlowAsync();
    }

    private async Task CompleteTujuanFinansialFlowAsync()
    {
        isPendingTujuanFinansialConfirmation = false;

        if (isDayEndPendingAfterTujuanFinansial)
        {
            isDayEndPendingAfterTujuanFinansial = false;

            await PostAkhirGiliranForDayEndAsync();
            if (this == null)
            {
                return;
            }

            // Urutannya disamakan dengan UpdateMoveAsync: narasi akhir hari sesudah hari ditutup
            // dan sebelum hari klien berganti. Jalur menabung sebelumnya melewatkan narasi ini.
            await PlayEndingHariRollingAsync();
            if (this == null)
            {
                return;
            }
        }

        GameState.Instance.AdvanceTurnIfMovesDepleted();

        view.UpdateDay(GameState.Instance.day);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();

        ShowNextScheduledChoice();
    }

    // Back dari daftar kartu tujuan kembali ke panel "ingin membeli?", bukan langsung mengakhiri giliran.
    public void BackFromTujuanFinansialList()
    {
        if (!isPendingTujuanFinansialConfirmation)
        {
            view.ShowChoice("Choice1");
            return;
        }

        view.ShowTujuanFinansialConfirmation();
    }
}
