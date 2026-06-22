using UnityEngine;

public partial class ChoiceController
{
    private int risikoCoinChange;
    private int risikoHargaChange;
    private bool risikoCoinDecisionActive;
    private int risikoCoinDecisionPlayer;
    private int risikoOriginalTurn;
    private int risikoOriginalMovesLeft;

    private void ShowRisikoKehidupanAfterJualMasakan()
    {
        risikoCoinChange = 0;
        risikoHargaChange = 0;
        risikoCoinDecisionActive = false;
        risikoCoinDecisionPlayer = 0;
        risikoOriginalTurn = GameState.Instance.turn;
        risikoOriginalMovesLeft = GameState.Instance.movesLeft;
        view.ResetRisikoKehidupanPanel();
        view.ShowChoice("RisikoKehidupan");
    }

    private void HandleChoiceRisikoKehidupan(string selectedChoice)
    {
        switch (selectedChoice)
        {
            case "DecreaseButtonRisikoCoin":
                risikoCoinChange--;
                view.UpdateRisikoCoinChangeText(risikoCoinChange);
                break;
            case "IncreaseButtonRisikoCoin":
                risikoCoinChange++;
                view.UpdateRisikoCoinChangeText(risikoCoinChange);
                break;
            case "DecreaseButtonRisikoHarga":
                risikoHargaChange--;
                view.UpdateRisikoHargaChangeText(risikoHargaChange);
                break;
            case "IncreaseButtonRisikoHarga":
                risikoHargaChange++;
                view.UpdateRisikoHargaChangeText(risikoHargaChange);
                break;
            case "NextButtonRisikoKehidupan":
                if (risikoCoinDecisionActive)
                {
                    HandleRisikoCoinDecisionNext();
                    break;
                }

                if (!view.IsRisikoKehidupanInputValid(risikoCoinChange, risikoHargaChange, out string warningText))
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
            if (risikoCoinChange < 0)
            {
                risikoOriginalTurn = GameState.Instance.turn;
                risikoOriginalMovesLeft = GameState.Instance.movesLeft;
                risikoCoinDecisionPlayer = GameState.Instance.turn;
                risikoCoinDecisionActive = true;
                ShowRisikoCoinDecisionForPlayer(risikoCoinDecisionPlayer);
                return;
            }

            GameState.Instance.ChangeCoins(risikoCoinChange);
            view.UpdateCoins(GameState.Instance.Coins);
        }

        view.HideChoiceContainer("ChoiceRisikoKehidupan");
        Debug.Log("Risiko Kehidupan panel pertama selesai. Coin change: " + risikoCoinChange + ", harga change: " + risikoHargaChange);
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
            GameState.Instance.ChangeCoins(player, risikoCoinChange);
            view.UpdateCoins(GameState.Instance.Coins);
        }

        if (risikoCoinDecisionPlayer < GameState.Instance.playerCount)
        {
            risikoCoinDecisionPlayer++;
            ShowRisikoCoinDecisionForPlayer(risikoCoinDecisionPlayer);
            return;
        }

        risikoCoinDecisionActive = false;
        risikoCoinDecisionPlayer = 0;
        view.HideRisikoKehidupanWarning();
        view.HideChoiceContainer("ChoiceRisikoKehidupan");
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

        bool canUseAsuransi = GameState.Instance.GetAsuransiDimiliki(player);
        bool canPayBank = GameState.Instance.GetCoins(player) + risikoCoinChange >= 0;
        view.ShowRisikoCoinDecisionContent(GetPlayerName(player), canUseAsuransi, canPayBank);
    }
}
