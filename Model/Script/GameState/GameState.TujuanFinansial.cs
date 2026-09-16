using System;
using System.Collections.Generic;
using UnityEngine;

public partial class GameState
{
    // Tabungan per tujuan finansial (goal_id ruleset session) per player.
    private readonly Dictionary<int, Dictionary<string, int>> playerTujuanFinansialSaving = new Dictionary<int, Dictionary<string, int>>();

    // Tujuan yang dianggap tercapai oleh klien karena server belum mengubah statusnya saat tabungan mencapai target.
    // Poinnya dihitung terpisah agar tidak terhapus saat kebahagiaan disamakan dengan server.
    private readonly Dictionary<int, HashSet<string>> playerTujuanFinansialClientAchieved = new Dictionary<int, HashSet<string>>();

    public int GetTujuanFinansialSaving(int player, string goalId)
    {
        if (string.IsNullOrWhiteSpace(goalId)
            || !playerTujuanFinansialSaving.TryGetValue(player, out Dictionary<string, int> savings))
        {
            return 0;
        }

        return savings.TryGetValue(goalId, out int saving) ? saving : 0;
    }

    public int GetTujuanFinansialRemaining(int player, string goalId, int target)
    {
        return Mathf.Max(0, target - GetTujuanFinansialSaving(player, goalId));
    }

    public void AddTujuanFinansialSaving(int player, string goalId, int amount, int target)
    {
        SetTujuanFinansialSaving(player, goalId, Mathf.Min(target, GetTujuanFinansialSaving(player, goalId) + amount));
        EnsurePlayerStats(player);
        playerSaving[player] += amount;
    }

    public void SetSaving(int player, int amount)
    {
        EnsurePlayerStats(player);
        playerSaving[player] = amount;
    }

