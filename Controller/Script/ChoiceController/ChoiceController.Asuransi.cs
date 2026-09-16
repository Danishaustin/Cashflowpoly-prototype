using System;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    // Nilai coverage_type yang dipakai server untuk polis pembagian awal.
    private const string AsuransiCoverageType = "MULTIRISK";

    private bool isSubmittingAsuransi;

    private void HandleChoiceAsuransi()
    {
        if (isSubmittingAsuransi)
        {
            return;
        }

        if (GameState.Instance != null && !GameState.Instance.InsuranceEnabled)
        {
            ShowSystemDialogThen("Fitur asuransi tidak tersedia pada ruleset ini.\n", () => view.ShowChoice("Choice1"));
            return;
        }

        int player = GameState.Instance.turn;
        if (GameState.Instance.GetAsuransiDimiliki(player))
        {
            view.AddTextToDialog("Kamu masih memiliki polis asuransi aktif.\n");
            view.ShowChoice("Choice1");
            return;
        }

        _ = BeliAsuransiAsync(player);
    }

    // Produk pertama pada katalog ruleset session, sama dengan pembagian awal.
    private static NarafinSetupInsurance GetAsuransiProduct()
    {
        NarafinSetupInsurance product = NarafinActiveSession.GetFirstInsurance(NarafinActiveSession.Catalog);
        if (product != null)
        {
            return product;
        }

        return new NarafinSetupInsurance
        {
            product_code = "INS-001",
            item_name = "Asuransi",
            premium = 1,
            usage_limit = 1
        };
    }

    private async Task BeliAsuransiAsync(int player)
    {
        NarafinSetupInsurance product = GetAsuransiProduct();
        string itemName = string.IsNullOrWhiteSpace(product.item_name) ? "Asuransi" : product.item_name;
        int premium = product.premium;

        string spendBlockedMessage = GameState.Instance.GetSpendBlockedMessage(player, premium);
        if (spendBlockedMessage != null)
        {
            view.AddTextToDialog("Tidak bisa membeli asuransi. " + spendBlockedMessage + "\n");
            view.ShowChoice("Choice1");
            return;
        }

        string policyId = product.product_code + ":unity:" + Guid.NewGuid().ToString("N");

        NarafinSessionOperationResult result;
        isSubmittingAsuransi = true;
        BeginServerWait("Mencatat pembelian " + itemName + "...");
        try
        {
            result = await SendPlayerEventNowAsync(
                player,
                "Asuransi",
                BuildAsuransiPayload(policyId, product.product_code, premium, AsuransiCoverageType),
                GetCurrentActionSlot());
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal mengirim pembelian asuransi: " + ex.Message);
            result = CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
        }
        finally
        {
            isSubmittingAsuransi = false;
        }

        await EndServerWaitAsync();

        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            ShowSystemDialogThen(itemName + " ditolak: " + result.ErrorMessage + "\n", () => view.ShowChoice("Choice1"));
            return;
        }

        GameState.Instance.ChangeCoins(player, -premium);
        GameState.Instance.SetAsuransiPolicy(player, policyId, product.usage_limit);
        view.UpdateCoins(GameState.Instance.GetCoins(player));

        string resultText = "Membeli " + itemName + " seharga " + premium + " koin.\n";
        PlayNarasiThen("Asuransi", resultText, UpdateMove);
    }
}
