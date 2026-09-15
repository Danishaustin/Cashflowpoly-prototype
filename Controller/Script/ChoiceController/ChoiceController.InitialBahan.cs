using System.Collections.Generic;
using UnityEngine;

public partial class ChoiceController
{
    private int initialBahanSelectionIndex = 0;
    private string selectedInitialBahanName = string.Empty;

    // Bahan awal dicatat per player dan baru masuk inventory setelah server menerima pembagian awal.
    private readonly Dictionary<int, string> initialBahanSelections = new Dictionary<int, string>();

    public void ResetInitialBahanSelection()
    {
        initialBahanSelectionIndex = 0;
        selectedInitialBahanName = string.Empty;
        initialBahanSelections.Clear();
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

        if (GameState.Instance == null || !GameState.Instance.IsKnownBahan(selectedInitialBahanName))
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

        initialBahanSelections[player] = selectedInitialBahanName;

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

    private List<string> GetInitialBahanNamesByPlayer(int playerCount)
    {
        List<string> bahanNames = new List<string>();
        for (int player = 1; player <= playerCount; player++)
        {
            bahanNames.Add(initialBahanSelections.TryGetValue(player, out string bahanName) ? bahanName : string.Empty);
        }

        return bahanNames;
    }

    private void ApplyInitialBahanSelections(bool deductLocalPrice)
    {
        foreach (KeyValuePair<int, string> selection in initialBahanSelections)
        {
            if (deductLocalPrice)
            {
                GameState.Instance.ChangeCoins(selection.Key, -GameState.Instance.GetHargaBahanEfektif(selection.Value));
            }

            GameState.Instance.AddBahanToList(selection.Key, selection.Value);
        }

        initialBahanSelections.Clear();
    }
}
