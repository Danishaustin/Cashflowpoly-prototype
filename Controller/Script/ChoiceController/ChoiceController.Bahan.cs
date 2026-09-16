using System;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    private bool isSubmittingBahanMasakan;

    // Handles buying cooking ingredients from ChoiceBM; selectedChoice berisi card_id bahan ruleset session.
    private void HandleChoiceBahan(string selectedChoice)
    {
        if (isSubmittingBahanMasakan)
        {
            return;
        }

        _ = BuyBahanMasakanAsync(selectedChoice);
    }

    private async Task BuyBahanMasakanAsync(string selectedChoice)
    {
        GameState gameState = GameState.Instance;
        int activePlayer = gameState.turn;
        string bahanKey = gameState.ResolveBahanKey(selectedChoice);
        string bahanName = gameState.GetBahanDisplayName(bahanKey);
        Debug.Log(bahanName + " (" + bahanKey + ") dipilih");

        if (!gameState.IsKnownBahan(bahanKey))
        {
            ShowSystemDialogThen("Bahan " + bahanName + " tidak ada di ruleset session.\n", () => view.ShowChoice("BahanMasakan"));
            return;
        }

        if (gameState.IsBahanTotalAtLimit(activePlayer))
        {
            ShowSystemDialogThen("Bahan masakan sudah maksimal " + gameState.MaxIngredientTotal + " kartu.\n", () => view.ShowChoice("Choice1"));
            return;
        }

        if (gameState.IsBahanAtLimit(activePlayer, bahanKey))
        {
            ShowSystemDialogThen(bahanName + " sudah maksimal " + gameState.MaxSameIngredient + " kartu.\n", () => view.ShowChoice("BahanMasakan"));
            return;
        }

        int hargaBahan = gameState.GetHargaBahanEfektif(bahanKey);
        if (hargaBahan <= 0)
        {
            ShowSystemDialogThen("Harga " + bahanName + " tidak valid.\n", () => view.ShowChoice("BahanMasakan"));
            return;
        }

        string spendBlockedMessage = gameState.GetSpendBlockedMessage(activePlayer, hargaBahan);
        if (spendBlockedMessage != null)
        {
            ShowSystemDialogThen("Tidak bisa membeli " + bahanName + ". " + spendBlockedMessage + "\n", () => view.ShowChoice("BahanMasakan"));
            return;
        }

        NarafinSessionOperationResult result;
        isSubmittingBahanMasakan = true;
        view.AddSystemTextToDialog("Mencatat pembelian " + bahanName + "...");
        try
        {
            result = await SendPlayerEventNowAsync(
                activePlayer,
                "BahanMasakan",
                BuildBahanMasakanPayload(bahanKey, bahanName, hargaBahan),
                GetCurrentActionSlot());
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal mengirim pembelian bahan: " + ex.Message);
            result = CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
        }
        finally
        {
            isSubmittingBahanMasakan = false;
        }

        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            ShowSystemDialogThen("Pembelian " + bahanName + " ditolak: " + result.ErrorMessage + "\n", () => view.ShowChoice("BahanMasakan"));
            return;
        }

        gameState.AddBahanToList(activePlayer, bahanKey);
        gameState.ChangeCoins(activePlayer, -hargaBahan);
        view.UpdateCoins(gameState.GetCoins(activePlayer));

        string resultText = "Membeli " + bahanName + " seharga " + hargaBahan + " koin\n";
        int aksiKe = gameState.bmAksiKe;
        gameState.bmAksiKe++;
        Debug.Log("bmAksiKe: " + gameState.bmAksiKe);

        if (PlayNpcStaticDialogThen("BahanMasakan", resultText, UpdateMove))
        {
            return;
        }

        if (Narasi("BahanMasakan", aksiKe, () =>
        {
            ShowSystemDialogThen(resultText, UpdateMove);
        }))
        {
            return;
        }

        ShowSystemDialogThen(resultText, UpdateMove);
    }
}
