using System;
using UnityEngine;

public static class NarafinSessionScope
{
    private const string SessionInstanceIdKey = "Narafin.SessionInstanceId";
    private const string SessionNameKey = "Narafin.SessionName";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetSessionOnPlayStart()
    {
        BeginNewSession(string.Empty);
    }

    public static string CurrentSessionId
    {
        get
        {
            string sessionId = PlayerPrefs.GetString(SessionInstanceIdKey, string.Empty);
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                sessionId = EnsureSessionId();
            }

            return sessionId;
        }
    }

    public static string CurrentSessionName
    {
        get
        {
            return PlayerPrefs.GetString(SessionNameKey, string.Empty);
        }
    }

    public static string BeginNewSession(string sessionName)
    {
        string sessionId = Guid.NewGuid().ToString("N");
        PlayerPrefs.SetString(SessionInstanceIdKey, sessionId);
        PlayerPrefs.SetString(SessionNameKey, sessionName ?? string.Empty);
        PlayerPrefs.Save();
        return sessionId;
    }

    public static string GetDialogPlayedKey(string dialogId, int player)
    {
        return BuildKey("DialogPlayed", dialogId, player);
    }

    private static string EnsureSessionId()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        PlayerPrefs.SetString(SessionInstanceIdKey, sessionId);
        PlayerPrefs.Save();
        return sessionId;
    }

    private static string BuildKey(string prefix, string dialogId, int player)
    {
        string safeDialogId = string.IsNullOrWhiteSpace(dialogId) ? "unknown" : dialogId.Trim();
        return prefix + "_" + CurrentSessionId + "_" + player + "_" + safeDialogId;
    }
}
