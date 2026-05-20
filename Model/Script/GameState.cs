using UnityEngine;
using System.Collections.Generic;
public class GameState : MonoBehaviour
{
    // Singleton
    public static GameState Instance { get; private set; }

    // Initial Player Stats
    private const int InitialCoins = 20;
    private const int InitialHappiness = 0;
    private const int InitialSaving = 0;

    // Player Turn
    public int turn = 1;
    public int playerCount = 3;

    // Active Player Stats
    public int Coins => GetCoins(turn);
    public int Happiness => GetHappiness(turn);
    public int Saving => GetSaving(turn);

    // Player Stat Storage
    private Dictionary<int, int> playerCoins;
    private Dictionary<int, int> playerHappiness;
    private Dictionary<int, int> playerSaving;

    // Game Progress
    public int day {get; private set;} = 1;
    public int movesLeft {get; private set;} = 2;
    public int finishDay {get; private set;} = 13;
    public bool isGameOver {get; private set;} = false;

    // Temporary UI State
    public int SavingText { get; private set; } = 0;
    public int JumatBerkah { get; private set; } = 0;
    public string  kebutuhanSelected {get; private set;} = "";

    // Inventory
    public Dictionary<string, int> bahanList {get; private set;}
    public Dictionary<string, List<string>> kebutuhanList {get; private set;}
    public string tujuanFinansial {get; private set;} = "";

    // Narasi
    public int jmAksiKe = 1;
    public int tfAksiKe = 1;
    public int bmAksiKe = 1;
    public int klAksiKe = 1;
    public int kAksiKe = 1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        InitializePlayerStats();
        bahanList = new Dictionary<string, int>();
        kebutuhanList = new Dictionary<string, List<string>>();
    }

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

    public void SetSavingText(int amount)
    {
        SavingText = amount;
    }

    public void SetKebutuhanSelected(string kebutuhan)
    {
        kebutuhanSelected = kebutuhan;
    }

    public void ChangeCoins(int amount)
    {
        EnsurePlayerStats(turn);
        playerCoins[turn] += amount;
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

    public void ChangeSavingText(int amount)
    {
        SavingText += amount;
    }

    public List<string> HasBahan(Dictionary<string, int> jumlahBahan)
    {
        var kurangBahan = new List<string>();
        foreach (var kv in jumlahBahan)
        {
            if (!bahanList.ContainsKey(kv.Key) || bahanList[kv.Key] < kv.Value)
            {
                kurangBahan.Add(kv.Key);
            }
        }
        return kurangBahan;
    }

    public void AddBahanToList(string nama)
    {
        bahanList[nama] = bahanList.ContainsKey(nama) ? bahanList[nama] + 1 : 1;
        Debug.Log($"Added {nama} to list. Current count: {bahanList[nama]}");
    }

    public void RemoveBahanFromList(string nama, int jumlah)
    {
        if (bahanList.ContainsKey(nama) && bahanList[nama] >= jumlah)
        {
            bahanList[nama] -= jumlah;
        }
    }

    public void AddKebutuhanToList(string nama, string tipe)
    {
        if (!kebutuhanList.ContainsKey(tipe))
        {
            kebutuhanList[tipe] = new List<string>();
        }
        kebutuhanList[tipe].Add(nama);
    }

    public void NextDay()
    {
        day++;
        turn = 1;
        movesLeft = 2;
        if (day >= finishDay)
        {
            isGameOver = true;
            return;
        }
        while(day % 7 == 6 || day % 7 == 0)
        {
            day++;
        }
    }

    public void UseMove()
    {
        if (movesLeft > 0)
        {
            movesLeft--;
        } 
        if (movesLeft <= 0)
        {
            if (turn == playerCount)
            {
                turn = 1;
                NextDay();
                return;
            } 

            turn++;
            movesLeft = 2;
        }
    }

    public bool IsJumatBerkah()
    {
        if(day % 7 == 5)
            return true;

        return false;
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }
}
