using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    private bool isSubmittingJualMasakan;

    private static bool UseOrderCatalog => NarafinActiveSession.GetOrders(NarafinActiveSession.Catalog).Count > 0;

    // Handles selling cooked recipes from ChoiceJM.
    private void HandleChoiceJM(string selectedChoice)
    {
        if (!UseOrderCatalog)
        {
            ShowSystemDialogThen(
                "Katalog pesanan tidak tersedia pada ruleset ini, jadi penjualan masakan tidak bisa dicatat.\n",
                () => view.ShowChoice("Choice1"));
            return;
        }

        if (isSubmittingJualMasakan)
        {
            return;
        }

        NarafinSetupOrder order = NarafinActiveSession.FindOrder(NarafinActiveSession.Catalog, selectedChoice);
        if (order == null)
        {
            ShowSystemDialogThen("Kartu pesanan tidak ditemukan.\n", () => view.ShowChoice("Choice1"));
            return;
        }

        AskConfirmation(
            "Jual " + GameState.GetOrderDisplayName(order) + " seharga " + order.hargaJual + " koin?",
            () => _ = JualMasakanAsync(order),
            () => view.ShowChoice("JualMasakan"));
    }

    // Kartu pesanan dari katalog ruleset session; koin dan bahan baru berubah setelah server menerima penjualan.
    private async Task JualMasakanAsync(NarafinSetupOrder order)
    {
        int player = GameState.Instance.turn;
        string orderName = GameState.GetOrderDisplayName(order);

        // Satu card_id per kartu bahan yang dipakai, jadi bahan yang sama bisa muncul lebih dari sekali.
        List<string> ingredientCardIds = new List<string>();
        if (order.bahan != null)
        {
            foreach (string bahan in order.bahan)
            {
                ingredientCardIds.Add(GameState.Instance.ResolveBahanKey(bahan));
            }
        }

        List<string> kurangBahan = new List<string>();
        foreach (IGrouping<string, string> bahan in ingredientCardIds.GroupBy(cardId => cardId))
        {
            if (GameState.Instance.GetBahanCount(player, bahan.Key) < bahan.Count())
            {
                kurangBahan.Add(GameState.Instance.GetBahanDisplayName(bahan.Key));
            }
        }

        if (ingredientCardIds.Count == 0 || kurangBahan.Count > 0)
        {
            string errorText = kurangBahan.Count > 0
                ? "Bahan tidak cukup: " + string.Join(", ", kurangBahan) + "\n"
                : "Bahan pesanan " + orderName + " tidak tersedia.\n";
            ShowSystemDialogThen(errorText, () => view.ShowChoice("Choice1"));
            return;
        }

        NarafinSessionOperationResult result;
        isSubmittingJualMasakan = true;
        BeginServerWait("Mencatat penjualan " + orderName + "...");
        try
        {
            result = await SendPlayerEventNowAsync(
                player,
                "JualMasakan",
                BuildJualMasakanPayload(order.id, ingredientCardIds, order.hargaJual),
                GetCurrentActionSlot());
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal mengirim penjualan masakan: " + ex.Message);
            result = CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
        }
        finally
        {
            isSubmittingJualMasakan = false;
        }

        await EndServerWaitAsync();

        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            ShowSystemDialogThen("Penjualan " + orderName + " ditolak: " + result.ErrorMessage + "\n", () => view.ShowChoice("Choice1"));
            return;
        }

        foreach (IGrouping<string, string> bahan in ingredientCardIds.GroupBy(cardId => cardId))
        {
            GameState.Instance.RemoveBahanFromList(bahan.Key, bahan.Count());
        }

        GameState.Instance.ChangeCoins(player, order.hargaJual);
        // Disimpan sebagai id pesanan katalog supaya prasyarat narasi tidak bergantung pada ejaan nama.
        GameState.Instance.AddMasakanDijualToList(order.id);
        view.UpdateCoins(GameState.Instance.GetCoins(player));

        // Mode MAHIR: setiap penjualan wajib dirujuk tepat satu kartu Risiko Kehidupan sebelum hari ditutup.
        string saleEventId = result.EventId;
        Action onComplete = UseRisikoCatalog ? () => StartRisikoKehidupanServer(saleEventId) : (Action)UpdateMove;

        string resultText = "Menjual " + orderName + " menghasilkan " + order.hargaJual + " koin\n";
        PlayNarasiThen("JualMasakan", resultText, onComplete);
    }

    // Alur lama tanpa katalog ruleset session (mode offline).
}
