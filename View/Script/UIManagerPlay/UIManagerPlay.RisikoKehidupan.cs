using UnityEngine.UIElements;

public partial class UIManagerPlay
{
    private bool risikoCanUseAsuransi;
    private bool risikoCanPayBank;

    private void BindRisikoKehidupanElements(VisualElement root)
    {
        risikoSetupContent = root.Q<VisualElement>("RisikoSetupContent");
        risikoCoinDecisionContent = root.Q<VisualElement>("RisikoCoinDecisionContent");
        risikoCoinInputContainer = root.Q<VisualElement>("RisikoCoinInputContainer");
        risikoDampakInputContainer = root.Q<VisualElement>("RisikoDampakInputContainer");
        risikoInvestasiEmasToggle = root.Q<Toggle>("RisikoInvestasiEmasToggle");
        risikoCoinToggle = root.Q<Toggle>("RisikoCoinToggle");
        risikoDampakSepekanToggle = root.Q<Toggle>("RisikoDampakSepekanToggle");
        risikoHargaBahanToggle = root.Q<Toggle>("RisikoHargaBahanToggle");
        risikoHargaKebutuhanToggle = root.Q<Toggle>("RisikoHargaKebutuhanToggle");
        risikoUseAsuransiToggle = root.Q<Toggle>("RisikoUseAsuransiToggle");
        risikoBayarBankToggle = root.Q<Toggle>("RisikoBayarBankToggle");
        risikoTidakCukupToggle = root.Q<Toggle>("RisikoTidakCukupToggle");

        risikoInvestasiEmasToggle?.RegisterValueChangedCallback(_ => RefreshRisikoKehidupanToggleState());
        risikoCoinToggle?.RegisterValueChangedCallback(_ => RefreshRisikoKehidupanToggleState());
        risikoDampakSepekanToggle?.RegisterValueChangedCallback(_ => RefreshRisikoKehidupanToggleState());
        risikoHargaBahanToggle?.RegisterValueChangedCallback(_ => HideRisikoKehidupanWarning());
        risikoHargaKebutuhanToggle?.RegisterValueChangedCallback(_ => HideRisikoKehidupanWarning());
        risikoUseAsuransiToggle?.RegisterValueChangedCallback(_ => RefreshRisikoCoinDecisionToggleState());
        risikoBayarBankToggle?.RegisterValueChangedCallback(_ => RefreshRisikoCoinDecisionToggleState());
        risikoTidakCukupToggle?.RegisterValueChangedCallback(_ => RefreshRisikoCoinDecisionToggleState());
    }

    public void ResetRisikoKehidupanPanel()
    {
        ShowRisikoSetupContent();
        risikoInvestasiEmasToggle?.SetValueWithoutNotify(false);
        risikoCoinToggle?.SetValueWithoutNotify(false);
        risikoDampakSepekanToggle?.SetValueWithoutNotify(false);
        risikoHargaBahanToggle?.SetValueWithoutNotify(false);
        risikoHargaKebutuhanToggle?.SetValueWithoutNotify(false);
        risikoUseAsuransiToggle?.SetValueWithoutNotify(false);
        risikoBayarBankToggle?.SetValueWithoutNotify(false);
        risikoTidakCukupToggle?.SetValueWithoutNotify(false);
        UpdateRisikoCoinChangeText(0);
        UpdateRisikoHargaChangeText(0);
        HideRisikoKehidupanWarning();
        RefreshRisikoKehidupanToggleState();
    }

    public void UpdateRisikoCoinChangeText(int amount)
    {
        if (risikoCoinChangeText != null)
        {
            risikoCoinChangeText.text = amount.ToString();
        }

        HideRisikoKehidupanWarning();
    }

    public void UpdateRisikoHargaChangeText(int amount)
    {
        if (risikoHargaChangeText != null)
        {
            risikoHargaChangeText.text = amount.ToString();
        }

        HideRisikoKehidupanWarning();
    }

    public bool IsRisikoKehidupanInputValid(int coinChange, int hargaChange, out string warningText)
    {
        bool investasiSelected = risikoInvestasiEmasToggle != null && risikoInvestasiEmasToggle.value;
        bool coinSelected = risikoCoinToggle != null && risikoCoinToggle.value;
        bool dampakSelected = risikoDampakSepekanToggle != null && risikoDampakSepekanToggle.value;

        if (!investasiSelected && !coinSelected && !dampakSelected)
        {
            warningText = "Pilih salah satu risiko terlebih dahulu.";
            return false;
        }

        if (coinSelected && coinChange == 0)
        {
            warningText = "Isi perubahan coin selain 0.";
            return false;
        }

        if (dampakSelected)
        {
            bool hargaBahanSelected = risikoHargaBahanToggle != null && risikoHargaBahanToggle.value;
            bool hargaKebutuhanSelected = risikoHargaKebutuhanToggle != null && risikoHargaKebutuhanToggle.value;

            if (hargaChange == 0)
            {
                warningText = "Isi perubahan harga selain 0.";
                return false;
            }

            if (!hargaBahanSelected && !hargaKebutuhanSelected)
            {
                warningText = "Pilih Harga Bahan atau Harga Kebutuhan.";
                return false;
            }
        }

        warningText = string.Empty;
        return true;
    }