    // goalIdAtauNama boleh goal_id ruleset atau nama tujuan lokal (prasyarat narasi, mis. "Rumah").
    public bool IsTujuanFinansialDimiliki(int player, string goalIdAtauNama)
    {
        string targetKey = ToTujuanFinansialKey(goalIdAtauNama);
        if (targetKey.Length == 0)
        {
            return false;
        }

        foreach (string owned in GetTujuanFinansialList(player))
        {
            if (ToTujuanFinansialKey(owned) == targetKey)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasAllTujuanFinansial(int player, List<string> requiredTujuan)
    {
        if (requiredTujuan == null)
        {
            return false;
        }

        foreach (string required in requiredTujuan)
        {
            if (!string.IsNullOrWhiteSpace(required) && !IsTujuanFinansialDimiliki(player, required))
            {
                return false;
            }
        }

        return true;
    }

    // Koin, tabungan, kebahagiaan, dan progres tujuan disamakan dengan state server. Tujuan yang tabungannya
    // sudah mencapai target dianggap dimiliki; bila server belum menandainya, poinnya ditambahkan oleh klien.
    public void ApplyServerTujuanFinansial(int player, NarafinSessionStatePlayer serverPlayer)
    {
        if (serverPlayer == null)
        {
            return;
        }

        SetCoins(player, serverPlayer.coins);
        SetSaving(player, serverPlayer.saving);
        HashSet<string> clientAchieved = GetClientAchievedTujuanFinansial(player);

        if (serverPlayer.tujuanFinansial != null)
        {
            foreach (NarafinStateFinancialGoal progress in serverPlayer.tujuanFinansial)
            {
                NarafinSetupFinancialGoal goal = FindTujuanFinansialByName(progress?.nama);
                if (goal == null)
                {
                    continue;
                }

                SetTujuanFinansialSaving(player, goal.id, progress.current_amount);

                bool isServerAchieved = !string.IsNullOrWhiteSpace(progress.status)
                    && !string.Equals(progress.status, "ONGOING", StringComparison.OrdinalIgnoreCase);
                if (isServerAchieved)
                {
                    clientAchieved.Remove(goal.id);
                    AddTujuanFinansialDimiliki(player, goal.id);
                }
                else if (progress.target_amount > 0 && progress.current_amount >= progress.target_amount)
                {
                    clientAchieved.Add(goal.id);
                    AddTujuanFinansialDimiliki(player, goal.id);
                }
            }
        }

        SetHappiness(player, serverPlayer.happiness + GetClientAchievedTujuanFinansialPoints(player));
    }

    // Dipakai bila state server tidak terbaca: tandai tujuan tercapai dari tabungan lokal. Mengembalikan true bila baru tercapai.
    public bool EnsureTujuanFinansialAchieved(int player, NarafinSetupFinancialGoal goal)
    {
        if (goal == null
            || IsTujuanFinansialDimiliki(player, goal.id)
            || GetTujuanFinansialSaving(player, goal.id) < goal.hargaBeli)
        {
            return false;
        }

        GetClientAchievedTujuanFinansial(player).Add(goal.id);
        AddTujuanFinansialDimiliki(player, goal.id);
        SetHappiness(player, GetHappiness(player) + goal.poinKebahagiaan);
        return true;
    }

    // Sprite tujuan lokal dipetakan berdasarkan harga yang sama, mis. tujuan_30 (Keluar Kota) -> "Tamasya".
    public static string GetTujuanFinansialSpriteName(NarafinSetupFinancialGoal goal)
    {
        TujuanFinansialData localTujuan = FindLocalTujuanFinansialByHarga(goal?.hargaBeli ?? -1);
        if (localTujuan != null)
        {
            return localTujuan.nama;
        }

        return (goal?.nama ?? string.Empty).Replace(" ", string.Empty);
    }

    private void AddTujuanFinansialDimiliki(int player, string goalId)
    {
        if (!IsTujuanFinansialDimiliki(player, goalId))
        {
            GetTujuanFinansialList(player).Add(goalId);
        }
    }

    private HashSet<string> GetClientAchievedTujuanFinansial(int player)
    {
        if (!playerTujuanFinansialClientAchieved.TryGetValue(player, out HashSet<string> achieved))
        {
            achieved = new HashSet<string>(StringComparer.Ordinal);
            playerTujuanFinansialClientAchieved[player] = achieved;
        }

        return achieved;
    }

    private int GetClientAchievedTujuanFinansialPoints(int player)
    {
        int points = 0;
        foreach (string goalId in GetClientAchievedTujuanFinansial(player))
        {
            NarafinSetupFinancialGoal goal = NarafinActiveSession.FindFinancialGoal(NarafinActiveSession.Catalog, goalId);
            if (goal != null)
            {
                points += goal.poinKebahagiaan;
            }
        }

        return points;
    }

    private void SetTujuanFinansialSaving(int player, string goalId, int amount)
    {
        if (!playerTujuanFinansialSaving.TryGetValue(player, out Dictionary<string, int> savings))
        {
            savings = new Dictionary<string, int>();
            playerTujuanFinansialSaving[player] = savings;
        }

        savings[goalId] = Mathf.Max(0, amount);
    }

    private static NarafinSetupFinancialGoal FindTujuanFinansialByName(string nama)
    {
        foreach (NarafinSetupFinancialGoal goal in NarafinActiveSession.GetFinancialGoals(NarafinActiveSession.Catalog))
        {
            if (string.Equals(goal.nama, nama, StringComparison.OrdinalIgnoreCase))
            {
                return goal;
            }
        }

        return null;
    }

    private static TujuanFinansialData FindLocalTujuanFinansialByHarga(int hargaBeli)
    {
        if (DataManager.Instance == null || DataManager.Instance.tujuanFinansialDict == null)
        {
            return null;
        }

        foreach (TujuanFinansialData tujuan in DataManager.Instance.tujuanFinansialDict.Values)
        {
            if (tujuan != null && tujuan.hargaBeli == hargaBeli)
            {
                return tujuan;
            }
        }

        return null;
    }

    // Menyamakan goal_id ruleset dan nama tujuan lokal ke satu kunci melalui harga yang sama.
    private static string ToTujuanFinansialKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        List<NarafinSetupFinancialGoal> goals = NarafinActiveSession.GetFinancialGoals(NarafinActiveSession.Catalog);
        foreach (NarafinSetupFinancialGoal goal in goals)
        {
            if (string.Equals(goal.id, value, StringComparison.Ordinal))
            {
                return goal.id;
            }
        }

        if (DataManager.Instance != null
            && DataManager.Instance.tujuanFinansialDict != null
            && DataManager.Instance.tujuanFinansialDict.TryGetValue(value, out TujuanFinansialData localTujuan))
        {
            foreach (NarafinSetupFinancialGoal goal in goals)
            {
                if (goal.hargaBeli == localTujuan.hargaBeli)
                {
                    return goal.id;
                }
            }
        }

        return NarafinActiveSession.NormalizeName(value);
    }
}
