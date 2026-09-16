using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManagerPlay
{
    // Pesan penungguan server baru tampil bila responsnya lambat, lalu ditahan sejenak.
    // Tanpa ini, koneksi cepat membuat teks penungguan tampil setengah jadi lalu langsung terpotong
    // oleh teks hasil aksi, karena keduanya memakai animasi mesin tik yang sama.
    private const long ServerWaitShowDelayMs = 300;
    private const float ServerWaitMinVisibleSeconds = 0.3f;

    private IVisualElementScheduledItem serverWaitSchedule;
    private string serverWaitText;
    private bool isServerWaitVisible;
    private float serverWaitShownTime;

    public void BeginServerWait(string progressText)
    {
        CancelServerWaitSchedule();
        isServerWaitVisible = false;

        if (dialog == null || string.IsNullOrWhiteSpace(progressText))
        {
            return;
        }

        serverWaitText = progressText;
        serverWaitSchedule = dialog.schedule.Execute(ShowServerWaitText).StartingIn(ServerWaitShowDelayMs);
    }

    // Sisa waktu (detik) yang perlu ditunggu sebelum teks hasil boleh menggantikan pesan penungguan.
    public float EndServerWait()
    {
        CancelServerWaitSchedule();

        if (!isServerWaitVisible)
        {
            return 0f;
        }

        isServerWaitVisible = false;
        float visibleForSeconds = Time.unscaledTime - serverWaitShownTime;
        return Mathf.Max(0f, ServerWaitMinVisibleSeconds - visibleForSeconds);
    }

    private void ShowServerWaitText()
    {
        serverWaitSchedule = null;
        if (string.IsNullOrWhiteSpace(serverWaitText))
        {
            return;
        }

        isServerWaitVisible = true;
        serverWaitShownTime = Time.unscaledTime;
        AddSystemTextToDialog(serverWaitText);
    }

    private void CancelServerWaitSchedule()
    {
        serverWaitSchedule?.Pause();
        serverWaitSchedule = null;
        serverWaitText = null;
    }
}
