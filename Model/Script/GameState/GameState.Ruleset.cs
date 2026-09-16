using System;
using System.Collections.Generic;
using UnityEngine;

public partial class GameState
{
    private const string RulesetModePlayerPrefsKey = "Narafin.RulesetMode";
    private const string PemulaRulesetResource = "Data/rulesetPemula";
    private const string MahirRulesetResource = "Data/rulesetMahir";

    private static readonly string[] FallbackActionIds =
    {
        "KerjaLepas",
        "Kebutuhan",
        "BahanMasakan",
        "JualMasakan",
        "RisikoKehidupan",
        "Asuransi",
        "Menabung",
        "TujuanFinansial",
        "PinjamanSyariah",
        "BayarPinjaman",
        "GunakanOpsiDarurat",
        "AkhirGiliran"
    };

    private readonly HashSet<string> activeActionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> ActiveActionIds => activeActionIds;

    public int ActionsPerTurn { get; private set; } = 2;
    public int MinPlayers { get; private set; } = 3;
    public int MaxPlayers { get; private set; } = 4;
    public int InitialCoins { get; private set; } = 20;
    public int InitialHappiness { get; private set; } = 0;
    public int InitialSaving { get; private set; } = 0;
    public int MaxIngredientTotal { get; private set; } = 6;
    public int MaxSameIngredient { get; private set; } = 3;
    public bool RequirePrimaryBeforeOthers { get; private set; } = true;
    public bool GoldTradeAllowBuy { get; private set; } = true;
    public bool GoldTradeAllowSell { get; private set; } = true;
    public bool LoanEnabled { get; private set; } = true;
    public bool InsuranceEnabled { get; private set; } = true;
    public int FreelanceIncome { get; private set; } = 1;
    public int DonationMinAmount { get; private set; } = 1;
    public int DonationMaxAmount { get; private set; } = 999999;
    public bool FridayEnabled { get; private set; } = true;
    public bool SaturdayEnabled { get; private set; } = true;
    public bool SundayEnabled { get; private set; } = true;

    // sunday_feature REST/LIBUR: hari Minggu tidak dimainkan, cukup dicatat sebagai hari libur.
    public bool SundayIsHoliday { get; private set; }

    private void InitializeRulesetActions()
    {
        LoadRulesetConfiguration();
        ApplyActiveSessionSettings();
    }

    // Settings ruleset session dari server menggantikan settings file ruleset lokal bila tersedia.
    private void ApplyActiveSessionSettings()
    {
        NarafinRulesetSettings sessionSettings = NarafinActiveSession.Catalog?.settings;
        if (sessionSettings == null)
        {
            return;
        }

        ApplyRulesetSettings(new RulesetSettingsData
        {
            actions_per_turn = sessionSettings.actions_per_turn,
            initial_coins = sessionSettings.initial_coins,
            initial_happiness = sessionSettings.initial_happiness,
            initial_saving = sessionSettings.initial_saving,
            finish_day = sessionSettings.finish_day,
            min_players = sessionSettings.min_players,
            max_players = sessionSettings.max_players,
            max_ingredient_total = sessionSettings.max_ingredient_total,
            max_same_ingredient = sessionSettings.max_same_ingredient,
            require_primary_before_others = sessionSettings.require_primary_before_others,
            gold_trade_allow_buy = sessionSettings.gold_trade_allow_buy,
            gold_trade_allow_sell = sessionSettings.gold_trade_allow_sell,
            loan_enabled = sessionSettings.loan_enabled,
            insurance_enabled = sessionSettings.insurance_enabled,
            freelance_income = sessionSettings.freelance_income,
            donation_min_amount = sessionSettings.donation_min_amount,
            donation_max_amount = sessionSettings.donation_max_amount
        });

        Debug.Log(
            "Settings ruleset session dipakai"
            + " | actions_per_turn=" + ActionsPerTurn
            + " | max_ingredient_total=" + MaxIngredientTotal
            + " | max_same_ingredient=" + MaxSameIngredient);
    }

    private void LoadRulesetConfiguration()
    {
        activeActionIds.Clear();

        string mode = PlayerPrefs.GetString(RulesetModePlayerPrefsKey, "Mahir");
        string resourcePath = GetRulesetResourcePath(mode);
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            LoadFallbackRulesetValues();
            return;
        }

        TextAsset rulesetAsset = Resources.Load<TextAsset>(resourcePath);
        if (rulesetAsset == null || string.IsNullOrWhiteSpace(rulesetAsset.text))
        {
            Debug.LogWarning("Ruleset resource tidak ditemukan: " + resourcePath);
            LoadFallbackRulesetValues();
            return;
        }

