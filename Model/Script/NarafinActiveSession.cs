using System;
using System.Collections.Generic;
using System.Text;

public class NarafinSetupStartResult
{
    public bool Success;
    public string ErrorCode;
    public string ErrorMessage;
    public List<NarafinSessionStatePlayer> Players;

    // Event session setelah start (termasuk SetupPinjamanAwal dan SetupAsuransiAwal); null bila tidak dibaca.
    public List<NarafinSessionEventSummary> SetupEvents;
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

    // True setelah server gagal dihubungi (timeout, 5xx, tidak terjangkau) atau nomor urut event ditolak.
    // Sisa sesi berjalan tanpa server; kembali online saat sesi baru dibuat.
    public static bool IsServerOffline { get; private set; }
    private static bool hasPendingOfflineNotice;

    public static void SwitchToServerOffline(string reason)
    {
        if (IsServerOffline)
        {
            return;
        }

        IsServerOffline = true;
        hasPendingOfflineNotice = true;
        UnityEngine.Debug.LogWarning("Narafin beralih ke mode offline untuk sisa sesi: " + reason);
    }

    // Mengembalikan true sekali setelah peralihan offline agar pemain diberi tahu satu kali.
    // Sesi yang ditutup backend (heartbeat 404 atau 409) tidak bisa dilanjutkan sama sekali.
    public static bool IsSessionClosedByServer { get; private set; }
    public static string SessionClosedMessage { get; private set; } = string.Empty;
    public static string HeartbeatExpiresAt { get; private set; } = string.Empty;
    private static bool hasPendingSessionClosedNotice;

    // Antrean event ditahan selama sinkronisasi setelah aplikasi dibuka kembali.
    public static bool IsEventSendingHeld { get; private set; }

    public static void MarkSessionClosedByServer(string reason)
    {
        if (IsSessionClosedByServer)
        {
            return;
        }

        IsSessionClosedByServer = true;
        hasPendingSessionClosedNotice = true;
        SessionClosedMessage = string.IsNullOrWhiteSpace(reason) ? "Sesi sudah ditutup server." : reason;
        UnityEngine.Debug.LogWarning("Sesi Narafin ditutup server: " + SessionClosedMessage);
    }

    // Sesi yang sudah diakhiri klien lewat POST /end; event berikutnya tidak dikirim, tanpa pesan peringatan.
    public static bool IsSessionEnded { get; private set; }
    public static string SessionEndStatus { get; private set; } = string.Empty;

    public static void MarkSessionEnded(string status)
    {
        IsSessionEnded = true;
        SessionEndStatus = status ?? string.Empty;
    }

    // Mengembalikan true sekali agar pemain diberi tahu satu kali bahwa sesi dihentikan.
    public static bool ConsumeSessionClosedNotice()
    {
        if (!hasPendingSessionClosedNotice)
        {
            return false;
        }

        hasPendingSessionClosedNotice = false;
        return true;
    }

    // Poin juara donasi dari ruleset session; server menolak nilai yang tidak sama dengan katalog.
    public static int GetDonationRankPoints(NarafinRulesetSetupDefinition catalog, int rank)
    {
        if (catalog?.donation_rank_points == null)
        {
            return 0;
        }

        foreach (NarafinSetupDonationRankPoint rankPoint in catalog.donation_rank_points)
        {
            if (rankPoint != null && rankPoint.rank == rank)
            {
                return rankPoint.points;
            }
        }

        return 0;
    }

    // Nama pemain seperti yang terdaftar di server; dipakai pada payload juara donasi.
    public static string GetServerPlayerName(int playerOrderNo)
    {
        foreach (NarafinSessionStatePlayer player in players)
        {
            if (player != null && player.player_order_no == playerOrderNo)
            {
                return player.name ?? string.Empty;
            }
        }

        return string.Empty;
    }

    // Versi state terakhir dari /state; dipakai untuk mendeteksi state yang sudah berubah.
    public static int StateVersion { get; private set; }

    public static void SetStateVersion(int stateVersion)
    {
        if (stateVersion > 0)
        {
            StateVersion = stateVersion;
        }
    }

    public static void SetHeartbeatExpiresAt(string expiresAt)
    {
        HeartbeatExpiresAt = expiresAt ?? string.Empty;
    }

    public static void SetEventSendingHold(bool isHeld)
    {
        IsEventSendingHeld = isHeld;
    }

    public static bool ConsumeOfflineNotice()
    {
        if (!hasPendingOfflineNotice)
        {
            return false;
        }

        hasPendingOfflineNotice = false;
        return true;
    }
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
        IsServerOffline = false;
        hasPendingOfflineNotice = false;
        IsSessionClosedByServer = false;
        hasPendingSessionClosedNotice = false;
        SessionClosedMessage = string.Empty;
        HeartbeatExpiresAt = string.Empty;
        IsEventSendingHeld = false;
        StateVersion = 0;
        IsSessionEnded = false;
        SessionEndStatus = string.Empty;

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

    public static List<NarafinSetupNeed> GetNeeds(NarafinRulesetSetupDefinition catalog)
    {
        List<NarafinSetupNeed> needs = new List<NarafinSetupNeed>();
        if (catalog?.needs == null)
        {
            return needs;
        }

        foreach (NarafinSetupNeed need in catalog.needs)
        {
            if (need != null && !string.IsNullOrWhiteSpace(need.id))
            {
                needs.Add(need);
            }
        }

        return needs;
    }

    public static NarafinSetupNeed FindNeed(NarafinRulesetSetupDefinition catalog, string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return null;
        }

