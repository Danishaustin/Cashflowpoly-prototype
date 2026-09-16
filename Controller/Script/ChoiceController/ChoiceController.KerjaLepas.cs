using System;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    private bool isSubmittingKerjaLepas;

    // Handles Kerja Lepas reward.
    private void HandleChoiceKL()
    {
        if (isSubmittingKerjaLepas)
        {
            return;
        }

        _ = KerjaLepasAsync();
    }

    // Pendapatan kerja lepas mengikuti freelance_income ruleset session dan baru dicatat setelah server menerima.
    private async Task KerjaLepasAsync()
    {
        int player = GameState.Instance.turn;
        int income = GameState.Instance.FreelanceIncome;
        if (income <= 0)
        {
            ShowSystemDialogThen("Pendapatan kerja lepas pada ruleset ini tidak valid.\n", () => view.ShowChoice("Choice1"));
            return;
        }

        NarafinSessionOperationResult result;
        isSubmittingKerjaLepas = true;
        view.AddSystemTextToDialog("Mencatat kerja lepas...");
        try
        {
            result = await SendPlayerEventNowAsync(player, "KerjaLepas", BuildKerjaLepasPayload(income), GetCurrentActionSlot());
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal mengirim kerja lepas: " + ex.Message);
            result = CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
        }
        finally
        {
            isSubmittingKerjaLepas = false;
        }

        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            ShowSystemDialogThen("Kerja lepas ditolak: " + result.ErrorMessage + "\n", () => view.ShowChoice("Choice1"));
            return;
        }

        GameState.Instance.ChangeCoins(player, income);
        view.UpdateCoins(GameState.Instance.GetCoins(player));

        string resultText = "Bekerja lepas mendapatkan " + income + " koin\n";
        int aksiKe = GameState.Instance.klAksiKe;
        GameState.Instance.klAksiKe++;
        Debug.Log("klAksiKe: " + GameState.Instance.klAksiKe);

        if (Narasi("KerjaLepas", aksiKe, () =>
        {
            ShowSystemDialogThen(resultText, UpdateMove);
        }))
        {
            return;
        }

        ShowSystemDialogThen(resultText, UpdateMove);
    }
}
