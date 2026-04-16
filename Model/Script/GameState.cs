using UnityEngine;
using System.Collections.Generic;
public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }
    public int Coins { get; private set; } = 20;
    public int Happiness { get; private set; } = 0;
    public int Saving { get; private set; } = 0;
    public int SavingText { get; private set; } = 0;
    public int JumatBerkah { get; private set; } = 0;
    public Dictionary<string, int> bahanList {get; private set;}
    public Dictionary<string, List<string>> kebutuhanList {get; private set;}
    public string tujuanFinansial {get; private set;} = "";
    public int day {get; private set;} = 1;
    public int movesLeft {get; private set;} = 2;
    public int finishDay {get; private set;} = 13;
    public bool isGameOver {get; private set;} = false;
    public string  kebutuhanSelected {get; private set;} = "";

    void Awake()
    {
        Instance = this;
        bahanList = new Dictionary<string, int>();
        kebutuhanList = new Dictionary<string, List<string>>();
    }

    public void SetCoins(int amount)
    {
        Coins = amount;
    }

    public void SetHappiness(int amount)
    {
        Happiness = amount;
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
        Coins += amount;
    }

    public void ChangeHappiness(int amount)
    {
        Happiness += amount;
    }

    public void ChangeSaving(int amount)
    {
        Saving += amount;
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