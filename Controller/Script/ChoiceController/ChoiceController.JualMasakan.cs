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
            HandleChoiceJMLegacy(selectedChoice);
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

        _ = JualMasakanAsync(order);
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
        view.AddSystemTextToDialog("Mencatat penjualan " + orderName + "...");
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
        GameState.Instance.AddMasakanDijualToList(GameState.GetOrderLocalName(order));
        view.UpdateCoins(GameState.Instance.GetCoins(player));

        // Mode MAHIR: setiap penjualan wajib dirujuk tepat satu kartu Risiko Kehidupan sebelum hari ditutup.
        string saleEventId = result.EventId;
        Action onComplete = UseRisikoCatalog ? () => StartRisikoKehidupanServer(saleEventId) : (Action)UpdateMove;

        string resultText = "Menjual " + orderName + " menghasilkan " + order.hargaJual + " koin\n";
        int aksiKe = GameState.Instance.jmAksiKe;
        GameState.Instance.jmAksiKe++;

        if (PlayNpcStaticDialogThen("JualMasakan", resultText, onComplete))
        {
            return;
        }

        if (Narasi("JualMasakan", aksiKe, () =>
        {
            ShowSystemDialogThen(resultText, onComplete);
        }))
        {
            return;
        }

        ShowSystemDialogThen(resultText, onComplete);
    }

    // Alur lama tanpa katalog ruleset session (mode offline).
    private void HandleChoiceJMLegacy(string selectedChoice)
    {
        Debug.Log($"{selectedChoice} dijual");
        var listBahan = DataManager.Instance.resepDict[selectedChoice].bahan;
        var jumlahBahan = listBahan
            .GroupBy(b => b)
            .ToDictionary(g => g.Key, g => g.Count());

        var kurangBahan = GameState.Instance.HasBahan(jumlahBahan);

        if (kurangBahan.Count > 0)
        {
            Debug.Log("Bahan tidak cukup: " + string.Join(", ", kurangBahan));
            string errorText = "Bahan tidak cukup: " + string.Join(", ", kurangBahan) + "\n";
            ShowSystemDialogThen(errorText, () => view.ShowChoice("Choice1"));
            return;
        }

        foreach (var kv in jumlahBahan)
        {
            GameState.Instance.RemoveBahanFromList(kv.Key, kv.Value);
        }

        var amountCoins = DataManager.Instance.resepDict[selectedChoice].hargaJual;
        GameState.Instance.ChangeCoins(amountCoins);
        GameState.Instance.AddMasakanDijualToList(selectedChoice);
        var amountHappiness = DataManager.Instance.resepDict[selectedChoice].poinKebahagiaan;
        GameState.Instance.ChangeHappiness(amountHappiness);
        view.UpdateCoins(GameState.Instance.Coins);
        view.UpdateHappiness(GameState.Instance.Happiness);
        PostJualMasakanEvent(GameState.Instance.turn, selectedChoice, listBahan, amountCoins);

        string resultText = "Menjual " + selectedChoice + " menghasilkan " + amountCoins + " koin\n";
        int aksiKe = GameState.Instance.jmAksiKe;
        GameState.Instance.jmAksiKe++;
        Debug.Log("jmAksiKe: " + GameState.Instance.jmAksiKe);

        if (PlayNpcStaticDialogThen("JualMasakan", resultText, ShowRisikoKehidupanAfterJualMasakan))
        {
            return;
        }

        if (Narasi("JualMasakan", aksiKe, () =>
        {
            ShowSystemDialogThen(resultText, ShowRisikoKehidupanAfterJualMasakan);
        }))
        {
            return;
        }

        ShowSystemDialogThen(resultText, ShowRisikoKehidupanAfterJualMasakan);
    }
}
