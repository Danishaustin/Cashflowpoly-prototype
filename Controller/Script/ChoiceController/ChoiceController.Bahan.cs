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

        string bahanKey = GameState.Instance.ResolveBahanKey(selectedChoice);
        string bahanName = GameState.Instance.GetBahanDisplayName(bahanKey);
        int harga = GameState.Instance.GetHargaBahanEfektif(bahanKey);

        AskConfirmation(
            "Beli bahan " + bahanName + " seharga " + harga + " koin?",
            () => _ = BuyBahanMasakanAsync(selectedChoice),
            () => view.ShowChoice("BahanMasakan"));
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

        // Harga nol SAH, dan tidak boleh ditolak. GetHargaBahanEfektif menjepit hasilnya dengan
        // Mathf.Max(0, ...), jadi kartu risiko INGREDIENT_PRICE_MODIFIER yang menurunkan harga sampai
        // di bawah harga dasar membuat bahannya gratis -- bukan rusak. Server pun sengaja
        // mengizinkannya: jalur pembelian bahan divalidasi "Amount minimal 0" (hanya menolak negatif),
        // berbeda dari pembelian kebutuhan yang menuntut "Amount harus > 0".
        //
        // Nilai nol dari GetHargaBahanEfektif juga bisa berarti bahan tidak ada di katalog, tetapi
        // kemungkinan itu sudah dihabisi IsKnownBahan di atas, yang memakai pencarian katalog yang
        // sama persis. Jadi nol yang sampai di sini pasti berarti gratis.
        int hargaBahan = gameState.GetHargaBahanEfektif(bahanKey);

        string spendBlockedMessage = gameState.GetSpendBlockedMessage(activePlayer, hargaBahan);
        if (spendBlockedMessage != null)
        {
            ShowSystemDialogThen("Tidak bisa membeli " + bahanName + ". " + spendBlockedMessage + "\n", () => view.ShowChoice("BahanMasakan"));
            return;
        }

        NarafinSessionOperationResult result;
        isSubmittingBahanMasakan = true;
        BeginServerWait("Mencatat pembelian " + bahanName + "...");
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

        await EndServerWaitAsync();

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
        PlayNarasiThen("BahanMasakan", resultText, UpdateMove);
    }
}
