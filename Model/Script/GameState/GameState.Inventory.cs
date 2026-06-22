using UnityEngine;
using System.Collections.Generic;

public partial class GameState
{
    private const int MaxTotalBahanPerPlayer = 6;
    private const int MaxSameBahanPerPlayer = 3;

    // Runtime inventory for bahan and kebutuhan collected by players.
    private void InitializeInventory()
    {
        playerBahanList = new Dictionary<int, Dictionary<string, int>>();
        playerKebutuhanList = new Dictionary<int, Dictionary<string, List<string>>>();
        playerTujuanFinansialList = new Dictionary<int, List<string>>();
        playerMasakanDijualList = new Dictionary<int, List<string>>();
        playerAsuransiDimiliki = new Dictionary<int, bool>();
        playerTargetKebutuhanId = new Dictionary<int, string>();

        for (int player = 1; player <= playerCount; player++)
        {
            playerBahanList[player] = new Dictionary<string, int>();
            playerKebutuhanList[player] = new Dictionary<string, List<string>>();
            playerTujuanFinansialList[player] = new List<string>();
            playerMasakanDijualList[player] = new List<string>();
            playerAsuransiDimiliki[player] = true;
            playerTargetKebutuhanId[player] = string.Empty;
        }
    }

    private void EnsurePlayerInventory(int player)
    {
        if (playerBahanList == null || playerKebutuhanList == null)
        {
            InitializeInventory();
        }

        if (!playerBahanList.ContainsKey(player))
        {
            playerBahanList[player] = new Dictionary<string, int>();
        }

        if (!playerKebutuhanList.ContainsKey(player))
        {
            playerKebutuhanList[player] = new Dictionary<string, List<string>>();
        }

        if (!playerTujuanFinansialList.ContainsKey(player))
        {
            playerTujuanFinansialList[player] = new List<string>();
        }

        if (!playerMasakanDijualList.ContainsKey(player))
        {
            playerMasakanDijualList[player] = new List<string>();
        }

        if (!playerAsuransiDimiliki.ContainsKey(player))
        {
            playerAsuransiDimiliki[player] = true;
        }

        if (!playerTargetKebutuhanId.ContainsKey(player))
        {
            playerTargetKebutuhanId[player] = string.Empty;
        }
    }

    public Dictionary<string, int> GetBahanList(int player)
    {
        EnsurePlayerInventory(player);
        return playerBahanList[player];
    }

    public Dictionary<string, List<string>> GetKebutuhanList(int player)
    {
        EnsurePlayerInventory(player);
        return playerKebutuhanList[player];
    }

    public List<string> GetTujuanFinansialList(int player)
    {
        EnsurePlayerInventory(player);
        return playerTujuanFinansialList[player];
    }

    public List<string> GetMasakanDijualList(int player)
    {
        EnsurePlayerInventory(player);
        return playerMasakanDijualList[player];
    }

    public bool GetAsuransiDimiliki(int player)
    {
        EnsurePlayerInventory(player);
        return playerAsuransiDimiliki[player];
    }

    public List<string> HasBahan(Dictionary<string, int> jumlahBahan)
    {
        Dictionary<string, int> activeBahanList = GetBahanList(turn);
        var kurangBahan = new List<string>();
        foreach (var kv in jumlahBahan)
        {
            if (!activeBahanList.ContainsKey(kv.Key) || activeBahanList[kv.Key] < kv.Value)
            {
                kurangBahan.Add(kv.Key);
            }
        }
        return kurangBahan;
    }

    public void AddBahanToList(string nama)
    {
        AddBahanToList(turn, nama);
    }

    public void AddBahanToList(int player, string nama)
    {
        if (!CanAddBahan(player, nama))
        {
            return;
        }

        Dictionary<string, int> activeBahanList = GetBahanList(player);
        activeBahanList[nama] = activeBahanList.ContainsKey(nama) ? activeBahanList[nama] + 1 : 1;
        Debug.Log($"Added {nama} to player {player} list. Current count: {activeBahanList[nama]}");
    }

    public void RemoveBahanFromList(string nama, int jumlah)
    {
        Dictionary<string, int> activeBahanList = GetBahanList(turn);
        if (activeBahanList.ContainsKey(nama) && activeBahanList[nama] >= jumlah)
        {
            activeBahanList[nama] -= jumlah;
        }
    }

    public void AddKebutuhanToList(string nama, string tipe)
    {
        Dictionary<string, List<string>> activeKebutuhanList = GetKebutuhanList(turn);
        if (!activeKebutuhanList.ContainsKey(tipe))
        {
            activeKebutuhanList[tipe] = new List<string>();
        }
        activeKebutuhanList[tipe].Add(nama);
    }

    public bool HasKebutuhanPrimer(int player)
    {
        Dictionary<string, List<string>> kebutuhan = GetKebutuhanList(player);
        foreach (var kv in kebutuhan)
        {
            if (string.Equals(kv.Key, "primer", System.StringComparison.OrdinalIgnoreCase)
                && kv.Value != null
                && kv.Value.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    public void AddTujuanFinansialToList(string nama)
    {
        tujuanFinansial = nama;
        List<string> activeTujuanFinansialList = GetTujuanFinansialList(turn);
        if (!activeTujuanFinansialList.Contains(nama))
        {
            activeTujuanFinansialList.Add(nama);
        }
    }

    public void AddMasakanDijualToList(string nama)
    {
        GetMasakanDijualList(turn).Add(nama);
    }

    public void SetAsuransiDimiliki(bool value)
    {
        EnsurePlayerInventory(turn);
        playerAsuransiDimiliki[turn] = value;
    }

    public void SetAsuransiDimiliki(int player, bool value)
    {
        EnsurePlayerInventory(player);
        playerAsuransiDimiliki[player] = value;
    }

    public void SetTargetKebutuhanId(int player, string targetId)
    {
        EnsurePlayerInventory(player);
        playerTargetKebutuhanId[player] = targetId ?? string.Empty;
    }

    public string GetTargetKebutuhanId(int player)
    {
        EnsurePlayerInventory(player);
        return playerTargetKebutuhanId[player];
    }

    public bool IsTargetKebutuhanSudahDipilih(string targetId)
    {
        if (string.IsNullOrEmpty(targetId))
        {
            return false;
        }

        foreach (var selected in playerTargetKebutuhanId.Values)
        {
            if (selected == targetId)
            {
                return true;
            }
        }

        return false;
    }

    public int GetTotalBahanCount(int player)
    {
        Dictionary<string, int> bahan = GetBahanList(player);
        int total = 0;
        foreach (var kv in bahan)
        {
            if (kv.Value > 0)
            {
                total += kv.Value;
            }
        }

        return total;
    }

    public int GetBahanCount(int player, string nama)
    {
        if (string.IsNullOrEmpty(nama))
        {
            return 0;
        }

        Dictionary<string, int> bahan = GetBahanList(player);
        return bahan.TryGetValue(nama, out int jumlah) ? jumlah : 0;
    }

    public bool IsBahanTotalAtLimit(int player)
    {
        return GetTotalBahanCount(player) >= MaxTotalBahanPerPlayer;
    }

    public bool IsBahanAtLimit(int player, string nama)
    {
        return GetBahanCount(player, nama) >= MaxSameBahanPerPlayer;
    }

    public bool CanAddBahan(int player, string nama)
    {
        if (string.IsNullOrEmpty(nama))
        {
            return false;
        }

        if (IsBahanTotalAtLimit(player))
        {
            return false;
        }

        return !IsBahanAtLimit(player, nama);
    }
}
