using UnityEngine;

public partial class ChoiceController
{
    private enum RisikoCoinDecisionMode
    {
        None,
        BayarBank,
        DapatHadiahDariTiapPemain
    }

    private int risikoBayarBankAmount;
    private int risikoDapatCoinAmount;
    private int risikoHargaChange;
    private bool risikoCoinDecisionActive;
    private bool risikoCoinDecisionAllPlayers;
    private RisikoCoinDecisionMode risikoCoinDecisionMode;
    private int risikoCoinDecisionPlayer;
    private int risikoCoinDecisionActorPlayer;
    private int risikoOriginalTurn;
    private int risikoOriginalMovesLeft;

    private void ShowRisikoKehidupanAfterJualMasakan()
    {
        risikoBayarBankAmount = 1;
        risikoDapatCoinAmount = 1;
        risikoHargaChange = 0;
        risikoCoinDecisionActive = false;
        risikoCoinDecisionAllPlayers = false;
        risikoCoinDecisionMode = RisikoCoinDecisionMode.None;
        risikoCoinDecisionPlayer = 0;
        risikoCoinDecisionActorPlayer = 0;
        risikoOriginalTurn = GameState.Instance.turn;
        risikoOriginalMovesLeft = GameState.Instance.movesLeft;
        view.ResetRisikoKehidupanPanel();
        view.UpdateRisikoCoinChangeText(risikoBayarBankAmount);
        view.UpdateRisikoDapatCoinText(risikoDapatCoinAmount);
        view.UpdateRisikoHargaChangeText(risikoHargaChange);
        view.ShowChoice("RisikoKehidupan");
    }

    private void HandleChoiceRisikoKehidupan(string selectedChoice)
    {
        switch (selectedChoice)
        {
            case "DecreaseButtonRisikoCoin":
                if (risikoBayarBankAmount > 1)
                {
                    risikoBayarBankAmount--;
                    view.UpdateRisikoCoinChangeText(risikoBayarBankAmount);
                }
                break;
            case "IncreaseButtonRisikoCoin":
                risikoBayarBankAmount++;
                view.UpdateRisikoCoinChangeText(risikoBayarBankAmount);
                break;
            case "DecreaseButtonRisikoDapatCoin":
                if (risikoDapatCoinAmount > 1)
                {
                    risikoDapatCoinAmount--;
                    view.UpdateRisikoDapatCoinText(risikoDapatCoinAmount);
                }
                break;
            case "IncreaseButtonRisikoDapatCoin":
                risikoDapatCoinAmount++;
                view.UpdateRisikoDapatCoinText(risikoDapatCoinAmount);
                break;
            case "DecreaseButtonRisikoHarga":
                risikoHargaChange = AdjustNonZeroValue(risikoHargaChange, -1);
                view.UpdateRisikoHargaChangeText(risikoHargaChange);
                break;
            case "IncreaseButtonRisikoHarga":
                risikoHargaChange = AdjustNonZeroValue(risikoHargaChange, 1);
                view.UpdateRisikoHargaChangeText(risikoHargaChange);
                break;
            case "DecreaseButtonRisikoJualEmas":
                view.ChangeRisikoJualEmasAmount(-1);
                break;
            case "IncreaseButtonRisikoJualEmas":
                view.ChangeRisikoJualEmasAmount(1);
                break;
            case "NextButtonRisikoKehidupan":
                if (risikoCoinDecisionActive)
                {
                    HandleRisikoCoinDecisionNext();
                    break;
                }

                if (!view.IsRisikoKehidupanInputValid(risikoBayarBankAmount, risikoDapatCoinAmount, risikoHargaChange, out string warningText))
                {
                    view.ShowRisikoKehidupanWarning(warningText);
                    break;
                }

                view.HideRisikoKehidupanWarning();
                HandleRisikoSetupNext();
                break;
            default:
                Debug.Log("Pilihan Risiko Kehidupan tidak valid: " + selectedChoice);
                break;
        }
    }

