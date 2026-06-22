using UnityEngine;
using System.Collections.Generic;

public partial class GameState
{
    // Per-player gold storage and Saturday gold-event progression.
    private void InitializeInvestasiEmas()
    {
        playerEmas = new Dictionary<int, int>();

        for (int player = 1; player <= playerCount; player++)
        {
            playerEmas[player] = 1;
        }
    }

    private void EnsureInvestasiEmasPlayer(int player)
    {
        if (playerEmas == null)
        {
            InitializeInvestasiEmas();
        }

        if (!playerEmas.ContainsKey(player))
        {
            playerEmas[player] = 1;
        }
    }

    public int GetEmas(int player)
    {
        EnsureInvestasiEmasPlayer(player);
        return playerEmas[player];
    }

    public void ChangeEmas(int amount)
    {
        EnsureInvestasiEmasPlayer(turn);
        playerEmas[turn] = Mathf.Max(0, playerEmas[turn] + amount);
    }

    public bool AdvanceInvestasiEmasTurn()
    {
        if (turn < playerCount)
        {
            turn++;
            movesLeft = 2;
            return false;
        }

        NextDay();
        return true;
    }
}
