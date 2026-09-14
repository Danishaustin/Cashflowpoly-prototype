using System.Collections.Generic;
using UnityEngine;

public partial class GameState
{
    private readonly List<int> playerTurnOrder = new List<int>();

    public IReadOnlyList<int> PlayerTurnOrder => playerTurnOrder;

    public void ResetPlayerTurnOrder()
    {
        EnsureTurnOrderCapacity();
        playerTurnOrder.Clear();

        for (int player = 1; player <= playerCount; player++)
        {
            playerTurnOrder.Add(player);
        }
    }

    public void SetPlayerTurnOrder(IEnumerable<int> orderedPlayers)
    {
        EnsureTurnOrderCapacity();
        playerTurnOrder.Clear();

        var seen = new HashSet<int>();
        if (orderedPlayers != null)
        {
            foreach (int player in orderedPlayers)
            {
                if (player < 1 || player > playerCount || seen.Contains(player))
                {
                    continue;
                }

                playerTurnOrder.Add(player);
                seen.Add(player);
            }
        }

        for (int player = 1; player <= playerCount; player++)
        {
            if (seen.Contains(player))
            {
                continue;
            }

            playerTurnOrder.Add(player);
        }

        if (playerTurnOrder.Count == 0)
        {
            for (int player = 1; player <= playerCount; player++)
            {
                playerTurnOrder.Add(player);
            }
        }

        turn = GetFirstPlayerInTurnOrder();
        movesLeft = ActionsPerTurn;
    }

    public int GetFirstPlayerInTurnOrder()
    {
        EnsureTurnOrderCapacity();
        if (playerTurnOrder.Count == 0)
        {
            return 1;
        }

        return playerTurnOrder[0];
    }

    public int GetNextPlayerInTurnOrder(int currentPlayer)
    {
        EnsureTurnOrderCapacity();
        if (playerTurnOrder.Count == 0)
        {
            return currentPlayer >= playerCount ? 1 : currentPlayer + 1;
        }

        int currentIndex = playerTurnOrder.IndexOf(currentPlayer);
        if (currentIndex < 0)
        {
            return playerTurnOrder[0];
        }

        int nextIndex = currentIndex + 1;
        if (nextIndex >= playerTurnOrder.Count)
        {
            return playerTurnOrder[0];
        }

        return playerTurnOrder[nextIndex];
    }

    public bool IsLastPlayerInTurnOrder(int currentPlayer)
    {
        EnsureTurnOrderCapacity();
        if (playerTurnOrder.Count == 0)
        {
            return currentPlayer >= playerCount;
        }

        int currentIndex = playerTurnOrder.IndexOf(currentPlayer);
        return currentIndex >= 0 && currentIndex == playerTurnOrder.Count - 1;
    }

    public int GetTurnOrderPlayerAt(int index)
    {
        EnsureTurnOrderCapacity();
        if (playerTurnOrder.Count == 0)
        {
            return Mathf.Clamp(index + 1, 1, playerCount);
        }

        if (index < 0 || index >= playerTurnOrder.Count)
        {
            return playerTurnOrder[0];
        }

        return playerTurnOrder[index];
    }

    private void EnsureTurnOrderCapacity()
    {
        if (playerCount < 1)
        {
            playerCount = 1;
        }

        if (playerTurnOrder.Count > playerCount)
        {
            playerTurnOrder.RemoveRange(playerCount, playerTurnOrder.Count - playerCount);
        }

        if (playerTurnOrder.Count < playerCount)
        {
            var seen = new HashSet<int>(playerTurnOrder);
            for (int player = 1; player <= playerCount; player++)
            {
                if (!seen.Contains(player))
                {
                    playerTurnOrder.Add(player);
                    seen.Add(player);
                }
            }
        }
    }
}
