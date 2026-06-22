using UnityEngine;

public partial class ChoiceController
{
    private int initialBahanSelectionPlayer = 1;
    private string selectedInitialBahanName = string.Empty;

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

        if (!DataManager.Instance.bahanDict.TryGetValue(selectedInitialBahanName, out BahanMakananData bahanData))
        {
            Debug.LogWarning("Data bahan awal tidak ditemukan: " + selectedInitialBahanName);
            return;
        }

        int hargaBahan = Mathf.Max(0, bahanData.hargaBeli);
        int player = initialBahanSelectionPlayer;
        if (GameState.Instance.GetCoins(player) < hargaBahan)
        {
            Debug.LogWarning("Coin player tidak cukup untuk bahan awal: " + selectedInitialBahanName);
            return;
        }

        GameState.Instance.ChangeCoins(player, -hargaBahan);
        GameState.Instance.AddBahanToList(player, selectedInitialBahanName);

        if (initialBahanSelectionPlayer < GameState.Instance.playerCount)
        {
            initialBahanSelectionPlayer++;
            selectedInitialBahanName = string.Empty;
            GameState.Instance.SetTurnAndMoves(initialBahanSelectionPlayer, 2);
            view.UpdatePlayerTurn(GameState.Instance.turn);
            view.UpdatePlayerStats();
            view.ShowInitialBahanChoiceForPlayer(initialBahanSelectionPlayer);
            return;
        }

        initialBahanSelectionPlayer = 1;
        selectedInitialBahanName = string.Empty;
        GameState.Instance.SetTurnAndMoves(1, 2);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();
        view.TransitionInitialBahanToTargetKebutuhan();
    }
}
