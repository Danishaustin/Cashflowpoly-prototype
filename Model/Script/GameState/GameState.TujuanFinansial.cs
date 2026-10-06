using System;
using System.Collections.Generic;

public partial class GameState
{
    // Tabungan mengikuti backend: satu saldo per pemain, tanpa pembagian per tujuan dan tanpa kartu yang dipesan.
    public void SetSaving(int player, int amount)
    {
        EnsurePlayerStats(player);
        playerSaving[player] = Math.Max(0, amount);
    }

    public void ChangeSaving(int player, int amount)
    {
        SetSaving(player, GetSaving(player) + amount);
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

    // Kartu tujuan dibeli pemain dari tabungan, lalu dicatat ke server sebagai event SYSTEM.
    public void BeliTujuanFinansial(int player, NarafinSetupFinancialGoal goal)
    {
        if (goal == null)
        {
            return;
        }

        ChangeSaving(player, -goal.hargaBeli);
        AddTujuanFinansialDimiliki(player, goal.id);
        SetHappiness(player, GetHappiness(player) + goal.poinKebahagiaan);
    }

    // Koin, tabungan, kebahagiaan, dan daftar tujuan disamakan dengan state server.
    public void ApplyServerTujuanFinansial(int player, NarafinSessionStatePlayer serverPlayer)
    {
        if (serverPlayer == null)
        {
            return;
        }

        SetCoins(player, serverPlayer.coins);
        SetSaving(player, serverPlayer.saving);
        SetHappiness(player, serverPlayer.happiness);

        if (serverPlayer.tujuanFinansial == null)
        {
            return;
        }

        foreach (NarafinStateFinancialGoal progress in serverPlayer.tujuanFinansial)
        {
            NarafinSetupFinancialGoal goal = FindTujuanFinansialByName(progress?.nama);
            bool isOwned = progress != null
                && !string.IsNullOrWhiteSpace(progress.status)
                && !string.Equals(progress.status, "ONGOING", StringComparison.OrdinalIgnoreCase);

            if (goal != null && isOwned)
            {
                AddTujuanFinansialDimiliki(player, goal.id);
            }
        }
    }

    // Sprite tujuan lokal dipetakan berdasarkan harga yang sama, mis. tujuan_30 (Keluar Kota) -> "Tamasya".
    public static string GetTujuanFinansialSpriteName(NarafinSetupFinancialGoal goal)
    {
        return NarafinSpriteMap.GetFinancialGoalSpriteName(goal?.id, goal?.nama);
    }

    private void AddTujuanFinansialDimiliki(int player, string goalId)
    {
        if (!IsTujuanFinansialDimiliki(player, goalId))
        {
            GetTujuanFinansialList(player).Add(goalId);
        }
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

        // Nama tujuan masih diterima untuk paket yang ditulis sebelum identitasnya memakai goal_id.
        string valueKey = NarafinActiveSession.NormalizeName(value);
        foreach (NarafinSetupFinancialGoal goal in goals)
        {
            if (NarafinActiveSession.NormalizeName(goal.nama) == valueKey)
            {
                return goal.id;
            }
        }

        return NarafinActiveSession.NormalizeName(value);
    }
}
