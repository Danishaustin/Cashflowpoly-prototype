using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

// Menjaga sesi Narafin tetap hidup: heartbeat dikirim sekali setelah sesi dibuat, lalu tiap 30 menit
// selama persiapan maupun permainan. Backend menutup sendiri sesi yang tidak mengirim heartbeat.
public class NarafinHeartbeatRunner : MonoBehaviour
{
    // Jarak kirim bawaan bila server tidak menyebutkan heartbeat_interval_seconds.
    private const float DefaultHeartbeatIntervalSeconds = 1800f;
    private const float MinimumHeartbeatIntervalSeconds = 60f;

    private float heartbeatIntervalSeconds = DefaultHeartbeatIntervalSeconds;

    public static NarafinHeartbeatRunner Instance { get; private set; }

    private Coroutine heartbeatRoutine;
    private bool isSyncing;

    public static NarafinHeartbeatRunner GetOrCreate(GameObject host)
    {
        if (Instance == null && host != null)
        {
            Instance = host.GetComponent<NarafinHeartbeatRunner>() ?? host.AddComponent<NarafinHeartbeatRunner>();
        }

        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void StartForActiveSession()
    {
        Stop();
        if (string.IsNullOrWhiteSpace(NarafinActiveSession.SessionId))
        {
            Debug.Log("Heartbeat session dilewati. Session belum ada atau mode offline aktif.");
            return;
        }

        Debug.Log("Heartbeat session dimulai. Session: " + NarafinActiveSession.SessionId);
        heartbeatRoutine = StartCoroutine(HeartbeatLoop());
    }

    public void Stop()
    {
        if (heartbeatRoutine != null)
        {
            StopCoroutine(heartbeatRoutine);
            heartbeatRoutine = null;
        }
    }

    private IEnumerator HeartbeatLoop()
    {
        while (true)
        {
            Task<NarafinSessionHeartbeatResult> heartbeatTask = SendHeartbeatAsync();
            while (!heartbeatTask.IsCompleted)
            {
                yield return null;
            }

            if (NarafinActiveSession.IsSessionClosedByServer)
            {
                heartbeatRoutine = null;
                yield break;
            }

            yield return new WaitForSecondsRealtime(heartbeatIntervalSeconds);
        }
    }

    // Aplikasi yang kembali aktif bisa saja tertidur lebih lama dari batas heartbeat, jadi sesi diperiksa ulang.
    private void OnApplicationPause(bool isPaused)
    {
        if (!isPaused)
        {
            _ = ResumeSyncAsync();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            _ = ResumeSyncAsync();
        }
    }

    // Urutan wajib dari kontrak backend: tahan antrean event, kirim heartbeat, baru ambil state terbaru.
    public async Task ResumeSyncAsync()
    {
        if (isSyncing
            || LoginManager.Instance == null
            || NarafinActiveSession.IsSessionClosedByServer
            || string.IsNullOrWhiteSpace(NarafinActiveSession.SessionId))
        {
            return;
        }

        isSyncing = true;
        NarafinActiveSession.SetEventSendingHold(true);
        try
        {
            await SendHeartbeatAsync();
            if (NarafinActiveSession.IsSessionClosedByServer)
            {
                return;
            }

            NarafinSessionStateResult stateResult = await LoginManager.Instance.GetActiveSessionStateAsync();
            if (!stateResult.Success)
            {
                Debug.LogWarning("Sinkronisasi state setelah aplikasi aktif gagal: " + stateResult.ErrorCode + " - " + stateResult.ErrorMessage);
            }
        }
        finally
        {
            NarafinActiveSession.SetEventSendingHold(false);
            isSyncing = false;
        }
    }

    private async Task<NarafinSessionHeartbeatResult> SendHeartbeatAsync()
    {
        NarafinSessionHeartbeatResult result = await LoginManager.Instance.SendSessionHeartbeatAsync();

        if (result.SessionClosed)
        {
            NarafinActiveSession.MarkSessionClosedByServer(result.ErrorMessage);
            Stop();
            return result;
        }

        if (result.Success)
        {
            Debug.Log("Heartbeat session terkirim. Berlaku sampai: "
                + (string.IsNullOrWhiteSpace(result.ExpiresAt) ? "(tidak disebutkan)" : result.ExpiresAt)
                + ", jarak kirim berikutnya: " + (result.IntervalSeconds > 0 ? result.IntervalSeconds : (int)heartbeatIntervalSeconds) + " detik.");
            NarafinActiveSession.SetHeartbeatExpiresAt(result.ExpiresAt);
            if (result.IntervalSeconds > 0)
            {
                heartbeatIntervalSeconds = Mathf.Max(MinimumHeartbeatIntervalSeconds, result.IntervalSeconds);
            }
        }
        else
        {
            Debug.LogWarning("Heartbeat session gagal: " + result.ErrorCode + " - " + result.ErrorMessage);
        }

        return result;
    }
}
