using UnityEngine;
using System.Collections.Generic;

public partial class GameState
{
    // Per-player Pinjaman Syariah card storage.
    private void InitializePinjamanSyariahCards()
    {
        playerPinjamanSyariahCards = new Dictionary<int, int>();

        for (int player = 1; player <= playerCount; player++)
        {
            playerPinjamanSyariahCards[player] = 1;
        }
    }

    private void EnsurePinjamanSyariahCards(int player)
    {
        if (playerPinjamanSyariahCards == null)
        {
            InitializePinjamanSyariahCards();
        }

        if (!playerPinjamanSyariahCards.ContainsKey(player))
        {
            playerPinjamanSyariahCards[player] = 1;
        }
    }

    public int GetPinjamanSyariahCards(int player)
    {
        EnsurePinjamanSyariahCards(player);
        return playerPinjamanSyariahCards[player];
    }

    public void ChangePinjamanSyariahCards(int amount)
    {
        EnsurePinjamanSyariahCards(turn);
        playerPinjamanSyariahCards[turn] = Mathf.Max(0, playerPinjamanSyariahCards[turn] + amount);
    }
}