    public bool IsRisikoInvestasiEmasSelected()
    {
        return risikoInvestasiEmasToggle != null && risikoInvestasiEmasToggle.value;
    }

    public bool IsRisikoCoinSelected()
    {
        return risikoCoinToggle != null && risikoCoinToggle.value;
    }

    public bool IsRisikoCoinDecisionInputValid(out string warningText)
    {
        bool useAsuransi = risikoUseAsuransiToggle != null && risikoUseAsuransiToggle.value;
        bool bayarBank = risikoBayarBankToggle != null && risikoBayarBankToggle.value;
        bool tidakCukup = risikoTidakCukupToggle != null && risikoTidakCukupToggle.value;

        if (!useAsuransi && !bayarBank && !tidakCukup)
        {
            warningText = "Pilih keputusan untuk player ini.";
            return false;
        }

        warningText = string.Empty;
        return true;
    }

    public bool IsRisikoUseAsuransiSelected()
    {
        return risikoUseAsuransiToggle != null && risikoUseAsuransiToggle.value;
    }

    public bool IsRisikoBayarBankSelected()
    {
        return risikoBayarBankToggle != null && risikoBayarBankToggle.value;
    }

    public void ShowRisikoCoinDecisionContent(string playerName, bool canUseAsuransi, bool canPayBank)
    {
        risikoCanUseAsuransi = canUseAsuransi;
        risikoCanPayBank = canPayBank;

        if (risikoSetupContent != null)
        {
            risikoSetupContent.style.display = DisplayStyle.None;
        }

        if (risikoCoinDecisionContent != null)
        {
            risikoCoinDecisionContent.style.display = DisplayStyle.Flex;
        }

        if (risikoDecisionPlayerText != null)
        {
            risikoDecisionPlayerText.text = "Keputusan Player: " + playerName;
        }

        risikoUseAsuransiToggle?.SetValueWithoutNotify(false);
        risikoBayarBankToggle?.SetValueWithoutNotify(false);
        risikoTidakCukupToggle?.SetValueWithoutNotify(false);

        risikoUseAsuransiToggle?.SetEnabled(canUseAsuransi);
        risikoBayarBankToggle?.SetEnabled(canPayBank);
        risikoTidakCukupToggle?.SetEnabled(true);

        HideRisikoKehidupanWarning();
        RefreshRisikoCoinDecisionToggleState();
    }

    private void ShowRisikoSetupContent()
    {
        if (risikoSetupContent != null)
        {
            risikoSetupContent.style.display = DisplayStyle.Flex;
        }

        if (risikoCoinDecisionContent != null)
        {
            risikoCoinDecisionContent.style.display = DisplayStyle.None;
        }
    }

    public void ShowRisikoKehidupanWarning(string warningText)
    {
        if (risikoWarningText == null)
        {
            return;
        }

        risikoWarningText.text = warningText;
        risikoWarningText.style.display = DisplayStyle.Flex;
    }

    public void HideRisikoKehidupanWarning()
    {
        if (risikoWarningText == null)
        {
            return;
        }

        risikoWarningText.text = string.Empty;
        risikoWarningText.style.display = DisplayStyle.None;
    }

    private void RefreshRisikoKehidupanToggleState()
    {
        bool investasiSelected = risikoInvestasiEmasToggle != null && risikoInvestasiEmasToggle.value;
        bool coinSelected = risikoCoinToggle != null && risikoCoinToggle.value;
        bool dampakSelected = risikoDampakSepekanToggle != null && risikoDampakSepekanToggle.value;
        bool hasMainSelection = investasiSelected || coinSelected || dampakSelected;

        HideRisikoKehidupanWarning();

        risikoInvestasiEmasToggle?.SetEnabled(!hasMainSelection || investasiSelected);
        risikoCoinToggle?.SetEnabled(!hasMainSelection || coinSelected);
        risikoDampakSepekanToggle?.SetEnabled(!hasMainSelection || dampakSelected);

        if (risikoCoinInputContainer != null)
        {
            risikoCoinInputContainer.style.display = coinSelected ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (risikoDampakInputContainer != null)
        {
            risikoDampakInputContainer.style.display = dampakSelected ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void RefreshRisikoCoinDecisionToggleState()
    {
        bool useAsuransi = risikoUseAsuransiToggle != null && risikoUseAsuransiToggle.value;
        bool bayarBank = risikoBayarBankToggle != null && risikoBayarBankToggle.value;
        bool tidakCukup = risikoTidakCukupToggle != null && risikoTidakCukupToggle.value;
        bool hasDecision = useAsuransi || bayarBank || tidakCukup;
        HideRisikoKehidupanWarning();

        risikoUseAsuransiToggle?.SetEnabled((!hasDecision && risikoCanUseAsuransi) || useAsuransi);
        risikoBayarBankToggle?.SetEnabled((!hasDecision && risikoCanPayBank) || bayarBank);
        risikoTidakCukupToggle?.SetEnabled(!hasDecision || tidakCukup);
    }
}
