using UnityEngine;
using System.Collections.Generic;

public partial class GameState
{
    private int weeklyBahanPriceDelta;
    private int weeklyBahanPriceWeek;

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

    // Inventory bahan dikunci dengan card_id ruleset session. Nama lokal (resep, narasi, data offline)
    // di-resolve ke card_id; tanpa katalog session, kunci tetap nama lokal.
    public string ResolveBahanKey(string namaAtauCardId)
    {
        if (string.IsNullOrWhiteSpace(namaAtauCardId))
        {
            return string.Empty;
        }

        NarafinRulesetSetupDefinition catalog = NarafinActiveSession.Catalog;
        if (catalog != null && NarafinActiveSession.TryResolveIngredientCardId(catalog, namaAtauCardId, out string cardId))
        {
            return cardId;
        }

        return namaAtauCardId;
    }

    public bool IsKnownBahan(string namaAtauCardId)
    {
        NarafinRulesetSetupDefinition catalog = NarafinActiveSession.Catalog;
        if (catalog != null)
        {
            return NarafinActiveSession.FindIngredient(catalog, namaAtauCardId) != null;
        }

        return DataManager.Instance != null
            && DataManager.Instance.bahanDict != null
            && !string.IsNullOrWhiteSpace(namaAtauCardId)
            && DataManager.Instance.bahanDict.ContainsKey(namaAtauCardId);
    }

    public string GetBahanDisplayName(string namaAtauCardId)
    {
        NarafinSetupIngredient ingredient = NarafinActiveSession.FindIngredient(NarafinActiveSession.Catalog, namaAtauCardId);
        if (ingredient != null && !string.IsNullOrWhiteSpace(ingredient.nama))
        {
            return ingredient.nama;
        }

        return namaAtauCardId ?? string.Empty;
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

    // jumlahBahan boleh memakai nama lokal; yang dikembalikan adalah nama yang kurang sesuai input.
    public List<string> HasBahan(Dictionary<string, int> jumlahBahan)
    {
        var kurangBahan = new List<string>();
        foreach (var kv in jumlahBahan)
        {
            if (GetBahanCount(turn, kv.Key) < kv.Value)
            {
                kurangBahan.Add(kv.Key);
            }
        }
        return kurangBahan;
    }

    public void AddBahanToList(string namaAtauCardId)
    {
        AddBahanToList(turn, namaAtauCardId);
    }

    public void AddBahanToList(int player, string namaAtauCardId)
    {
        string bahanKey = ResolveBahanKey(namaAtauCardId);
        if (!CanAddBahan(player, bahanKey))
        {
            return;
        }

        Dictionary<string, int> activeBahanList = GetBahanList(player);
        activeBahanList[bahanKey] = activeBahanList.ContainsKey(bahanKey) ? activeBahanList[bahanKey] + 1 : 1;
        Debug.Log($"Added {bahanKey} to player {player} list. Current count: {activeBahanList[bahanKey]}");
    }

    public void RemoveBahanFromList(string namaAtauCardId, int jumlah)
    {
        string bahanKey = ResolveBahanKey(namaAtauCardId);
        Dictionary<string, int> activeBahanList = GetBahanList(turn);
        if (activeBahanList.ContainsKey(bahanKey) && activeBahanList[bahanKey] >= jumlah)
        {
            activeBahanList[bahanKey] -= jumlah;
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

    public int GetBahanCount(int player, string namaAtauCardId)
    {
        if (string.IsNullOrEmpty(namaAtauCardId))
        {
            return 0;
        }

        Dictionary<string, int> bahan = GetBahanList(player);
        return bahan.TryGetValue(ResolveBahanKey(namaAtauCardId), out int jumlah) ? jumlah : 0;
    }

    public bool IsBahanTotalAtLimit(int player)
    {
        return GetTotalBahanCount(player) >= MaxIngredientTotal;
    }

    public bool IsBahanAtLimit(int player, string namaAtauCardId)
    {
        return GetBahanCount(player, namaAtauCardId) >= MaxSameIngredient;
    }

    public bool CanAddBahan(int player, string namaAtauCardId)
    {
        if (string.IsNullOrEmpty(namaAtauCardId))
        {
            return false;
        }

        if (IsBahanTotalAtLimit(player))
        {
            return false;
        }

        return !IsBahanAtLimit(player, namaAtauCardId);
    }

    // Temporary weekly modifier from Risiko Kehidupan for ingredient prices.
    public void SetMingguanPerubahanHargaBahan(int delta)
    {
        weeklyBahanPriceDelta = delta;
        weeklyBahanPriceWeek = GetCurrentWeekIndex();
    }

    public int GetMingguanPerubahanHargaBahan()
    {
        if (GetCurrentWeekIndex() != weeklyBahanPriceWeek)
        {
            return 0;
        }

        return weeklyBahanPriceDelta;
    }

    // Harga dasar dari katalog ruleset session; tanpa katalog memakai data bahan lokal.
    public int GetHargaBahanEfektif(string namaAtauCardId)
    {
        int hargaDasar;
        NarafinSetupIngredient ingredient = NarafinActiveSession.FindIngredient(NarafinActiveSession.Catalog, namaAtauCardId);
        if (ingredient != null)
        {
            hargaDasar = ingredient.hargaBeli;
        }
        else if (DataManager.Instance != null
                 && DataManager.Instance.bahanDict != null
                 && !string.IsNullOrWhiteSpace(namaAtauCardId)
                 && DataManager.Instance.bahanDict.TryGetValue(namaAtauCardId, out BahanMakananData bahanData))
        {
            hargaDasar = bahanData.hargaBeli;
        }
        else
        {
            return 0;
        }

        return Mathf.Max(0, hargaDasar + GetMingguanPerubahanHargaBahan());
    }

    private int GetCurrentWeekIndex()
    {
        return ((day - 1) / 7) + 1;
    }
}