    private void HandleRisikoSetupNext()
    {
        if (view.IsRisikoInvestasiEmasSelected())
        {
            view.HideChoiceContainer("ChoiceRisikoKehidupan");
            ShowInvestasiEmasHargaInputFromRisiko();
            return;
        }

        if (view.IsRisikoCoinSelected())
        {
            StartRisikoCoinDecision(RisikoCoinDecisionMode.BayarBank, view.IsRisikoSemuaPemainSelected());
            return;
        }

        if (view.IsRisikoDapatCoinSelected())
        {
            if (view.IsRisikoDariTiapPemainSelected())
            {
                StartRisikoCoinDecision(RisikoCoinDecisionMode.DapatHadiahDariTiapPemain, true);
                return;
            }

            ApplyRisikoDapatHadiahInstant();
        }

        if (view.IsRisikoPerubahanHargaSelected())
        {
            GameState.Instance.SetMingguanPerubahanHargaBahan(risikoHargaChange);
        }

        view.HideChoiceContainer("ChoiceRisikoKehidupan");
        Debug.Log("Risiko Kehidupan panel pertama selesai. Bayar ke bank: " + risikoBayarBankAmount + ", dapat hadiah: " + risikoDapatCoinAmount + ", harga change: " + risikoHargaChange);
        PostCurrentRisikoKehidupanEvent(risikoOriginalTurn);
        UpdateMove();
    }

    private void HandleRisikoCoinDecisionNext()
    {
        if (!view.IsRisikoCoinDecisionInputValid(out string warningText))
        {
            view.ShowRisikoKehidupanWarning(warningText);
            return;
        }

        int player = risikoCoinDecisionPlayer;

        if (view.IsRisikoUseAsuransiSelected())
        {
            GameState.Instance.SetAsuransiDimiliki(player, false);
        }
        else if (view.IsRisikoBayarBankSelected())
        {
            ApplyRisikoBankOrHadiahPayment(player);
        }
        else if (view.IsRisikoJualEmasSelected())
        {
            if (!view.IsRisikoJualEmasInputValid(GameState.Instance.GetEmas(player), out string jualEmasWarning))
            {
                view.ShowRisikoKehidupanWarning(jualEmasWarning);
                return;
            }

            int jualEmasAmount = view.GetRisikoJualEmasAmount();
            int hargaEmasAcuan = GetHargaEmasAcuanRisiko();
            int income = jualEmasAmount * hargaEmasAcuan;
            GameState.Instance.ChangeCoins(player, income);
            GameState.Instance.ChangeEmas(-jualEmasAmount);
            ApplyRisikoBankOrHadiahPayment(player);
        }
        else if (view.IsRisikoPinjamanSyariahSelected())
        {
            GameState.Instance.ChangePinjamanSyariahCards(1);
            GameState.Instance.ChangeCoins(player, 10);
            ApplyRisikoBankOrHadiahPayment(player);
        }

        int nextPlayer = GetNextRisikoDecisionPlayer(risikoCoinDecisionPlayer);
        if (nextPlayer != 0)
        {
            risikoCoinDecisionPlayer = nextPlayer;
            ShowRisikoCoinDecisionForPlayer(risikoCoinDecisionPlayer);
            return;
        }

        risikoCoinDecisionActive = false;
        risikoCoinDecisionMode = RisikoCoinDecisionMode.None;
        risikoCoinDecisionPlayer = 0;
        view.HideRisikoKehidupanWarning();
        view.HideChoiceContainer("ChoiceRisikoKehidupan");
        PostCurrentRisikoKehidupanEvent(risikoCoinDecisionActorPlayer);
        GameState.Instance.SetTurnAndMoves(risikoOriginalTurn, risikoOriginalMovesLeft);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();
        UpdateMove();
    }

    private void ShowRisikoCoinDecisionForPlayer(int player)
    {
        GameState.Instance.SetTurnAndMoves(player, GameState.Instance.movesLeft);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();

        bool canUseAsuransi = GameState.Instance.InsuranceEnabled
                              && GameState.Instance.GetAsuransiDimiliki(player)
                              && risikoCoinDecisionMode != RisikoCoinDecisionMode.DapatHadiahDariTiapPemain;
        bool canPayBank = GameState.Instance.GetCoins(player) >= GetRisikoDecisionAmount();
        bool hasKebutuhan = HasAnyKebutuhan(player);
        bool hasEmas = GameState.Instance.GetEmas(player) > 0;
        bool canJualKebutuhan = !canPayBank && hasKebutuhan;
        bool canJualEmas = !canPayBank && hasEmas && GameState.Instance.GoldTradeAllowSell;
        bool canPinjamanSyariah = !canPayBank && GameState.Instance.LoanEnabled;

        view.ShowRisikoCoinDecisionContent(
            GetPlayerName(player),
            canUseAsuransi,
            canPayBank,
            canJualKebutuhan,
            canJualEmas,
            canPinjamanSyariah,
            GameState.Instance.GetEmas(player));
    }

