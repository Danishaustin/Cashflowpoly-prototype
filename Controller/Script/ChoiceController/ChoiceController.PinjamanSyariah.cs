using System;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    private bool isSubmittingPinjamanSyariah;

    // Handles taking and returning Pinjaman Syariah.
    private void HandleChoicePinjamanSyariah(string selectedChoice)
    {
        if (isSubmittingPinjamanSyariah)
        {
            return;
        }

        if (GameState.Instance != null && !GameState.Instance.LoanEnabled)
        {
            ShowSystemDialogThen("Fitur pinjaman syariah tidak tersedia pada ruleset ini.\n", () => view.ShowChoice("Choice1"));
            return;
        }

        switch (selectedChoice)
        {
            case "AmbilPinjamanSyariah":
                _ = AmbilPinjamanSyariahAsync();
                break;
            case "KembalikanPinjamanSyariah":
                _ = KembalikanPinjamanSyariahAsync();
                break;
            default:
                Debug.Log("Pilihan pinjaman syariah tidak valid");
                view.ShowChoice("Choice1");
                break;
        }
    }

    // Produk pinjaman pertama pada ruleset session, sama dengan pembagian awal.
    private static NarafinSetupLoan GetPinjamanSyariahProduct()
    {
        NarafinSetupLoan loan = NarafinActiveSession.GetFirstLoan(NarafinActiveSession.Catalog);
        if (loan != null)
        {
            return loan;
        }

        return new NarafinSetupLoan
        {
            loan_code = "LOAN-001",
            item_name = "Pinjaman Syariah",
            principal = 10,
            repayment_amount = 10,
            duration_days = 25,
            penalty_points = 15
        };
    }

    private async Task AmbilPinjamanSyariahAsync()
    {
        int player = GameState.Instance.turn;
        NarafinSetupLoan loan = GetPinjamanSyariahProduct();
        string itemName = string.IsNullOrWhiteSpace(loan.item_name) ? "Pinjaman Syariah" : loan.item_name;

        // Server tidak menolak loan_id ganda, jadi keunikan setiap kartu dijaga oleh klien.
        string loanId = loan.loan_code + ":unity:" + Guid.NewGuid().ToString("N");

        NarafinSessionOperationResult result = await SendPinjamanSyariahEventAsync(
            player,
            "PinjamanSyariah",
            BuildPinjamanSyariahPayload(loanId, loan),
            "Mencatat " + itemName + "...");

        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            ShowSystemDialogThen(itemName + " ditolak: " + result.ErrorMessage + "\n", () => view.ShowChoice("PinjamanSyariah"));
            return;
        }

        GameState.Instance.ChangeCoins(player, loan.principal);
        GameState.Instance.AddPinjamanSyariah(player, new PinjamanSyariahHolding
        {
            LoanId = loanId,
            LoanCode = loan.loan_code,
            ItemName = itemName,
            Principal = loan.principal,
            RepaymentAmount = loan.repayment_amount
        });
        view.UpdateCoins(GameState.Instance.GetCoins(player));

        string resultText = "Mengambil " + itemName + ". Mendapatkan " + loan.principal
            + " koin dan 1 kartu pinjaman syariah. Kartu saat ini: "
            + GameState.Instance.GetPinjamanSyariahCards(player) + "\n";
        PlayNarasiThen("PinjamanSyariah", resultText, UpdateMove);
    }

    // Pinjaman yang dikembalikan selalu yang paling lama diambil.
    private async Task KembalikanPinjamanSyariahAsync()
    {
        int player = GameState.Instance.turn;
        PinjamanSyariahHolding holding = GameState.Instance.GetOldestPinjamanSyariah(player);
        if (holding == null)
        {
            ShowSystemDialogThen("Tidak ada kartu pinjaman syariah yang dimiliki.\n", () => view.ShowChoice("Choice1"));
            return;
        }

        if (string.IsNullOrWhiteSpace(holding.LoanId) && !NarafinRuntimeConfig.UseOfflineMode)
        {
            ShowSystemDialogThen("Pinjaman ini belum tercatat di server sehingga belum bisa dikembalikan.\n", () => view.ShowChoice("Choice1"));
            return;
        }

        int amount = holding.RepaymentAmount > 0 ? holding.RepaymentAmount : holding.Principal;
        string spendBlockedMessage = GameState.Instance.GetSpendBlockedMessage(player, amount);
        if (spendBlockedMessage != null)
        {
            ShowSystemDialogThen("Tidak bisa mengembalikan pinjaman syariah. " + spendBlockedMessage + "\n", () => view.ShowChoice("Choice1"));
            return;
        }

        NarafinSessionOperationResult result = await SendPinjamanSyariahEventAsync(
            player,
            "BayarPinjaman",
            BuildBayarPinjamanPayload(holding.LoanId, amount),
            "Mencatat pengembalian " + holding.ItemName + "...");

        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            ShowSystemDialogThen("Pengembalian " + holding.ItemName + " ditolak: " + result.ErrorMessage + "\n", () => view.ShowChoice("PinjamanSyariah"));
            return;
        }

        GameState.Instance.ChangeCoins(player, -amount);
        GameState.Instance.RemovePinjamanSyariah(player, holding);
        view.UpdateCoins(GameState.Instance.GetCoins(player));

        string resultText = "Mengembalikan " + holding.ItemName + ". Membayar " + amount
            + " koin dan mengurangi 1 kartu pinjaman syariah. Kartu tersisa: "
            + GameState.Instance.GetPinjamanSyariahCards(player) + "\n";
        PlayNarasiThen("BayarPinjaman", resultText, UpdateMove);
    }

    private async Task<NarafinSessionOperationResult> SendPinjamanSyariahEventAsync(int player, string actionType, string payloadJson, string progressText)
    {
        isSubmittingPinjamanSyariah = true;
        BeginServerWait(progressText);

        NarafinSessionOperationResult result;
        try
        {
            result = await SendPlayerEventNowAsync(player, actionType, payloadJson, GetCurrentActionSlot());
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal mengirim event pinjaman syariah: " + ex.Message);
            result = CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
        }
        finally
        {
            isSubmittingPinjamanSyariah = false;
        }

        await EndServerWaitAsync();
        return result;
    }
}
