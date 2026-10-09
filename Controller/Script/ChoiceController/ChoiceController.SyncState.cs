using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    // Menyamakan koin, kebahagiaan, dan tabungan tiap pemain dengan hitungan server di akhir hari.
    //
    // Selama hari berjalan ketiga angka itu dihitung klien sendiri dari ruleset lokal, sementara
    // server menghitung versinya dari event yang masuk. Keduanya bisa menyimpang -- misalnya bila
    // ruleset lokal tidak sama dengan ruleset di server, atau ada event yang ditolak diam-diam.
    // Sinkronisasi ini menutup hari dengan angka server sebagai acuan.
    //
    // Hanya tiga angka itu yang disamakan. /state tidak memuat emas, polis asuransi, pinjaman,
    // maupun risiko tertunda, jadi sisanya tetap hanya ada di klien.
    private async Task SyncPlayerStatsFromServerAsync()
    {
        // Mode luring menganggap event sukses tanpa mengirimnya, jadi tidak ada state server untuk
        // dibaca; memanggilnya di sana hanya akan menggantung permainan.
        if (NarafinActiveSession.IsServerOffline
            || NarafinActiveSession.IsSessionEnded
            || NarafinActiveSession.IsSessionClosedByServer
            || LoginManager.Instance == null
            || GameState.Instance == null)
        {
            return;
        }

        NarafinSessionStateResult stateResult;
        try
        {
            stateResult = await LoginManager.Instance.GetActiveSessionStateAsync();
        }
        catch (Exception ex)
        {
            // Jaringan putus tidak boleh menghentikan permainan; angka lokal tetap dipakai.
            Debug.LogWarning("Sinkronisasi akhir hari gagal: " + ex.Message);
            return;
        }

        if (this == null)
        {
            return;
        }

        if (!stateResult.Success || stateResult.Players == null)
        {
            Debug.LogWarning("Sinkronisasi akhir hari gagal: "
                + stateResult.ErrorCode + " - " + stateResult.ErrorMessage);
            return;
        }

        List<string> perubahan = new List<string>();
        foreach (NarafinSessionStatePlayer serverPlayer in stateResult.Players)
        {
            if (serverPlayer == null
                || serverPlayer.player_order_no < 1
                || serverPlayer.player_order_no > GameState.Instance.playerCount)
            {
                continue;
            }

            int player = serverPlayer.player_order_no;

            CatatSelisihStat(perubahan, player, "Koin",
                GameState.Instance.GetCoins(player), serverPlayer.coins);
            CatatSelisihStat(perubahan, player, "Kebahagiaan",
                GameState.Instance.GetHappiness(player), serverPlayer.happiness);
            CatatSelisihStat(perubahan, player, "Tabungan",
                GameState.Instance.GetSaving(player), serverPlayer.saving);

            GameState.Instance.SetCoins(player, serverPlayer.coins);
            GameState.Instance.SetHappiness(player, serverPlayer.happiness);
            GameState.Instance.SetSaving(player, serverPlayer.saving);
        }

        if (perubahan.Count == 0)
        {
            return;
        }

        // Angka yang berubah sendiri tanpa penjelasan akan terbaca sebagai bug, dan instruktur justru
        // perlu tahu bila klien dan server berbeda hitungan.
        view.UpdatePlayerStats();
        foreach (string baris in perubahan)
        {
            view.AddSystemTextToDialog(baris);
        }
    }

    private void CatatSelisihStat(List<string> perubahan, int player, string nama, int lokal, int server)
    {
        if (lokal == server)
        {
            return;
        }

        perubahan.Add(nama + " " + GetPlayerName(player) + " disesuaikan: " + lokal + " → " + server);
    }
}
