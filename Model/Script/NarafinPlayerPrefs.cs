using System.Collections.Generic;
using UnityEngine;

public static class NarafinPlayerPrefs
{
    public const string AccessTokenKey = "Narafin.AccessToken";
    public const string UserIdKey = "Narafin.UserId";
    public const string UsernameKey = "Narafin.Username";
    public const string DisplayNameKey = "Narafin.DisplayName";
    public const string RoleKey = "Narafin.Role";
    public const string ExpiresAtKey = "Narafin.ExpiresAt";
    public const string UgsPlayerIdKey = "Narafin.Ugs.PlayerId";
    public const string UgsProfileKey = "Narafin.Ugs.Profile";
    public const string UgsAuthModeKey = "Narafin.Ugs.AuthMode";
    public const string UgsLinkedNarafinUserIdKey = "Narafin.Ugs.LinkedNarafinUserId";
    public const string ApiSessionIdKey = "Narafin.ApiSessionId";
    public const string ApiRulesetVersionIdKey = "Narafin.ApiRulesetVersionId";
    public const string EventSequenceKey = "Narafin.EventSequence";
    public const string ApiPlayerUserIdKeyPrefix = "Narafin.PlayerUserId_";

    public static string ApiSessionId => PlayerPrefs.GetString(ApiSessionIdKey, string.Empty);
    public static string ApiRulesetVersionId => PlayerPrefs.GetString(ApiRulesetVersionIdKey, string.Empty);
    public static string UgsPlayerId => PlayerPrefs.GetString(UgsPlayerIdKey, string.Empty);
    public static string UgsProfile => PlayerPrefs.GetString(UgsProfileKey, string.Empty);
    public static string UgsAuthMode => PlayerPrefs.GetString(UgsAuthModeKey, string.Empty);
    public static string UgsLinkedNarafinUserId => PlayerPrefs.GetString(UgsLinkedNarafinUserIdKey, string.Empty);

    public static void StorePlaySession(string sessionId, string rulesetVersionId, IReadOnlyList<NarafinPlayerSummary> players)
    {
        PlayerPrefs.SetString(ApiSessionIdKey, sessionId ?? string.Empty);
        PlayerPrefs.SetString(ApiRulesetVersionIdKey, rulesetVersionId ?? string.Empty);
        PlayerPrefs.SetInt(EventSequenceKey, 0);
        ClearApiPlayerUserIds();

        if (players != null)
        {
            for (int i = 0; i < players.Count; i++)
            {
                PlayerPrefs.SetString(ApiPlayerUserIdKeyPrefix + (i + 1), players[i]?.user_id ?? string.Empty);
            }
        }

        PlayerPrefs.Save();
    }

    public static int GetNextEventSequenceNumber()
    {
        return PlayerPrefs.GetInt(EventSequenceKey, 0) + 1;
    }

    public static void SetLastEventSequenceNumber(int sequenceNumber)
    {
        PlayerPrefs.SetInt(EventSequenceKey, Mathf.Max(0, sequenceNumber));
        PlayerPrefs.Save();
    }

    public static void ConfirmEventSequenceNumber(int sequenceNumber)
    {
        int savedSequence = PlayerPrefs.GetInt(EventSequenceKey, 0);
        if (sequenceNumber > savedSequence)
        {
            SetLastEventSequenceNumber(sequenceNumber);
        }
    }

    public static string GetApiPlayerUserId(int player)
    {
        return PlayerPrefs.GetString(ApiPlayerUserIdKeyPrefix + player, string.Empty);
    }

    public static void StoreUgsAuthMapping(string narafinUserId, string ugsPlayerId, string ugsProfile, string ugsAuthMode)
    {
        PlayerPrefs.SetString(UgsLinkedNarafinUserIdKey, narafinUserId ?? string.Empty);
        PlayerPrefs.SetString(UgsPlayerIdKey, ugsPlayerId ?? string.Empty);
        PlayerPrefs.SetString(UgsProfileKey, ugsProfile ?? string.Empty);
        PlayerPrefs.SetString(UgsAuthModeKey, ugsAuthMode ?? string.Empty);
        PlayerPrefs.Save();
    }

    public static void ClearAuthSession()
    {
        PlayerPrefs.DeleteKey(AccessTokenKey);
        PlayerPrefs.DeleteKey(UserIdKey);
        PlayerPrefs.DeleteKey(UsernameKey);
        PlayerPrefs.DeleteKey(DisplayNameKey);
        PlayerPrefs.DeleteKey(RoleKey);
        PlayerPrefs.DeleteKey(ExpiresAtKey);
        ClearUgsAuthMapping();
        PlayerPrefs.DeleteKey(ApiSessionIdKey);
        PlayerPrefs.DeleteKey(ApiRulesetVersionIdKey);
        PlayerPrefs.DeleteKey(EventSequenceKey);
        ClearApiPlayerUserIds();
        PlayerPrefs.Save();
    }

    private static void ClearApiPlayerUserIds()
    {
        for (int i = 1; i <= 4; i++)
        {
            PlayerPrefs.DeleteKey(ApiPlayerUserIdKeyPrefix + i);
        }
    }

    private static void ClearUgsAuthMapping()
    {
        PlayerPrefs.DeleteKey(UgsLinkedNarafinUserIdKey);
        PlayerPrefs.DeleteKey(UgsPlayerIdKey);
        PlayerPrefs.DeleteKey(UgsProfileKey);
        PlayerPrefs.DeleteKey(UgsAuthModeKey);
    }
}
