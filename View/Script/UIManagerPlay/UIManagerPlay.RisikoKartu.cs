using UnityEngine.UIElements;

public partial class UIManagerPlay
{
    // Satu tombol per kartu life_risks ruleset session; pemain memilih kartu fisik yang ditarik.
    private void BuildRisikoKartuButtons()
    {
        if (risikoKartuList == null)
        {
            return;
        }

        risikoKartuList.Clear();
        foreach (NarafinSetupLifeRisk risk in NarafinActiveSession.GetLifeRisks(NarafinActiveSession.Catalog))
        {
            var button = new Button
            {
                name = RisikoKartuButtonPrefix + risk.risk_code,
                text = GetRisikoKartuLabel(risk)
            };
            button.AddToClassList("choice-button");
            button.AddToClassList("risk-card-button");
            risikoKartuList.Add(button);
        }
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
                    + " selama " + risk.duration_days + " hari)";
            case "GOLD_TRADE":
                return riskName + " (transaksi emas)";
            default:
                return riskName;
        }
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

    private void SetRisikoContentVisible(bool showSetup, bool showDecision, bool showKartuList)
    {
        if (risikoSetupContent != null)
        {
            risikoSetupContent.style.display = showSetup ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (risikoCoinDecisionContent != null)
        {
            risikoCoinDecisionContent.style.display = showDecision ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (risikoKartuList != null)
        {
            risikoKartuList.style.display = showKartuList ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Kartu dipilih langsung dari daftar, jadi tombol Next hanya dipakai panel lain.
        if (risikoNextButton != null)
        {
            risikoNextButton.style.display = showKartuList ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
