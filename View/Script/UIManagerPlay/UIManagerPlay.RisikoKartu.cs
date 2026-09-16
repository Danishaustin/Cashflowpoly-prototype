using System.Collections.Generic;
using UnityEngine.UIElements;

public partial class UIManagerPlay
{
    private readonly Dictionary<string, string> risikoKartuOptionLookup = new Dictionary<string, string>();

    // Satu baris dropdown per kartu life_risks ruleset session; pemain memilih kartu fisik yang ditarik,
    // lalu menekan Next untuk mengirimkannya.
    private void BuildRisikoKartuDropdown()
    {
        risikoKartuOptionLookup.Clear();
        if (risikoKartuDropdown == null)
        {
            return;
        }

        List<string> options = new List<string>();
        foreach (NarafinSetupLifeRisk risk in NarafinActiveSession.GetLifeRisks(NarafinActiveSession.Catalog))
        {
            string label = GetRisikoKartuLabel(risk);
            string uniqueLabel = label;
            int duplicateCount = 2;
            while (risikoKartuOptionLookup.ContainsKey(uniqueLabel))
            {
                uniqueLabel = label + " (" + duplicateCount + ")";
                duplicateCount++;
            }

            risikoKartuOptionLookup[uniqueLabel] = risk.risk_code;
            options.Add(uniqueLabel);
        }

        risikoKartuDropdown.choices = options;
        risikoKartuDropdown.SetValueWithoutNotify(options.Count > 0 ? options[0] : string.Empty);
    }

    private static string GetRisikoKartuLabel(NarafinSetupLifeRisk risk)
    {
        string riskName = string.IsNullOrWhiteSpace(risk.item_name) ? risk.risk_code : risk.item_name;
        string sign = string.Equals(risk.direction, "IN", System.StringComparison.OrdinalIgnoreCase) ? "+" : "-";

        switch (risk.effect_type)
        {
            case "COIN_EFFECT":
                return riskName + " (" + sign + risk.amount + " koin)";
            case "ALL_PLAYERS_COIN_EFFECT":
                return riskName + " (semua pemain " + sign + risk.amount + " koin)";
            case "PLAYER_TO_PLAYER_TRANSFER":
                return riskName + " (" + risk.amount + " koin dari tiap pemain lain)";
            case "INGREDIENT_PRICE_MODIFIER":
                return riskName + " (harga bahan " + (risk.value_delta >= 0 ? "+" : string.Empty) + risk.value_delta
                    + ", " + risk.duration_days + " hari)";
            case "GOLD_TRADE":
                return riskName + " (transaksi emas)";
            default:
                return riskName;
        }
    }

    // risk_code kartu yang sedang dipilih; kosong berarti tidak ada pilihan yang sah.
    public string GetSelectedRisikoKartuCode()
    {
        string selectedOption = risikoKartuDropdown?.value;
        if (string.IsNullOrWhiteSpace(selectedOption))
        {
            return string.Empty;
        }

        return risikoKartuOptionLookup.TryGetValue(selectedOption, out string riskCode) ? riskCode : string.Empty;
    }

    public void ShowRisikoKartuContent()
    {
        SetRisikoContentVisible(false, false, true);
        HideRisikoKehidupanWarning();
    }

    public void SetRisikoDecisionTitle(string text)
    {
        if (risikoDecisionPlayerText != null)
        {
            risikoDecisionPlayerText.text = text;
        }
    }

    private void SetRisikoContentVisible(bool showSetup, bool showDecision, bool showKartu)
    {
        if (risikoSetupContent != null)
        {
            risikoSetupContent.style.display = showSetup ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (risikoCoinDecisionContent != null)
        {
            risikoCoinDecisionContent.style.display = showDecision ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (risikoKartuContent != null)
        {
            risikoKartuContent.style.display = showKartu ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
