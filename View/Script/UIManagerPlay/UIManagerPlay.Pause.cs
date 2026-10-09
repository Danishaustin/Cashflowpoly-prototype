using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public partial class UIManagerPlay
{
    // Membedakan dua pemakaian panel konfirmasi yang sama: keluar di tengah permainan, yang
    // membatalkan sesi, dan kembali ke Home sesudah permainan berakhir, yang tidak merusak apa pun.
    private bool homeConfirmFromGameFinished;

    // Pause panel actions and scene navigation.
    private void TogglePause(ClickEvent evt)
    {
        SetPauseState(!GameState.Instance.isPaused);
    }


    private void ResumePause(ClickEvent evt)
    {
        SetPauseState(false);
    }

    // Muncul hanya bila penutupan sesi gagal, mis. masih ada risiko pengeluaran yang tertunda di server.
    private void RetryEndSessionFromGameFinished(ClickEvent evt)
    {
        choiceController?.RetryEndSession();
    }

    // Sesi sudah ditutup saat permainan berakhir, jadi kembali ke Home tidak merusak apa pun.
    // Konfirmasinya tetap diminta supaya tidak ada yang keluar karena salah tekan.
    private void GoToHomeAfterGameFinished(ClickEvent evt)
    {
        homeConfirmFromGameFinished = true;
        ShowExitConfirmPanel(true);
    }

    // Di tengah permainan sesi TIDAK bisa diakhiri resmi: /end menolak 422 sampai hari finish. Yang
    // benar-benar terjadi adalah heartbeat dihentikan, lalu server membatalkan sesi (CANCELLED,
    // end_reason=HEARTBEAT_TIMEOUT) sesudah satu jam tanpa aktivitas, dengan riwayat tetap tersimpan
    // tanpa skor akhir. Sesi yang belum ada aktivitas bermain justru dihapus seketika oleh /end.
    private void GoToHome(ClickEvent evt)
    {
        ShowExitConfirmPanel(true);
    }

    private void CancelExitToHome(ClickEvent evt)
    {
        ShowExitConfirmPanel(false);
        homeConfirmFromGameFinished = false;
    }

    private void ConfirmExitToHome(ClickEvent evt)
    {
        if (homeConfirmFromGameFinished)
        {
            // Sesi sudah ditutup saat permainan berakhir; memanggil penutupan lagi hanya akan
            // menghasilkan galat dari server.
            homeConfirmFromGameFinished = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
            return;
        }

        _ = ExitToHomeAsync();
    }

    private void ShowExitConfirmPanel(bool show)
    {
        if (exitConfirmPanel == null)
        {
            // Tanpa panel konfirmasi, perilaku lama dipakai supaya tombol home tetap berfungsi.
            SetPauseState(false);
            SceneManager.LoadScene(0);
            return;
        }

        // Panel jeda hanya diutak-atik pada jalur keluar di tengah permainan. Di akhir permainan
        // panel jeda tidak sedang terbuka, dan menampilkannya kembali saat konfirmasi ditutup akan
        // memunculkan panel yang tidak diminta siapa pun.
        if (pausePanel != null && !homeConfirmFromGameFinished)
        {
            pausePanel.RemoveFromClassList("show-pause-panel");
            pausePanel.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
            if (!show)
            {
                pausePanel.AddToClassList("show-pause-panel");
            }
        }

        if (exitConfirmText != null)
        {
            exitConfirmText.text = homeConfirmFromGameFinished
                ? "Kembali ke Home?"
                : "Keluar dan batalkan sesi?";
        }

        // Di tengah permainan tombol Ya membatalkan sesi, jadi ia memakai warna merusak. Di akhir
        // permainan sesi sudah ditutup, jadi Ya hanya berpindah layar dan memakai warna utama.
        exitConfirmYesButton?.EnableInClassList("btn-bahaya", !homeConfirmFromGameFinished);

        exitConfirmYesButton?.SetEnabled(true);
        exitConfirmNoButton?.SetEnabled(true);
        if (show)
        {
            exitConfirmPanel.style.display = DisplayStyle.Flex;
            exitConfirmPanel.BringToFront();
            exitConfirmPanel.schedule.Execute(() =>
            {
                exitConfirmPanel.AddToClassList("show-pause-panel");
            }).StartingIn(1);
            return;
        }

        exitConfirmPanel.RemoveFromClassList("show-pause-panel");
        exitConfirmPanel.style.display = DisplayStyle.None;
    }

    private async Task ExitToHomeAsync()
    {
        exitConfirmYesButton?.SetEnabled(false);
        exitConfirmNoButton?.SetEnabled(false);

        if (exitConfirmText != null)
        {
            exitConfirmText.text = "Menghentikan heartbeat...";
        }

        // Permintaan jaringan tidak terpengaruh timeScale, tetapi waktu dikembalikan dulu sebelum pindah scene.
        Time.timeScale = 1f;

        if (LoginManager.Instance != null
            && !NarafinActiveSession.IsServerOffline
            && !NarafinActiveSession.IsSessionEnded
            && !NarafinActiveSession.IsSessionClosedByServer
            && !string.IsNullOrWhiteSpace(NarafinActiveSession.SessionId))
        {
            NarafinSessionEndResult result = await LoginManager.Instance.EndActiveSessionAsync();
            if (!result.Success && !result.AlreadyClosed)
            {
                Debug.LogWarning("Sesi gagal ditutup saat keluar ke Home: " + result.ErrorCode + " - " + result.ErrorMessage);
            }
        }

        if (this == null)
        {
            return;
        }

        ShowExitConfirmPanel(false);
        SetPauseState(false);
        SceneManager.LoadScene(0);
    }

    private void SetPauseState(bool pause)
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.SetPause(pause);
        }

        Time.timeScale = pause ? 0f : 1f;

        if (pausePanel == null)
        {
            return;
        }

        if (pause)
        {
            if (pauseInputBlocker != null)
            {
                pauseInputBlocker.style.display = DisplayStyle.Flex;
                pauseInputBlocker.BringToFront();
            }

            pauseToggleButton?.BringToFront();
            pausePanel.style.display = DisplayStyle.Flex;
            pausePanel.BringToFront();
            pausePanel.schedule.Execute(() =>
            {
                pausePanel.AddToClassList("show-pause-panel");
            }).StartingIn(1);
            return;
        }

        pausePanel.RemoveFromClassList("show-pause-panel");
        pausePanel.schedule.Execute(() =>
        {
            if (GameState.Instance == null || !GameState.Instance.isPaused)
            {
                pausePanel.style.display = DisplayStyle.None;

                if (pauseInputBlocker != null)
                {
                    pauseInputBlocker.style.display = DisplayStyle.None;
                }
            }
        }).StartingIn(350);
    }
}
