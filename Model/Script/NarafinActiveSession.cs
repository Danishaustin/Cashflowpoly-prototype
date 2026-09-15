using System;
using System.Collections.Generic;
using System.Text;

public class NarafinSetupStartResult
{
    public bool Success;
    public string ErrorCode;
    public string ErrorMessage;
    public List<NarafinSessionStatePlayer> Players;
}

// Session Narafin yang sedang disiapkan atau dimainkan: dibuat di Home, dipakai scene Play untuk pembagian awal dan start.
public static class NarafinActiveSession
{
    private static readonly List<NarafinSessionStatePlayer> players = new List<NarafinSessionStatePlayer>();

    public static string SessionId { get; private set; } = string.Empty;
    public static string RulesetId { get; private set; } = string.Empty;
    public static string RulesetVersionId { get; private set; } = string.Empty;
    public static int RulesetVersion { get; private set; }
    public static string Mode { get; private set; } = string.Empty;
    public static NarafinRulesetSetupDefinition Catalog { get; private set; }
    public static bool IsStarted { get; private set; }
    public static IReadOnlyList<NarafinSessionStatePlayer> Players => players;
    public static bool IsMahir => string.Equals(Mode, "MAHIR", StringComparison.OrdinalIgnoreCase);

    public static void Begin(
        string sessionId,
        string rulesetId,
        string rulesetVersionId,
        int rulesetVersion,
        string mode,
        NarafinRulesetSetupDefinition catalog,
        IEnumerable<NarafinSessionStatePlayer> sessionPlayers)
    {
        SessionId = sessionId ?? string.Empty;
        RulesetId = rulesetId ?? string.Empty;
        RulesetVersionId = rulesetVersionId ?? string.Empty;
        RulesetVersion = rulesetVersion;
        Mode = mode ?? string.Empty;
        Catalog = catalog;
        IsStarted = false;

        players.Clear();
        if (sessionPlayers != null)
        {
            foreach (NarafinSessionStatePlayer player in sessionPlayers)
            {
                if (player != null)
                {
                    players.Add(player);
                }
            }
        }

        players.Sort((a, b) => a.player_order_no.CompareTo(b.player_order_no));
    }

    public static void MarkStarted()
    {
        IsStarted = true;
    }

    public static void Clear()
    {
        Begin(string.Empty, string.Empty, string.Empty, 0, string.Empty, null, null);
    }

    // Diurutkan berdasarkan nomor agar player urutan ke-n memperoleh tie breaker nomor ke-n.
    public static List<NarafinSetupTieBreaker> GetTieBreakersByNumber(NarafinRulesetSetupDefinition catalog)
    {
        List<NarafinSetupTieBreaker> tieBreakers = new List<NarafinSetupTieBreaker>();
        if (catalog?.tie_breakers == null)
        {
            return tieBreakers;
        }

        foreach (NarafinSetupTieBreaker tieBreaker in catalog.tie_breakers)
        {
            if (tieBreaker != null && !string.IsNullOrWhiteSpace(tieBreaker.tie_breaker_code))
            {
                tieBreakers.Add(tieBreaker);
            }
        }

        tieBreakers.Sort((a, b) => a.tie_number.CompareTo(b.tie_number));
        return tieBreakers;
    }

    public static NarafinSetupLoan GetFirstLoan(NarafinRulesetSetupDefinition catalog)
    {
        if (catalog?.sharia_loans == null)
        {
            return null;
        }

        foreach (NarafinSetupLoan loan in catalog.sharia_loans)
        {
            if (loan != null && !string.IsNullOrWhiteSpace(loan.loan_code))
            {
                return loan;
            }
        }

        return null;
    }

    public static NarafinSetupInsurance GetFirstInsurance(NarafinRulesetSetupDefinition catalog)
    {
        if (catalog?.insurance_products == null)
        {
            return null;
        }

        foreach (NarafinSetupInsurance insurance in catalog.insurance_products)
        {
            if (insurance != null && !string.IsNullOrWhiteSpace(insurance.product_code))
            {
                return insurance;
            }
        }

        return null;
    }

    public static List<NarafinSetupMission> GetMissions(NarafinRulesetSetupDefinition catalog)
    {
        List<NarafinSetupMission> missions = new List<NarafinSetupMission>();
        if (catalog?.collection_missions == null)
        {
            return missions;
        }

        foreach (NarafinSetupMission mission in catalog.collection_missions)
        {
            if (mission != null && !string.IsNullOrWhiteSpace(mission.id))
            {
                missions.Add(mission);
            }
        }

        return missions;
    }

    public static bool HasMission(NarafinRulesetSetupDefinition catalog, string missionId)
    {
        if (string.IsNullOrWhiteSpace(missionId))
        {
            return false;
        }

        foreach (NarafinSetupMission mission in GetMissions(catalog))
        {
            if (string.Equals(mission.id, missionId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    // Nama bahan lokal bisa berbeda dari katalog ("Nasi" vs "Nasi Putih", "Sayuran" vs "Sayur"):
    // cocokkan persis dulu, lalu satu-satunya bahan yang namanya saling memuat.
    public static bool TryResolveIngredientCardId(NarafinRulesetSetupDefinition catalog, string localName, out string cardId)
    {
        cardId = string.Empty;
        string localKey = NormalizeName(localName);
        if (localKey.Length == 0 || catalog?.ingredients == null)
        {
            return false;
        }

        NarafinSetupIngredient partialMatch = null;
        int partialMatchCount = 0;

        foreach (NarafinSetupIngredient ingredient in catalog.ingredients)
        {
            if (ingredient == null || string.IsNullOrWhiteSpace(ingredient.id))
            {
                continue;
            }

            string nameKey = NormalizeName(ingredient.nama);
            if (nameKey == localKey || NormalizeName(ingredient.id) == localKey)
            {
                cardId = ingredient.id;
                return true;
            }

            if (nameKey.Length > 0 && (nameKey.Contains(localKey) || localKey.Contains(nameKey)))
            {
                partialMatch = ingredient;
                partialMatchCount++;
            }
        }

        if (partialMatchCount != 1)
        {
            return false;
        }

        cardId = partialMatch.id;
        return true;
    }

    public static List<NarafinSetupIngredient> GetIngredients(NarafinRulesetSetupDefinition catalog)
    {
        List<NarafinSetupIngredient> ingredients = new List<NarafinSetupIngredient>();
        if (catalog?.ingredients == null)
        {
            return ingredients;
        }

        foreach (NarafinSetupIngredient ingredient in catalog.ingredients)
        {
            if (ingredient != null && !string.IsNullOrWhiteSpace(ingredient.id))
            {
                ingredients.Add(ingredient);
            }
        }

        return ingredients;
    }

    public static NarafinSetupIngredient FindIngredient(NarafinRulesetSetupDefinition catalog, string nameOrCardId)
    {
        if (!TryResolveIngredientCardId(catalog, nameOrCardId, out string cardId))
        {
            return null;
        }

        foreach (NarafinSetupIngredient ingredient in GetIngredients(catalog))
        {
            if (string.Equals(ingredient.id, cardId, StringComparison.Ordinal))
            {
                return ingredient;
            }
        }

        return null;
    }

    public static string NormalizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(value.Length);
        foreach (char c in value)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }
}