    private bool HasAnyKebutuhan(int player)
    {
        var kebutuhanByTipe = GameState.Instance.GetKebutuhanList(player);
        if (kebutuhanByTipe == null)
        {
            return false;
        }

        foreach (var entry in kebutuhanByTipe)
        {
            if (entry.Value != null && entry.Value.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyRisikoDapatHadiahInstant()
    {
        int actorPlayer = GameState.Instance.turn;
        GameState.Instance.ChangeCoins(actorPlayer, risikoDapatCoinAmount);
        view.UpdatePlayerStats();
    }

    private void StartRisikoCoinDecision(RisikoCoinDecisionMode mode, bool allPlayers)
    {
        risikoOriginalTurn = GameState.Instance.turn;
        risikoOriginalMovesLeft = GameState.Instance.movesLeft;
        risikoCoinDecisionActorPlayer = GameState.Instance.turn;
        risikoCoinDecisionAllPlayers = allPlayers;
        risikoCoinDecisionMode = mode;
        risikoCoinDecisionActive = true;

        if (!risikoCoinDecisionAllPlayers)
        {
            risikoCoinDecisionPlayer = risikoCoinDecisionActorPlayer;
            ShowRisikoCoinDecisionForPlayer(risikoCoinDecisionPlayer);
            return;
        }

        int firstPlayer = GetFirstRisikoDecisionPlayer();
        if (firstPlayer == 0)
        {
            risikoCoinDecisionActive = false;
            risikoCoinDecisionMode = RisikoCoinDecisionMode.None;
            view.HideChoiceContainer("ChoiceRisikoKehidupan");
            GameState.Instance.SetTurnAndMoves(risikoOriginalTurn, risikoOriginalMovesLeft);
            view.UpdatePlayerTurn(GameState.Instance.turn);
            view.UpdatePlayerStats();
            UpdateMove();
            return;
        }

        risikoCoinDecisionPlayer = firstPlayer;
        ShowRisikoCoinDecisionForPlayer(risikoCoinDecisionPlayer);
    }

    private void ApplyRisikoBankOrHadiahPayment(int payerPlayer)
    {
        int amount = GetRisikoDecisionAmount();
        if (amount <= 0)
        {
            return;
        }

        GameState.Instance.ChangeCoins(payerPlayer, -amount);

        if (risikoCoinDecisionMode == RisikoCoinDecisionMode.DapatHadiahDariTiapPemain)
        {
            GameState.Instance.ChangeCoins(risikoCoinDecisionActorPlayer, amount);
        }

        view.UpdatePlayerStats();
    }

    private int GetRisikoDecisionAmount()
    {
        return risikoCoinDecisionMode == RisikoCoinDecisionMode.DapatHadiahDariTiapPemain
            ? risikoDapatCoinAmount
            : risikoBayarBankAmount;
    }

    private int GetFirstRisikoDecisionPlayer()
    {
        for (int player = 1; player <= GameState.Instance.playerCount; player++)
        {
            if (IsRisikoDecisionPlayerEligible(player))
            {
                return player;
            }
        }

        return 0;
    }

    private int GetNextRisikoDecisionPlayer(int currentPlayer)
    {
        if (!risikoCoinDecisionAllPlayers)
        {
            return 0;
        }

        for (int player = currentPlayer + 1; player <= GameState.Instance.playerCount; player++)
        {
            if (IsRisikoDecisionPlayerEligible(player))
            {
                return player;
            }
        }

        return 0;
    }

    private bool IsRisikoDecisionPlayerEligible(int player)
    {
        if (player < 1 || player > GameState.Instance.playerCount)
        {
            return false;
        }

        if (risikoCoinDecisionMode == RisikoCoinDecisionMode.DapatHadiahDariTiapPemain
            && player == risikoCoinDecisionActorPlayer)
        {
            return false;
        }

        return true;
    }

    private int AdjustNonZeroValue(int current, int delta)
    {
        int next = current + delta;
        if (next == 0)
        {
            next += delta > 0 ? 1 : -1;
        }

        return next;
    }

    private void PostCurrentRisikoKehidupanEvent(int player)
    {
        PostRisikoKehidupanEvent(
            player,
            view.IsRisikoInvestasiEmasSelected(),
            view.IsRisikoCoinSelected(),
            view.IsRisikoDapatCoinSelected(),
            view.IsRisikoPerubahanHargaSelected(),
            risikoBayarBankAmount,
            risikoDapatCoinAmount,
            risikoHargaChange);
    }

    private int GetHargaEmasAcuanRisiko()
    {
        return GameState.Instance.HargaEmasSaatIni > 0
            ? GameState.Instance.HargaEmasSaatIni
            : 5;
    }
}