        foreach (NarafinSetupNeed need in GetNeeds(catalog))
        {
            if (string.Equals(need.id, cardId, StringComparison.Ordinal))
            {
                return need;
            }
        }

        return null;
    }

    public static string GetNeedFamily(NarafinSetupNeed need)
    {
        if (need == null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(need.family) ? need.id : need.family;
    }

    // Satu kartu per family sesuai urutan katalog; dipakai sebagai tombol jenis kebutuhan.
    public static List<NarafinSetupNeed> GetNeedFamilies(NarafinRulesetSetupDefinition catalog)
    {
        List<NarafinSetupNeed> families = new List<NarafinSetupNeed>();
        HashSet<string> seenFamilies = new HashSet<string>(StringComparer.Ordinal);

        foreach (NarafinSetupNeed need in GetNeeds(catalog))
        {
            if (seenFamilies.Add(GetNeedFamily(need)))
            {
                families.Add(need);
            }
        }

        return families;
    }

    // Varian kartu dalam satu family, diurutkan dari harga termurah.
    public static List<NarafinSetupNeed> GetNeedVariants(NarafinRulesetSetupDefinition catalog, string family)
    {
        List<NarafinSetupNeed> variants = new List<NarafinSetupNeed>();
        if (string.IsNullOrWhiteSpace(family))
        {
            return variants;
        }

        foreach (NarafinSetupNeed need in GetNeeds(catalog))
        {
            if (string.Equals(GetNeedFamily(need), family, StringComparison.Ordinal))
            {
                variants.Add(need);
            }
        }

        variants.Sort((a, b) =>
        {
            int priceCompare = a.hargaBeli.CompareTo(b.hargaBeli);
            return priceCompare != 0 ? priceCompare : string.CompareOrdinal(a.id, b.id);
        });
        return variants;
    }

    // need_tier wajib dikirim pada event Kebutuhan; tanpa itu server menolak sebagian besar kartu.
    public static string GetNeedTierCode(string tipe)
    {
        switch ((tipe ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "primer":
                return "PRIMARY";
            case "sekunder":
                return "SECONDARY";
            case "tersier":
                return "TERTIARY";
            default:
                return (tipe ?? string.Empty).Trim().ToUpperInvariant();
        }
    }

    public static List<NarafinSetupFinancialGoal> GetFinancialGoals(NarafinRulesetSetupDefinition catalog)
    {
        List<NarafinSetupFinancialGoal> goals = new List<NarafinSetupFinancialGoal>();
        if (catalog?.financial_goals == null)
        {
            return goals;
        }

        foreach (NarafinSetupFinancialGoal goal in catalog.financial_goals)
        {
            if (goal != null && !string.IsNullOrWhiteSpace(goal.id))
            {
                goals.Add(goal);
            }
        }

        return goals;
    }

    public static NarafinSetupFinancialGoal FindFinancialGoal(NarafinRulesetSetupDefinition catalog, string goalId)
    {
        if (string.IsNullOrWhiteSpace(goalId))
        {
            return null;
        }

        foreach (NarafinSetupFinancialGoal goal in GetFinancialGoals(catalog))
        {
            if (string.Equals(goal.id, goalId, StringComparison.Ordinal))
            {
                return goal;
            }
        }

        return null;
    }

    // Harga dari Kartu Harga Emas ruleset (unik, urut dari termurah); server menolak harga di luar daftar ini.
    public static List<int> GetGoldPrices(NarafinRulesetSetupDefinition catalog)
    {
        List<int> prices = new List<int>();
        if (catalog?.gold_prices == null)
        {
            return prices;
        }

        foreach (NarafinSetupGoldPrice goldPrice in catalog.gold_prices)
        {
            if (goldPrice != null && goldPrice.unit_price > 0 && !prices.Contains(goldPrice.unit_price))
            {
                prices.Add(goldPrice.unit_price);
            }
        }

        prices.Sort();
        return prices;
    }

    public static List<NarafinSetupOrder> GetOrders(NarafinRulesetSetupDefinition catalog)
    {
        List<NarafinSetupOrder> orders = new List<NarafinSetupOrder>();
        if (catalog?.orders == null)
        {
            return orders;
        }

        foreach (NarafinSetupOrder order in catalog.orders)
        {
            if (order != null && !string.IsNullOrWhiteSpace(order.id))
            {
                orders.Add(order);
            }
        }

        return orders;
    }

    public static NarafinSetupOrder FindOrder(NarafinRulesetSetupDefinition catalog, string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            return null;
        }

        foreach (NarafinSetupOrder order in GetOrders(catalog))
        {
            if (string.Equals(order.id, orderId, StringComparison.Ordinal))
            {
                return order;
            }
        }

        return null;
    }

    public static List<NarafinSetupLifeRisk> GetLifeRisks(NarafinRulesetSetupDefinition catalog)
    {
        List<NarafinSetupLifeRisk> risks = new List<NarafinSetupLifeRisk>();
        if (catalog?.life_risks == null)
        {
            return risks;
        }

        foreach (NarafinSetupLifeRisk risk in catalog.life_risks)
        {
            if (risk != null && !string.IsNullOrWhiteSpace(risk.risk_code))
            {
                risks.Add(risk);
            }
        }

        return risks;
    }

    public static NarafinSetupLifeRisk FindLifeRisk(NarafinRulesetSetupDefinition catalog, string riskCode)
    {
        if (string.IsNullOrWhiteSpace(riskCode))
        {
            return null;
        }

        foreach (NarafinSetupLifeRisk risk in GetLifeRisks(catalog))
        {
            if (string.Equals(risk.risk_code, riskCode, StringComparison.Ordinal))
            {
                return risk;
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
