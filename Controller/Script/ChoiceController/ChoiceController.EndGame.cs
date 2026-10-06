using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    private const string GameFinishedText = "Permainan telah berakhir.";

    private bool isEndingGame;
    private bool hasEndedGame;
    private List<NarafinAnalyticsPlayer> finalScorePlayers;

    // Permainan berakhir setelah aksi terakhir pemain terakhir pada finish_day. Sesi ditutup di server,
    // lalu poin kebahagiaan akhir dibaca dari analitik backend.
    public void ShowGameFinished()
    {
        _ = FinishGameAsync();
    }

    // Server menolak penutupan bila masih ada risiko pengeluaran tertunda atau donasi belum lengkap,
    // dan analitik bisa gagal dibaca, jadi pemain bisa mencoba lagi.
    public void RetryEndSession()
    {
        hasEndedGame = false;
        _ = FinishGameAsync();
    }

    private async Task FinishGameAsync()
    {
        if (isEndingGame || hasEndedGame)
        {
            return;
        }

        isEndingGame = true;
        view.ShowGameFinishedPanel(GameFinishedText);
        view.SetGameFinishedRetryVisible(false);
        view.ShowGameFinishedScores(null);

        NarafinSessionEndResult result = await EndNarafinSessionAsync();

        if (this == null)
        {
            isEndingGame = false;
            return;
        }

        if (result != null && !result.Success && !result.AlreadyClosed)
        {
            isEndingGame = false;
            view.ShowGameFinishedPanel(GameFinishedText + "\nSesi belum bisa ditutup di server: " + result.ErrorMessage);
            view.SetGameFinishedRetryVisible(true);
            return;
        }

        NarafinSessionAnalyticsResult analytics = await LoadFinalScoresAsync();

        isEndingGame = false;
        hasEndedGame = true;

        if (this == null)
        {
            return;
        }

        if (analytics == null || !analytics.Success)
        {
            string reason = analytics == null ? "Mode offline." : analytics.ErrorMessage;
            view.ShowGameFinishedPanel(GameFinishedText + "\nPoin akhir belum bisa diambil: " + reason);
            view.SetGameFinishedRetryVisible(true);
            return;
        }

        finalScorePlayers = analytics.Players;
        view.ShowGameFinishedPanel(GameFinishedText);
        view.SetGameFinishedRetryVisible(false);
        ShowFinalScores();
    }

    private async Task<NarafinSessionAnalyticsResult> LoadFinalScoresAsync()
    {
        if (LoginManager.Instance == null
            || NarafinActiveSession.IsServerOffline
            || string.IsNullOrWhiteSpace(NarafinActiveSession.SessionId))
        {
            return null;
        }

        try
        {
            return await LoginManager.Instance.GetActiveSessionAnalyticsAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Gagal membaca poin akhir dari analitik: " + ex.Message);
            return null;
        }
    }

    // Peringkat memakai total poin; bila seri, pemain dengan nomor giliran terbesar lebih dulu, sesuai rulebook.
    private void ShowFinalScores()
    {
        if (finalScorePlayers == null || finalScorePlayers.Count == 0)
        {
            view.ShowGameFinishedScores(null);
            return;
        }

        List<NarafinAnalyticsPlayer> ranked = new List<NarafinAnalyticsPlayer>(finalScorePlayers);
        ranked.Sort((a, b) =>
        {
            int pointCompare = b.happiness_points_total.CompareTo(a.happiness_points_total);
            return pointCompare != 0 ? pointCompare : b.player_order_no.CompareTo(a.player_order_no);
        });

        List<string> rows = new List<string>();
        for (int i = 0; i < ranked.Count; i++)
        {
            NarafinAnalyticsPlayer player = ranked[i];
            rows.Add((i + 1) + ". " + GetPlayerName(player.player_order_no) + " — " + player.happiness_points_total + " poin");
        }

        // Peringkat dan rinciannya ditampilkan sekaligus; tidak ada lagi tombol untuk berpindah tampilan.
        foreach (NarafinAnalyticsPlayer player in ranked)
        {
            rows.Add("#" + GetPlayerName(player.player_order_no));
            rows.Add("Kebutuhan " + player.need_points_total + "   Bonus set " + player.need_set_bonus_points
                + "   Misi " + FormatSignedPoints(player.mission_reward_total - player.mission_penalty_total));
            rows.Add("Donasi " + player.donation_points_total + "   Dana pensiun " + player.pension_points_total);
            rows.Add("Tujuan finansial " + player.saving_goal_points_total
                + "   Pinjaman " + FormatSignedPoints(-player.loan_penalty_total)
                + "   Emas " + player.gold_points_total);
        }

        view.ShowGameFinishedScores(rows);
    }

    private static string FormatSignedPoints(int points)
    {
        return points > 0 ? "+" + points : points.ToString();
    }

    // ENDED dan DELETED sama-sama berarti berhasil. Sesi yang sudah tertutup tidak dipanggil lagi.
    private async Task<NarafinSessionEndResult> EndNarafinSessionAsync()
    {
        if (LoginManager.Instance == null
            || NarafinActiveSession.IsSessionEnded
            || NarafinActiveSession.IsSessionClosedByServer
            || NarafinActiveSession.IsServerOffline
            || string.IsNullOrWhiteSpace(NarafinActiveSession.SessionId))
        {
            return null;
        }

        NarafinSessionEndResult result = await LoginManager.Instance.EndActiveSessionAsync();
        if (result != null && !result.Success && !result.AlreadyClosed)
        {
            Debug.LogWarning("Sesi gagal ditutup di akhir permainan: " + result.ErrorCode + " - " + result.ErrorMessage);
        }

        return result;
    }
}
