using UnityEngine;

public partial class ChoiceController
{
    private int initialBahanSelectionIndex = 0;
    private string selectedInitialBahanName = string.Empty;

    public void ResetInitialBahanSelection()
    {
        initialBahanSelectionIndex = 0;
        selectedInitialBahanName = string.Empty;
    }

    private int GetCurrentInitialBahanPlayer()
    {
        if (GameState.Instance == null)
        {
            return 1;
        }

        return GameState.Instance.GetTurnOrderPlayerAt(initialBahanSelectionIndex);
    }

    private void HandleChoiceInitialBahan(string selectedChoice)
    {
        if (string.IsNullOrEmpty(selectedChoice))
        {
            return;
        }

        if (selectedChoice.StartsWith("InitialBahanOption_"))
        {
            selectedInitialBahanName = selectedChoice.Replace("InitialBahanOption_", string.Empty);
            view.SetSelectedInitialBahanButton(selectedChoice);
            return;
        }

        if (selectedChoice != "InitialBahanNextButton")
        {
            return;
        }

        if (string.IsNullOrEmpty(selectedInitialBahanName))
        {
            return;
        }

        if (DataManager.Instance == null || DataManager.Instance.bahanDict == null)
        {
            Debug.LogWarning("Data bahan belum siap.");
            return;
        }

        if (!DataManager.Instance.bahanDict.ContainsKey(selectedInitialBahanName))
        {
            Debug.LogWarning("Data bahan awal tidak ditemukan: " + selectedInitialBahanName);
            return;
        }

        int hargaBahan = GameState.Instance.GetHargaBahanEfektif(selectedInitialBahanName);
        int player = GetCurrentInitialBahanPlayer();
        if (GameState.Instance.GetCoins(player) < hargaBahan)
        {
            Debug.LogWarning("Coin player tidak cukup untuk bahan awal: " + selectedInitialBahanName);
            return;
        }

        GameState.Instance.ChangeCoins(player, -hargaBahan);
        GameState.Instance.AddBahanToList(player, selectedInitialBahanName);

        if (initialBahanSelectionIndex < GameState.Instance.playerCount - 1)
        {
            initialBahanSelectionIndex++;
            selectedInitialBahanName = string.Empty;
            int nextPlayer = GetCurrentInitialBahanPlayer();
            GameState.Instance.SetTurnAndMoves(nextPlayer, GameState.Instance.ActionsPerTurn);
            view.UpdatePlayerTurn(GameState.Instance.turn);
            view.UpdatePlayerStats();
            view.ShowInitialBahanChoiceForPlayer(nextPlayer);
            return;
        }

        initialBahanSelectionIndex = 0;
        selectedInitialBahanName = string.Empty;
        GameState.Instance.SetTurnAndMoves(GameState.Instance.GetFirstPlayerInTurnOrder(), GameState.Instance.ActionsPerTurn);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();
        view.TransitionInitialBahanToTargetKebutuhan();
    }
}
