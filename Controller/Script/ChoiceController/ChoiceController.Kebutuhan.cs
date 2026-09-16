using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    private readonly List<NarafinSetupNeed> kebutuhanVariants = new List<NarafinSetupNeed>();
    private int kebutuhanVariantIndex;
    private bool isSubmittingKebutuhan;

    // Dipakai view agar tombol Back tidak menutup panel saat pembelian masih menunggu server.
    public bool IsSubmittingKebutuhan => isSubmittingKebutuhan;

    private static bool UseKebutuhanCatalog => NarafinActiveSession.GetNeeds(NarafinActiveSession.Catalog).Count > 0;

    // Handles selecting a kebutuhan family; varian kartu dipilih berdasarkan harga di ChoiceKJumlah.
    private void HandleChoiceK(string selectedChoice)
    {
        if (!UseKebutuhanCatalog)
        {
            HandleChoiceKLegacy(selectedChoice);
            return;
        }

        List<NarafinSetupNeed> variants = NarafinActiveSession.GetNeedVariants(NarafinActiveSession.Catalog, selectedChoice);
        if (variants.Count == 0)
        {
            ShowSystemDialogThen("Kebutuhan ini tidak ada di ruleset session.\n", () => view.ShowChoice("Kebutuhan"));
            return;
        }

        bool isPrimer = string.Equals(variants[0].tipe, "primer", StringComparison.OrdinalIgnoreCase);
        if (GameState.Instance.RequirePrimaryBeforeOthers && !isPrimer && !GameState.Instance.HasKebutuhanPrimer(GameState.Instance.turn))
        {
            view.AddTextToDialog("Harus membeli kebutuhan primer dulu.\n");
            view.ShowChoice("Kebutuhan");
            return;
        }

        GameState.Instance.SetKebutuhanSelected(selectedChoice);
        kebutuhanVariants.Clear();
        kebutuhanVariants.AddRange(variants);
        kebutuhanVariantIndex = 0;
        view.ShowChoice("ChoiceKJumlah");
        UpdateKebutuhanVariantView();
    }

    private void HandleChoiceKJumlah(string selectedChoice)
    {
        if (!UseKebutuhanCatalog)
        {
            HandleChoiceKJumlahLegacy(selectedChoice);
            return;
        }

        if (isSubmittingKebutuhan || kebutuhanVariants.Count == 0)
        {
            return;
        }

        switch (selectedChoice)
        {
            case "MinButtonK":
                kebutuhanVariantIndex = 0;
                break;
            case "MaxButtonK":
                kebutuhanVariantIndex = kebutuhanVariants.Count - 1;
                break;
            case "DecreaseButtonK":
                kebutuhanVariantIndex = Mathf.Max(0, kebutuhanVariantIndex - 1);
                break;
            case "IncreaseButtonK":
                kebutuhanVariantIndex = Mathf.Min(kebutuhanVariants.Count - 1, kebutuhanVariantIndex + 1);
                break;
            case "ConfirmButtonK":
                _ = BuyKebutuhanAsync(kebutuhanVariants[kebutuhanVariantIndex]);
                return;
            default:
                Debug.Log("Pilihan tidak valid");
                break;
        }

        UpdateKebutuhanVariantView();
    }

    private void UpdateKebutuhanVariantView()
    {
        NarafinSetupNeed need = kebutuhanVariants[kebutuhanVariantIndex];
        view.UpdateKebutuhanVariantText(need.nama, need.hargaBeli, need.poinKebahagiaan);
    }

    private async Task BuyKebutuhanAsync(NarafinSetupNeed need)
    {
        int player = GameState.Instance.turn;
        string kebutuhanName = string.IsNullOrWhiteSpace(need.nama) ? need.id : need.nama;

        string spendBlockedMessage = GameState.Instance.GetSpendBlockedMessage(player, need.hargaBeli);
        if (spendBlockedMessage != null)
        {
            ShowSystemDialogThen("Tidak bisa membeli " + kebutuhanName + " seharga " + need.hargaBeli + " koin. " + spendBlockedMessage + "\n", () => view.ShowChoice("Kebutuhan"));
            return;
        }

        NarafinSessionOperationResult result;
        isSubmittingKebutuhan = true;
        view.AddSystemTextToDialog("Mencatat pembelian " + kebutuhanName + "...");
        try
        {
            result = await SendPlayerEventNowAsync(player, "Kebutuhan", BuildKebutuhanPayload(need), GetCurrentActionSlot());
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal mengirim pembelian kebutuhan: " + ex.Message);
            result = CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
        }
        finally
        {
            isSubmittingKebutuhan = false;
        }

        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            ShowSystemDialogThen("Pembelian " + kebutuhanName + " ditolak: " + result.ErrorMessage + "\n", () => view.ShowChoice("Kebutuhan"));
            return;
        }

        GameState.Instance.AddKebutuhanToList(player, need.id, need.tipe);
        GameState.Instance.ChangeCoins(player, -need.hargaBeli);
        GameState.Instance.SetHappiness(player, GameState.Instance.GetHappiness(player) + need.poinKebahagiaan);
        view.UpdateCoins(GameState.Instance.GetCoins(player));
        view.UpdateHappiness(GameState.Instance.GetHappiness(player));

        string resultText = "Membeli kebutuhan " + kebutuhanName + " seharga " + need.hargaBeli + " koin dengan poin kebahagiaan " + need.poinKebahagiaan + "\n";
        int aksiKe = GameState.Instance.kAksiKe;
        GameState.Instance.kAksiKe++;
        Debug.Log("kAksiKe: " + GameState.Instance.kAksiKe);

        if (PlayNpcStaticDialogThen("Kebutuhan", resultText, UpdateMove))
        {
            return;
        }

        if (Narasi("Kebutuhan", aksiKe, () =>
        {
            ShowSystemDialogThen(resultText, UpdateMove);
        }))
        {
            return;
        }

        ShowSystemDialogThen(resultText, UpdateMove);
    }

    // Alur lama tanpa katalog ruleset session (mode offline): harga diisi manual dari data lokal.
    private void HandleChoiceKLegacy(string selectedChoice)
    {
        Debug.Log($"{selectedChoice} dipilih");
        if (DataManager.Instance != null && DataManager.Instance.kebutuhanDict != null
            && DataManager.Instance.kebutuhanDict.TryGetValue(selectedChoice, out KebutuhanData kebutuhanData))
        {
            bool isPrimer = string.Equals(kebutuhanData.tipe, "primer", StringComparison.OrdinalIgnoreCase);
            bool hasPrimer = GameState.Instance.HasKebutuhanPrimer(GameState.Instance.turn);
            if (GameState.Instance.RequirePrimaryBeforeOthers && !isPrimer && !hasPrimer)
            {
                view.AddTextToDialog("Harus membeli kebutuhan primer dulu.\n");
                view.ShowChoice("Kebutuhan");
                return;
            }
        }

        GameState.Instance.SetKebutuhanSelected(selectedChoice);
        view.ShowChoice("ChoiceKJumlah");
        GameState.Instance.SetSavingText(0);
        view.UpdateKebutuhanText(GameState.Instance.SavingText);
    }

    private void HandleChoiceKJumlahLegacy(string selectedChoice)
    {
        Debug.Log($"{selectedChoice} dipilih");

        switch (selectedChoice)
        {
            case "MaxButtonK":
                GameState.Instance.SetSavingText(GameState.Instance.Coins);
                break;
            case "MinButtonK":
                GameState.Instance.SetSavingText(0);
                break;
            case "IncreaseButtonK":
                if (GameState.Instance.SavingText < GameState.Instance.Coins)
                {
                    GameState.Instance.ChangeSavingText(1);
                }
                break;
            case "DecreaseButtonK":
                if (GameState.Instance.SavingText > 0)
                {
                    GameState.Instance.ChangeSavingText(-1);
                }
                break;
            case "ConfirmButtonK":
                if (GameState.Instance.SavingText <= 0 || GameState.Instance.SavingText > GameState.Instance.Coins)
                {
                    Debug.Log("Jumlah tidak valid");
                    view.AddTextToDialog("Jumlah tidak valid\n");
                    view.ShowChoice("Choice1");
                    return;
                }

                var tipeKebutuhan = DataManager.Instance.kebutuhanDict[GameState.Instance.kebutuhanSelected].tipe;
                GameState.Instance.AddKebutuhanToList(GameState.Instance.kebutuhanSelected, tipeKebutuhan);
                GameState.Instance.ChangeCoins(-GameState.Instance.SavingText);
                GameState.Instance.ChangeHappiness(GameState.Instance.SavingText - 1);
                view.UpdateCoins(GameState.Instance.Coins);
                view.UpdateHappiness(GameState.Instance.Happiness);
                PostKebutuhanEvent(GameState.Instance.turn, GameState.Instance.kebutuhanSelected, GameState.Instance.SavingText, GameState.Instance.SavingText - 1);

                string resultText = "Membeli kebutuhan " + GameState.Instance.kebutuhanSelected + " seharga " + GameState.Instance.SavingText + " koin dengan poin kebahagiaan " + (GameState.Instance.SavingText - 1) + "\n";
                int aksiKe = GameState.Instance.kAksiKe;
                GameState.Instance.kAksiKe++;
                Debug.Log("kAksiKe: " + GameState.Instance.kAksiKe);

                if (PlayNpcStaticDialogThen("Kebutuhan", resultText, UpdateMove))
                {
                    return;
                }

                if (Narasi("Kebutuhan", aksiKe, () =>
                {
                    ShowSystemDialogThen(resultText, UpdateMove);
                }))
                {
                    return;
                }

                ShowSystemDialogThen(resultText, UpdateMove);
                break;
            default:
                Debug.Log("Pilihan tidak valid");
                view.AddTextToDialog("Pilihan tidak valid\n");
                break;
        }

        Debug.Log("SavingText: " + GameState.Instance.SavingText.ToString());
        view.UpdateKebutuhanText(GameState.Instance.SavingText);
    }
}
