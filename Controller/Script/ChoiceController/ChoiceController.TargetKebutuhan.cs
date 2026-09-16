using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    private readonly List<string> targetKebutuhanSelectionOrder = new();
    private bool isSubmittingOpeningSetup;

    public void ResetTargetKebutuhanSelection()
    {
        targetKebutuhanSelectionOrder.Clear();
    }

    private void HandleChoiceTargetKebutuhan(string selectedChoice)
    {
        if (isSubmittingOpeningSetup)
        {
            return;
        }

        int playerCount = GameState.Instance != null ? GameState.Instance.playerCount : 3;

        if (selectedChoice == "TargetKebutuhanResetButton")
        {
            targetKebutuhanSelectionOrder.Clear();
            view.RefreshTargetKebutuhanSelectionUI(targetKebutuhanSelectionOrder, playerCount);
            return;
        }

        if (selectedChoice == "TargetKebutuhanNextButton")
        {
            if (targetKebutuhanSelectionOrder.Count != playerCount)
            {
                return;
            }

            for (int player = 1; player <= playerCount; player++)
            {
                GameState.Instance.SetTargetKebutuhanId(player, string.Empty);
            }

            for (int player = 1; player <= targetKebutuhanSelectionOrder.Count; player++)
            {
                GameState.Instance.SetTargetKebutuhanId(player, targetKebutuhanSelectionOrder[player - 1]);
            }

            _ = SubmitOpeningSetupAsync(new List<string>(targetKebutuhanSelectionOrder));
            return;
        }

        if (!view.IsTargetKebutuhanOption(selectedChoice))
        {
            Debug.LogWarning("Target kebutuhan tidak ditemukan: " + selectedChoice);
            return;
        }

        if (targetKebutuhanSelectionOrder.Contains(selectedChoice))
        {
            return;
        }

        if (targetKebutuhanSelectionOrder.Count >= playerCount)
        {
            return;
        }

        targetKebutuhanSelectionOrder.Add(selectedChoice);
        view.RefreshTargetKebutuhanSelectionUI(targetKebutuhanSelectionOrder, playerCount);
    }

    // Mengirim pembagian awal lalu memulai session; state lokal baru diubah setelah server menerima.
    private async Task SubmitOpeningSetupAsync(List<string> missionIds)
    {
        int playerCount = GameState.Instance.playerCount;
        isSubmittingOpeningSetup = true;
        view.SetOpeningSetupSubmitting(true);

        NarafinSetupStartResult result;
        try
        {
            result = LoginManager.Instance != null
                ? await LoginManager.Instance.SaveSetupAndStartPlaySessionAsync(GetInitialBahanNamesByPlayer(playerCount), missionIds)
                : new NarafinSetupStartResult { Success = false, ErrorMessage = "Sistem login belum siap." };
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal menyimpan pembagian awal: " + ex.Message);
            result = new NarafinSetupStartResult { Success = false, ErrorMessage = "Gagal menyimpan pembagian awal. Silakan coba lagi." };
        }

        isSubmittingOpeningSetup = false;
        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            Debug.LogWarning("Pembagian awal gagal: " + result.ErrorCode + " - " + result.ErrorMessage);
            view.RefreshTargetKebutuhanSelectionUI(targetKebutuhanSelectionOrder, playerCount);
            view.ShowOpeningSetupError(result.ErrorMessage);
            return;
        }

        bool hasServerPlayers = result.Players != null && result.Players.Count > 0;
        ApplyInitialBahanSelections(!hasServerPlayers);
        if (hasServerPlayers)
        {
            ApplyServerCoins(result.Players);
            ApplyServerSetupAssets(result.Players, result.SetupEvents);
        }

        targetKebutuhanSelectionOrder.Clear();
        GameState.Instance.SetTurnAndMoves(1, GameState.Instance.ActionsPerTurn);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();
        view.CompleteOpeningSetup();
        view.ShowPlayerContainer();
    }

    // Koin awal di server sudah memperhitungkan modal, pinjaman setup, dan harga bahan awal.
    private void ApplyServerCoins(IEnumerable<NarafinSessionStatePlayer> serverPlayers)
    {
        foreach (NarafinSessionStatePlayer serverPlayer in serverPlayers)
        {
            if (serverPlayer == null || serverPlayer.player_order_no < 1 || serverPlayer.player_order_no > GameState.Instance.playerCount)
            {
                continue;
            }

            GameState.Instance.SetCoins(serverPlayer.player_order_no, serverPlayer.coins);
        }
    }

    // Pinjaman dan polis awal dibaca dari event setup server agar loan_id dan policy_id asli bisa dipakai.
    private void ApplyServerSetupAssets(IEnumerable<NarafinSessionStatePlayer> serverPlayers, IEnumerable<NarafinSessionEventSummary> setupEvents)
    {
        if (setupEvents == null)
        {
            Debug.LogWarning("Event setup tidak tersedia; pinjaman dan asuransi awal memakai data lokal.");
            return;
        }

        foreach (NarafinSessionStatePlayer serverPlayer in serverPlayers)
        {
            if (serverPlayer == null || serverPlayer.player_order_no < 1 || serverPlayer.player_order_no > GameState.Instance.playerCount)
            {
                continue;
            }

            List<PinjamanSyariahHolding> loans = new List<PinjamanSyariahHolding>();
            string policyId = string.Empty;
            int policyUsageLimit = 0;

            foreach (NarafinSessionEventSummary setupEvent in setupEvents)
            {
                if (setupEvent?.payload == null
                    || !string.Equals(setupEvent.user_id, serverPlayer.user_id, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (setupEvent.action_type == "SetupPinjamanAwal" && !string.IsNullOrWhiteSpace(setupEvent.payload.loan_id))
                {
                    loans.Add(new PinjamanSyariahHolding
                    {
                        LoanId = setupEvent.payload.loan_id,
                        LoanCode = setupEvent.payload.loan_code,
                        ItemName = GetPinjamanSyariahItemName(setupEvent.payload.loan_code),
                        Principal = setupEvent.payload.principal,
                        RepaymentAmount = setupEvent.payload.repayment_amount
                    });
                }
                else if (setupEvent.action_type == "SetupAsuransiAwal" && !string.IsNullOrWhiteSpace(setupEvent.payload.policy_id))
                {
                    policyId = setupEvent.payload.policy_id;
                    policyUsageLimit = GetAsuransiUsageLimit(setupEvent.payload.product_code);
                }
            }

            GameState.Instance.SetPinjamanSyariahList(serverPlayer.player_order_no, loans);
            GameState.Instance.SetAsuransiPolicy(serverPlayer.player_order_no, policyId, policyUsageLimit);
        }
    }

    private static int GetAsuransiUsageLimit(string productCode)
    {
        NarafinRulesetSetupDefinition catalog = NarafinActiveSession.Catalog;
        if (catalog?.insurance_products != null)
        {
            foreach (NarafinSetupInsurance product in catalog.insurance_products)
            {
                if (product != null && string.Equals(product.product_code, productCode, StringComparison.Ordinal))
                {
                    return Mathf.Max(1, product.usage_limit);
                }
            }
        }

        return 1;
    }

    private static string GetPinjamanSyariahItemName(string loanCode)
    {
        NarafinRulesetSetupDefinition catalog = NarafinActiveSession.Catalog;
        if (catalog?.sharia_loans != null)
        {
            foreach (NarafinSetupLoan loan in catalog.sharia_loans)
            {
                if (loan != null
                    && string.Equals(loan.loan_code, loanCode, StringComparison.Ordinal)
                    && !string.IsNullOrWhiteSpace(loan.item_name))
                {
                    return loan.item_name;
                }
            }
        }

        return "Pinjaman Syariah";
    }
}
