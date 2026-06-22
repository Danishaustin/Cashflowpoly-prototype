using UnityEngine;
using System.Collections.Generic;

public partial class GameState
{
    // Per-player coin, happiness, and saving storage.
    private void InitializePlayerStats()
    {
        playerCount = Mathf.Clamp(PlayerPrefs.GetInt("PlayerCount", playerCount), 3, 4);
        turn = Mathf.Clamp(turn, 1, playerCount);

        playerCoins = new Dictionary<int, int>();
        playerHappiness = new Dictionary<int, int>();
        playerSaving = new Dictionary<int, int>();

        for (int player = 1; player <= playerCount; player++)
        {
            playerCoins[player] = InitialCoins;
            playerHappiness[player] = InitialHappiness;
            playerSaving[player] = InitialSaving;
        }
    }

    private void EnsurePlayerStats(int player)
    {
        if (playerCoins == null || playerHappiness == null || playerSaving == null)
        {
            InitializePlayerStats();
        }

        if (!playerCoins.ContainsKey(player))
        {
            playerCoins[player] = InitialCoins;
        }

        if (!playerHappiness.ContainsKey(player))
        {
            playerHappiness[player] = InitialHappiness;
        }

        if (!playerSaving.ContainsKey(player))
        {
            playerSaving[player] = InitialSaving;
        }
    }

    public int GetCoins(int player)
    {
        EnsurePlayerStats(player);
        return playerCoins[player];
    }

    public int GetHappiness(int player)
    {
        EnsurePlayerStats(player);
        return playerHappiness[player];
    }

    public int GetSaving(int player)
    {
        EnsurePlayerStats(player);
        return playerSaving[player];
    }

    public void SetCoins(int amount)
    {
        EnsurePlayerStats(turn);
        playerCoins[turn] = amount;
    }

    public void SetHappiness(int amount)
    {
        EnsurePlayerStats(turn);
        playerHappiness[turn] = amount;
    }

    public void SetHappiness(int player, int amount)
    {
        EnsurePlayerStats(player);
        playerHappiness[player] = amount;
    }

    public void ChangeCoins(int amount)
    {
        EnsurePlayerStats(turn);
        playerCoins[turn] += amount;
    }

    public void ChangeCoins(int player, int amount)
    {
        EnsurePlayerStats(player);
        playerCoins[player] += amount;
    }

    public void ChangeHappiness(int amount)
    {
        EnsurePlayerStats(turn);
        playerHappiness[turn] += amount;
    }

    public void ChangeSaving(int amount)
    {
        EnsurePlayerStats(turn);
        playerSaving[turn] += amount;
    }
}
