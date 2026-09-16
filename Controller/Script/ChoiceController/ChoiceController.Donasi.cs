using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    private bool isSubmittingDonasi;

    // Handles Peduli Donasi / Jumat Berkah: setiap pemain wajib berdonasi tepat satu kali sesuai urutan giliran.
    private void JumatBerkah(string selectedChoice)
    {
        Debug.Log($"{selectedChoice} dipilih");
        if (isSubmittingDonasi)
        {
            return;
        }

        int player = GameState.Instance.turn;
        int minAmount = GetMinDonasi();
        int maxAmount = GetMaxDonasi(player);

        switch (selectedChoice)
        {
            case "MaxButtonJB":
                GameState.Instance.SetSavingText(maxAmount);
                break;
            case "MinButtonJB":
                GameState.Instance.SetSavingText(Mathf.Min(minAmount, maxAmount));
                break;
            case "IncreaseButtonJB":
                if (GameState.Instance.SavingText < maxAmount)
                {
                    GameState.Instance.ChangeSavingText(1);
                }
                break;
            case "DecreaseButtonJB":
                if (GameState.Instance.SavingText > minAmount)
                {
                    GameState.Instance.ChangeSavingText(-1);
                }
                break;
            case "ConfirmButtonJB":
                int amount = GameState.Instance.SavingText;
                if (amount < minAmount || amount > maxAmount)
                {
                    ShowSystemDialogThen("Donasi harus " + minAmount + " sampai " + maxAmount + " koin.", () => view.ShowChoice("JumatBerkah"));
                    return;
                }

                _ = DonasiAsync(player, amount);
                return;
            default:
                Debug.Log("Pilihan tidak valid");
                ShowSystemDialogThen("Pilihan tidak valid.", () => view.ShowChoice("JumatBerkah"));
                return;
        }

        view.UpdateJumatBerkahText(GameState.Instance.SavingText);
    }

    private int GetMinDonasi()
    {
        return Mathf.Max(1, GameState.Instance.DonationMinAmount);
    }

    private int GetMaxDonasi(int player)
    {
        return Mathf.Min(GameState.Instance.GetCoins(player), Mathf.Max(GetMinDonasi(), GameState.Instance.DonationMaxAmount));
    }

    private async Task DonasiAsync(int player, int amount)
    {
        string playerName = GetPlayerName(player);
        NarafinSessionOperationResult result = await SendDonasiEventAsync(
            player,
            "JumatBerkah",
            BuildJumatBerkahPayload(amount),
            0,
            "Mencatat donasi " + playerName + "...");

        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            ShowSystemDialogThen("Donasi " + playerName + " ditolak: " + result.ErrorMessage, () => view.ShowChoice("JumatBerkah"));
            return;
        }

        GameState.Instance.CatatPeduliDonasi(amount);
        GameState.Instance.ChangeCoins(player, -amount);
        view.UpdateCoins(GameState.Instance.GetCoins(player));
        await PostAkhirGiliranForDayEndIfLastPlayerAsync(player);
        if (this == null)
        {
            return;
        }

        bool isPeduliDonasiSelesai = GameState.Instance.AdvancePeduliDonasiTurn();
        view.UpdateDay(GameState.Instance.day);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();

        ShowSystemDialogThen(playerName + " berdonasi " + amount + " coin.", () => ContinueAfterPeduliDonasiStep(isPeduliDonasiSelesai));
    }

    // Donasi Jumat wajib; pemain yang koinnya kurang melakukan Kerja Lepas dulu (diterima server sebelum donasi)
    // agar hari Jumat tidak macet.
    private void ShowJumatBerkahOrSkipNoCoins()
    {
        int player = GameState.Instance.turn;
        if (GameState.Instance.GetCoins(player) >= GetMinDonasi())
        {
            view.ShowChoice("JumatBerkah");
            return;
        }

        string playerName = GetPlayerName(player);
        ShowSystemDialogThen(
            playerName + " tidak memiliki cukup koin untuk donasi wajib, jadi melakukan Kerja Lepas terlebih dahulu.",
            () => _ = KerjaLepasSebelumDonasiAsync(player));
    }

    private async Task KerjaLepasSebelumDonasiAsync(int player)
    {
        string playerName = GetPlayerName(player);
        int income = GameState.Instance.FreelanceIncome;
        NarafinSessionOperationResult result = await SendDonasiEventAsync(
            player,
            "KerjaLepas",
            BuildKerjaLepasPayload(income),
            GetCurrentActionSlot(),
            "Mencatat kerja lepas " + playerName + "...");

        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            ShowSystemDialogThen(
                "Kerja lepas " + playerName + " ditolak: " + result.ErrorMessage + " Donasi Jumat wajib, jadi coba lagi.",
                ShowJumatBerkahOrSkipNoCoins);
            return;
        }

        GameState.Instance.ChangeCoins(player, income);
        GameState.Instance.ConsumeMoveWithoutTurnProgress();
        view.UpdateCoins(GameState.Instance.GetCoins(player));
        ShowSystemDialogThen(playerName + " bekerja lepas mendapatkan " + income + " koin.", ShowJumatBerkahOrSkipNoCoins);
    }

    private async Task<NarafinSessionOperationResult> SendDonasiEventAsync(int player, string actionType, string payloadJson, int actionSlot, string progressText)
    {
        isSubmittingDonasi = true;
        view.AddSystemTextToDialog(progressText);
        try
        {
            return await SendPlayerEventNowAsync(player, actionType, payloadJson, actionSlot);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal mengirim event Jumat: " + ex.Message);
            return CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
        }
        finally
        {
            isSubmittingDonasi = false;
        }
    }

    private void ContinueAfterPeduliDonasiStep(bool isPeduliDonasiSelesai)
    {
        if (!isPeduliDonasiSelesai)
        {
            ShowJumatBerkahOrSkipNoCoins();
            return;
        }

        string juaraText = BuildJuaraPeduliDonasiText();
        if (string.IsNullOrEmpty(juaraText))
        {
            ShowNextScheduledChoice();
            return;
        }

        ShowSystemDialogThen(juaraText, ShowNextScheduledChoice);
    }

    private string BuildJuaraPeduliDonasiText()
    {
        List<int> ranking = GameState.Instance.GetLatestPeduliDonasiRanking();
        if (ranking == null || ranking.Count == 0)
        {
            return string.Empty;
        }

        int topCount = Mathf.Min(3, ranking.Count);
        var parts = new List<string>();
        for (int i = 0; i < topCount; i++)
        {
            string juaraName = GetPlayerName(ranking[i]);
            parts.Add("Juara " + (i + 1) + ": " + juaraName);
        }

        return string.Join(" | ", parts);
    }

    private string GetPlayerName(int player)
    {
        return PlayerPrefs.GetString("PlayerName_" + player, "Player " + player);
    }
}
