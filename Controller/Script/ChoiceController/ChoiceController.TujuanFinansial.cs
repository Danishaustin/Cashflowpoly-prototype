using System;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    private const int MaxMenabungPerAksi = 15;

    private bool isPendingTujuanFinansialConfirmation;
    private bool isSubmittingMenabung;
    private string selectedTujuanFinansialId = string.Empty;

    private static bool UseTujuanFinansialCatalog => NarafinActiveSession.GetFinancialGoals(NarafinActiveSession.Catalog).Count > 0;

    // Tujuan finansial diperoleh otomatis oleh server saat tabungan tujuan mencapai target,
    // jadi pemain memilih tujuan lalu menabung; tidak ada aksi membeli tujuan.
    private void HandleChoiceTF(string selectedChoice)
    {
        if (!UseTujuanFinansialCatalog)
        {
            HandleChoiceTFLegacy(selectedChoice);
            return;
        }

        NarafinSetupFinancialGoal goal = NarafinActiveSession.FindFinancialGoal(NarafinActiveSession.Catalog, selectedChoice);
        if (goal == null)
        {
            ShowSystemDialogThen("Tujuan finansial tidak ada di ruleset session.\n", () => view.ShowChoice("Choice1"));
            return;
        }

        int player = GameState.Instance.turn;
        if (GameState.Instance.GetTujuanFinansialRemaining(player, goal.id, goal.hargaBeli) <= 0)
        {
            ShowSystemDialogThen("Tabungan untuk " + goal.nama + " sudah mencapai target " + goal.hargaBeli + " koin.\n", () => view.ShowChoice("TujuanFinansial"));
            return;
        }

        string spendBlockedMessage = GameState.Instance.GetSpendBlockedMessage(player, 1);
        if (spendBlockedMessage != null)
        {
            ShowSystemDialogThen("Tidak bisa menabung. " + spendBlockedMessage + "\n", () => view.ShowChoice("Choice1"));
            return;
        }

        selectedTujuanFinansialId = goal.id;
        GameState.Instance.SetSavingText(Mathf.Min(1, GetMaxMenabung(goal)));
        view.ShowChoice("Menabung");
        UpdateMenabungView(goal);
    }

    private void HandleChoiceMenabung(string selectedChoice)
    {
        if (!UseTujuanFinansialCatalog)
        {
            HandleChoiceMenabungLegacy(selectedChoice);
            return;
        }

        if (isSubmittingMenabung)
        {
            return;
        }

        NarafinSetupFinancialGoal goal = NarafinActiveSession.FindFinancialGoal(NarafinActiveSession.Catalog, selectedTujuanFinansialId);
        if (goal == null)
        {
            view.ShowChoice("Choice1");
            return;
        }

        int maxAmount = GetMaxMenabung(goal);
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
                        UpdateMenabungView(goal);
                    });
                    return;
                }

                _ = MenabungAsync(goal, amount);
                return;
            default:
                Debug.Log("Pilihan tidak valid");
                break;
        }

        UpdateMenabungView(goal);
    }

    // Maksimal 15 koin per aksi, tidak melebihi koin yang boleh dibelanjakan (setelah cadangan donasi Jumat)
    // maupun sisa target tujuan.
    private int GetMaxMenabung(NarafinSetupFinancialGoal goal)
    {
        int player = GameState.Instance.turn;
        int remaining = GameState.Instance.GetTujuanFinansialRemaining(player, goal.id, goal.hargaBeli);
        return Mathf.Max(0, Mathf.Min(MaxMenabungPerAksi, Mathf.Min(GameState.Instance.GetSpendableCoins(player), remaining)));
    }

    private void UpdateMenabungView(NarafinSetupFinancialGoal goal)
    {
        int player = GameState.Instance.turn;
        view.UpdateSavingText(GameState.Instance.SavingText);
        view.UpdateMenabungTitle("Menabung untuk " + goal.nama + " ("
            + GameState.Instance.GetTujuanFinansialSaving(player, goal.id) + "/" + goal.hargaBeli + ")");
    }

    private async Task MenabungAsync(NarafinSetupFinancialGoal goal, int amount)
    {
        int player = GameState.Instance.turn;
        bool wasOwned = GameState.Instance.IsTujuanFinansialDimiliki(player, goal.id);

        NarafinSessionOperationResult result;
        isSubmittingMenabung = true;
        view.AddSystemTextToDialog("Mencatat tabungan untuk " + goal.nama + "...");
        try
        {
            result = await SendPlayerEventNowAsync(player, "Menabung", BuildMenabungPayload(goal.id, amount), GetCurrentActionSlot());
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal mengirim tabungan: " + ex.Message);
            result = CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
        }

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
        GameState.Instance.AddTujuanFinansialSaving(player, goal.id, amount, goal.hargaBeli);
        await SyncTujuanFinansialFromServerAsync(player);
        isSubmittingMenabung = false;

        if (this == null)
        {
            return;
        }

        GameState.Instance.EnsureTujuanFinansialAchieved(player, goal);
        view.UpdatePlayerStats();

        int saving = GameState.Instance.GetTujuanFinansialSaving(player, goal.id);
        string resultText = "Menabung " + amount + " koin untuk " + goal.nama + ". Tabungan: " + saving + "/" + goal.hargaBeli + "\n";
        bool isNewlyOwned = !wasOwned && GameState.Instance.IsTujuanFinansialDimiliki(player, goal.id);

        if (isNewlyOwned)
        {
            resultText += "Target tercapai! Kartu tujuan " + goal.nama + " diperoleh (+" + goal.poinKebahagiaan + " kebahagiaan).\n";
            int aksiKe = GameState.Instance.tfAksiKe;
            GameState.Instance.tfAksiKe++;

            if (PlayNpcStaticDialogThen("TujuanFinansial", resultText, UpdateMove))
            {
                return;
            }

            if (Narasi("TujuanFinansial", aksiKe, () =>
            {
                ShowSystemDialogThen(resultText, UpdateMove);
            }))
            {
                return;
            }

            ShowSystemDialogThen(resultText, UpdateMove);
            return;
        }

        if (PlayNpcStaticDialogThen("Menabung", resultText, UpdateMove))
        {
            return;
        }

        ShowSystemDialogThen(resultText, UpdateMove);
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
    private void HandleChoiceTFLegacy(string selectedChoice)
    {
        Debug.Log($"{selectedChoice} dipilih");
        if (DataManager.Instance == null || DataManager.Instance.tujuanFinansialDict == null
            || !DataManager.Instance.tujuanFinansialDict.ContainsKey(selectedChoice))
        {
            return;
        }

        var amount = 0 - DataManager.Instance.tujuanFinansialDict[selectedChoice].hargaBeli;

        if (GameState.Instance.Saving + amount < 0)
        {
            Debug.Log("Tabungan tidak cukup untuk membeli " + selectedChoice);
            view.AddTextToDialog("Tabungan tidak cukup untuk membeli " + selectedChoice + "\n");
            CompleteTujuanFinansialFlow();
            return;
        }

        GameState.Instance.ChangeSaving(amount);
        GameState.Instance.AddTujuanFinansialToList(selectedChoice);
        view.UpdateSaving(GameState.Instance.Saving);

        var amountHappiness = DataManager.Instance.tujuanFinansialDict[selectedChoice].poinKebahagiaan;
        GameState.Instance.ChangeHappiness(amountHappiness);
        view.UpdateHappiness(GameState.Instance.Happiness);
        PostTujuanFinansialEvent(GameState.Instance.turn, selectedChoice, -amount, amountHappiness);

        string resultText = "Membeli tujuan finansial " + selectedChoice + " seharga " + (-amount) + " koin dengan poin kebahagiaan " + amountHappiness + "\n";
        int aksiKe = GameState.Instance.tfAksiKe;
        GameState.Instance.tfAksiKe++;
        Debug.Log("tfAksiKe: " + GameState.Instance.tfAksiKe);

        if (PlayNpcStaticDialogThen("TujuanFinansial", resultText, CompleteTujuanFinansialFlow))
        {
            return;
        }

        if (Narasi("TujuanFinansial", aksiKe, () =>
        {
            ShowSystemDialogThen(resultText, CompleteTujuanFinansialFlow);
        }))
        {
            return;
        }

        ShowSystemDialogThen(resultText, CompleteTujuanFinansialFlow);
    }

    private void HandleChoiceMenabungLegacy(string selectedChoice)
    {
        Debug.Log($"{selectedChoice} dipilih");

        switch (selectedChoice)
        {
            case "MaxButton":
                GameState.Instance.SetSavingText(15);
                break;
            case "MinButton":
                GameState.Instance.SetSavingText(0);
                break;
            case "IncreaseButton":
                if (GameState.Instance.SavingText < 15)
                {
                    GameState.Instance.ChangeSavingText(1);
                }
                break;
            case "DecreaseButton":
                if (GameState.Instance.SavingText > 0)
                {
                    GameState.Instance.ChangeSavingText(-1);
                }
                break;
            case "ConfirmButton":
                if (GameState.Instance.SavingText <= 0 || GameState.Instance.SavingText > GameState.Instance.Coins)
                {
                    Debug.Log("Jumlah tabungan harus lebih dari 0");
                    view.AddTextToDialog("Jumlah tabungan harus lebih dari 0\n");
                    view.ShowChoice("Choice1");
                    return;
                }
                GameState.Instance.ChangeSaving(GameState.Instance.SavingText);
                GameState.Instance.ChangeCoins(-GameState.Instance.SavingText);
                view.UpdateCoins(GameState.Instance.Coins);
                view.UpdateSaving(GameState.Instance.Saving);
                PostMenabungEvent(GameState.Instance.turn, GameState.Instance.SavingText);

                string savingText = "Menabung " + GameState.Instance.SavingText + " koin\n";
                if (PlayNpcStaticDialogThen("Menabung", savingText, ContinueAfterMenabung))
                {
                    break;
                }

                ShowSystemDialogThen(savingText, ContinueAfterMenabung);
                break;
            default:
                Debug.Log("Pilihan tidak valid");
                break;
        }

        Debug.Log("SavingText: " + GameState.Instance.SavingText.ToString());
        view.UpdateSavingText(GameState.Instance.SavingText);
    }

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

    private void ContinueAfterMenabung()
    {
        _ = ContinueAfterMenabungAsync();
    }

    private async Task ContinueAfterMenabungAsync()
    {
        int previousTurn = GameState.Instance.turn;
        await PostAkhirGiliranIfDayWillAdvanceAsync(previousTurn);
        if (this == null)
        {
            return;
        }

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
        if (DataManager.Instance == null || DataManager.Instance.tujuanFinansialDict == null)
        {
            return false;
        }

        foreach (var tujuan in DataManager.Instance.tujuanFinansialDict.Values)
        {
            if (GameState.Instance.Saving >= tujuan.hargaBeli)
            {
                return true;
            }
        }

        return false;
    }

    private void CompleteTujuanFinansialFlow()
    {
        isPendingTujuanFinansialConfirmation = false;
        GameState.Instance.AdvanceTurnIfMovesDepleted();

        view.UpdateDay(GameState.Instance.day);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();

        ShowNextScheduledChoice();
    }

    public void CancelPendingTujuanFinansialConfirmation()
    {
        if (!isPendingTujuanFinansialConfirmation)
        {
            view.ShowChoice("Choice1");
            return;
        }

        CompleteTujuanFinansialFlow();
    }
}
