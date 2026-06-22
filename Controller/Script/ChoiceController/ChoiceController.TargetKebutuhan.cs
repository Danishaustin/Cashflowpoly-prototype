using System.Collections.Generic;
using UnityEngine;

public partial class ChoiceController
{
    private readonly List<string> targetKebutuhanSelectionOrder = new();

    public void ResetTargetKebutuhanSelection()
    {
        targetKebutuhanSelectionOrder.Clear();
    }

    private void HandleChoiceTargetKebutuhan(string selectedChoice)
    {
        if (DataManager.Instance == null || DataManager.Instance.targetKebutuhanDict == null)
        {
            Debug.LogWarning("Data target kebutuhan belum siap.");
            view.ShowPlayerContainer();
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

            targetKebutuhanSelectionOrder.Clear();
            GameState.Instance.SetTurnAndMoves(1, 2);
            view.UpdatePlayerTurn(GameState.Instance.turn);
            view.UpdatePlayerStats();
            view.ShowPlayerContainer();
            return;
        }

        if (!DataManager.Instance.targetKebutuhanDict.ContainsKey(selectedChoice))
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
}