        try
        {
            RulesetFileData rulesetFile = JsonUtility.FromJson<RulesetFileData>(rulesetAsset.text);
            if (rulesetFile == null || rulesetFile.definition == null)
            {
                Debug.LogWarning("Format ruleset tidak valid untuk resource: " + resourcePath);
                LoadFallbackRulesetValues();
                return;
            }

            ApplyRulesetSettings(rulesetFile.definition.settings);
            ApplyPlayerOrdering(rulesetFile.definition.player_ordering);
            ApplyRulesetActions(rulesetFile.definition.actions);

            if (activeActionIds.Count == 0)
            {
                LoadFallbackActions();
            }

            Debug.Log(
                "Ruleset loaded dari " + resourcePath
                + " | mode=" + mode
                + " | actions_per_turn=" + ActionsPerTurn
                + " | initial_coins=" + InitialCoins
                + " | finish_day=" + finishDay);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal memuat ruleset dari " + resourcePath + ": " + ex.Message);
            LoadFallbackRulesetValues();
        }
    }

    public bool IsActionEnabled(string actionId)
    {
        if (string.IsNullOrWhiteSpace(actionId))
        {
            return false;
        }

        if (activeActionIds.Count == 0)
        {
            return true;
        }

        return activeActionIds.Contains(actionId.Trim());
    }

    private void ApplyRulesetSettings(RulesetSettingsData settings)
    {
        if (settings == null)
        {
            return;
        }

        if (settings.actions_per_turn > 0)
        {
            ActionsPerTurn = settings.actions_per_turn;
        }

        if (settings.min_players > 0)
        {
            MinPlayers = settings.min_players;
        }

        if (settings.max_players > 0)
        {
            MaxPlayers = settings.max_players;
        }

        InitialCoins = settings.initial_coins;
        InitialHappiness = settings.initial_happiness;
        InitialSaving = settings.initial_saving;

        if (settings.finish_day > 0)
        {
            finishDay = settings.finish_day;
        }

        if (settings.max_ingredient_total > 0)
        {
            MaxIngredientTotal = settings.max_ingredient_total;
        }

        if (settings.max_same_ingredient > 0)
        {
            MaxSameIngredient = settings.max_same_ingredient;
        }

        RequirePrimaryBeforeOthers = settings.require_primary_before_others;
        GoldTradeAllowBuy = settings.gold_trade_allow_buy;
        GoldTradeAllowSell = settings.gold_trade_allow_sell;
        LoanEnabled = settings.loan_enabled;
        InsuranceEnabled = settings.insurance_enabled;
        FreelanceIncome = settings.freelance_income;

        if (settings.donation_min_amount > 0)
        {
            DonationMinAmount = settings.donation_min_amount;
        }

        if (settings.donation_max_amount > 0)
        {
            DonationMaxAmount = settings.donation_max_amount;
        }
    }

    private void ApplyPlayerOrdering(RulesetPlayerOrderingData playerOrdering)
    {
        if (playerOrdering == null)
        {
            return;
        }

        FridayEnabled = playerOrdering.friday_enabled;
        SaturdayEnabled = playerOrdering.saturday_enabled;
        SundayEnabled = playerOrdering.sunday_enabled;
        SundayIsHoliday = IsHolidayFeature(playerOrdering.sunday_feature);
    }

    private static bool IsHolidayFeature(string feature)
    {
        switch ((feature ?? string.Empty).Trim().ToUpperInvariant())
        {
            case "REST":
            case "LIBUR":
            case "HOLIDAY":
                return true;
            default:
                return false;
        }
    }

    private void ApplyRulesetActions(List<RulesetActionData> actions)
    {
        if (actions == null)
        {
            return;
        }

        foreach (RulesetActionData action in actions)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.action_id))
            {
                continue;
            }

            activeActionIds.Add(action.action_id.Trim());
        }
    }

    private static string GetRulesetResourcePath(string mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return MahirRulesetResource;
        }

        if (string.Equals(mode, "Pemula", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mode, "PEMULA", StringComparison.OrdinalIgnoreCase))
        {
            return PemulaRulesetResource;
        }

        if (string.Equals(mode, "Mahir", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mode, "MAHIR", StringComparison.OrdinalIgnoreCase))
        {
            return MahirRulesetResource;
        }

        return string.Empty;
    }

    private void LoadFallbackRulesetValues()
    {
        LoadFallbackActions();
    }

    private void LoadFallbackActions()
    {
        foreach (string actionId in FallbackActionIds)
        {
            activeActionIds.Add(actionId);
        }

        Debug.LogWarning("Ruleset actions fallback digunakan karena resource ruleset tidak terbaca.");
    }

    [Serializable]
    private class RulesetFileData
    {
        public string name;
        public string description;
        public RulesetDefinitionData definition;
    }

    [Serializable]
    private class RulesetDefinitionData
    {
        public string mode;
        public RulesetSettingsData settings;
        public RulesetPlayerOrderingData player_ordering;
        public List<RulesetActionData> actions;
    }

    [Serializable]
    private class RulesetSettingsData
    {
        public int actions_per_turn;
        public int initial_coins;
        public int initial_happiness;
        public int initial_saving;
        public int finish_day;
        public int min_players;
        public int max_players;
        public int max_ingredient_total;
        public int max_same_ingredient;
        public bool require_primary_before_others;
        public bool gold_trade_allow_buy;
        public bool gold_trade_allow_sell;
        public bool loan_enabled;
        public bool insurance_enabled;
        public int freelance_income;
        public int donation_min_amount;
        public int donation_max_amount;
    }

    [Serializable]
    private class RulesetPlayerOrderingData
    {
        public string ordering_code;
        public string friday_feature;
        public bool friday_enabled;
        public string saturday_feature;
        public bool saturday_enabled;
        public string sunday_feature;
        public bool sunday_enabled;
        public List<string> instructor_player_usernames;
    }

    [Serializable]
    private class RulesetActionData
    {
        public string action_id;
    }
}
