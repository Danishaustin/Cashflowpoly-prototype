using UnityEngine.UIElements;

public partial class UIManagerPlay
{
    private bool risikoCanUseAsuransi;
    private bool risikoCanPayBank;
    private bool risikoCanJualKebutuhan;
    private bool risikoCanJualEmas;
    private bool risikoCanPinjamanSyariah;
    private int risikoJualEmasMax = 1;

    public const string RisikoKartuButtonPrefix = "RisikoKartu_";

    private VisualElement risikoKartuList;
    private Button risikoNextButton;

    private void BindRisikoKehidupanElements(VisualElement root)
    {
        risikoKartuList = root.Q<VisualElement>("RisikoKartuList");
        risikoNextButton = root.Q<Button>("NextButtonRisikoKehidupan");
        BuildRisikoKartuButtons();

        risikoSetupContent = root.Q<VisualElement>("RisikoSetupContent");
        risikoCoinDecisionContent = root.Q<VisualElement>("RisikoCoinDecisionContent");
        risikoCoinInputContainer = root.Q<VisualElement>("RisikoCoinInputContainer");
        risikoDapatCoinInputContainer = root.Q<VisualElement>("RisikoDapatCoinInputContainer");
        risikoDampakInputContainer = root.Q<VisualElement>("RisikoDampakInputContainer");
        risikoJualEmasInputContainer = root.Q<VisualElement>("RisikoJualEmasInputContainer");
        risikoInvestasiEmasToggle = root.Q<Toggle>("RisikoInvestasiEmasToggle");
        risikoBayarBankSetupToggle = root.Q<Toggle>("RisikoBayarBankSetupToggle");
        risikoSemuaPemainToggle = root.Q<Toggle>("RisikoSemuaPemainToggle");
        risikoDapatCoinToggle = root.Q<Toggle>("RisikoDapatCoinToggle");
        risikoDariTiapPemainToggle = root.Q<Toggle>("RisikoDariTiapPemainToggle");
        risikoPerubahanHargaToggle = root.Q<Toggle>("RisikoPerubahanHargaToggle");
        risikoUseAsuransiToggle = root.Q<Toggle>("RisikoUseAsuransiToggle");
        risikoBayarBankToggle = root.Q<Toggle>("RisikoBayarBankToggle");
        risikoJualKebutuhanToggle = root.Q<Toggle>("RisikoJualKebutuhanToggle");
        risikoJualEmasToggle = root.Q<Toggle>("RisikoJualEmasToggle");
        risikoPinjamanSyariahToggle = root.Q<Toggle>("RisikoPinjamanSyariahToggle");

        risikoInvestasiEmasToggle?.RegisterValueChangedCallback(_ => RefreshRisikoKehidupanToggleState());
        risikoBayarBankSetupToggle?.RegisterValueChangedCallback(_ => RefreshRisikoKehidupanToggleState());
        risikoDapatCoinToggle?.RegisterValueChangedCallback(_ => RefreshRisikoKehidupanToggleState());
        risikoPerubahanHargaToggle?.RegisterValueChangedCallback(_ => RefreshRisikoKehidupanToggleState());
        risikoSemuaPemainToggle?.RegisterValueChangedCallback(_ => HideRisikoKehidupanWarning());
        risikoDariTiapPemainToggle?.RegisterValueChangedCallback(_ => HideRisikoKehidupanWarning());
        risikoUseAsuransiToggle?.RegisterValueChangedCallback(_ => RefreshRisikoCoinDecisionToggleState());
        risikoBayarBankToggle?.RegisterValueChangedCallback(_ => RefreshRisikoCoinDecisionToggleState());
        risikoJualKebutuhanToggle?.RegisterValueChangedCallback(_ => RefreshRisikoCoinDecisionToggleState());
        risikoJualEmasToggle?.RegisterValueChangedCallback(_ => RefreshRisikoCoinDecisionToggleState());
        risikoPinjamanSyariahToggle?.RegisterValueChangedCallback(_ => RefreshRisikoCoinDecisionToggleState());
    }

    public void ResetRisikoKehidupanPanel()
    {
        ShowRisikoSetupContent();
        risikoInvestasiEmasToggle?.SetValueWithoutNotify(false);
        risikoBayarBankSetupToggle?.SetValueWithoutNotify(false);
        risikoSemuaPemainToggle?.SetValueWithoutNotify(false);
        risikoDapatCoinToggle?.SetValueWithoutNotify(false);
        risikoDariTiapPemainToggle?.SetValueWithoutNotify(false);
        risikoPerubahanHargaToggle?.SetValueWithoutNotify(false);
        risikoUseAsuransiToggle?.SetValueWithoutNotify(false);
        risikoBayarBankToggle?.SetValueWithoutNotify(false);
        risikoJualKebutuhanToggle?.SetValueWithoutNotify(false);
        risikoJualEmasToggle?.SetValueWithoutNotify(false);
        risikoPinjamanSyariahToggle?.SetValueWithoutNotify(false);
        UpdateRisikoCoinChangeText(1);
        UpdateRisikoDapatCoinText(1);
        UpdateRisikoHargaChangeText(0);
        SetRisikoJualEmasRange(1, 1);
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

    public void UpdateRisikoDapatCoinText(int amount)
    {
        if (risikoDapatCoinText != null)
        {
            risikoDapatCoinText.text = amount.ToString();
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

    public void SetRisikoJualEmasRange(int amount, int maxAmount)
    {
        risikoJualEmasMax = UnityEngine.Mathf.Max(1, maxAmount);
        int clampedAmount = UnityEngine.Mathf.Clamp(amount, 1, risikoJualEmasMax);
        if (risikoJualEmasText != null)
        {
            risikoJualEmasText.text = clampedAmount.ToString();
        }

        HideRisikoKehidupanWarning();
    }

    public int GetRisikoJualEmasAmount()
    {
        if (risikoJualEmasText == null || string.IsNullOrWhiteSpace(risikoJualEmasText.text))
        {
            return 1;
        }

        if (!int.TryParse(risikoJualEmasText.text, out int amount))
        {
            return 1;
        }

        return UnityEngine.Mathf.Clamp(amount, 1, risikoJualEmasMax);
    }

    public void ChangeRisikoJualEmasAmount(int delta)
    {
        int next = GetRisikoJualEmasAmount() + delta;
        SetRisikoJualEmasRange(next, risikoJualEmasMax);
    }

    public bool IsRisikoKehidupanInputValid(int bayarBankAmount, int dapatCoinAmount, int hargaChange, out string warningText)
    {
        bool investasiSelected = risikoInvestasiEmasToggle != null && risikoInvestasiEmasToggle.value;
        bool bayarBankSelected = risikoBayarBankSetupToggle != null && risikoBayarBankSetupToggle.value;
        bool dapatCoinSelected = risikoDapatCoinToggle != null && risikoDapatCoinToggle.value;
        bool perubahanHargaSelected = risikoPerubahanHargaToggle != null && risikoPerubahanHargaToggle.value;

        if (!investasiSelected && !bayarBankSelected && !dapatCoinSelected && !perubahanHargaSelected)
        {
            warningText = "Pilih salah satu risiko terlebih dahulu.";
            return false;
        }

        if (bayarBankSelected && bayarBankAmount <= 0)
        {
            warningText = "Nominal Bayar ke Bank harus lebih dari 0.";
            return false;
        }

        if (dapatCoinSelected && dapatCoinAmount <= 0)
        {
            warningText = "Nominal Dapat Coin harus lebih dari 0.";
            return false;
        }

        if (perubahanHargaSelected)
        {
            if (hargaChange == 0)
            {
                warningText = "Isi perubahan harga selain 0.";
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
        return risikoBayarBankSetupToggle != null && risikoBayarBankSetupToggle.value;
    }

    public bool IsRisikoSemuaPemainSelected()
    {
        return risikoSemuaPemainToggle != null && risikoSemuaPemainToggle.value;
    }

    public bool IsRisikoDapatCoinSelected()
    {
        return risikoDapatCoinToggle != null && risikoDapatCoinToggle.value;
    }

    public bool IsRisikoDariTiapPemainSelected()
    {
        return risikoDariTiapPemainToggle != null && risikoDariTiapPemainToggle.value;
    }

    public bool IsRisikoPerubahanHargaSelected()
    {
        return risikoPerubahanHargaToggle != null && risikoPerubahanHargaToggle.value;
    }

    public bool IsRisikoCoinDecisionInputValid(out string warningText)
    {
        bool useAsuransi = risikoUseAsuransiToggle != null && risikoUseAsuransiToggle.value;
        bool bayarBank = risikoBayarBankToggle != null && risikoBayarBankToggle.value;
        bool jualKebutuhan = risikoJualKebutuhanToggle != null && risikoJualKebutuhanToggle.value;
        bool jualEmas = risikoJualEmasToggle != null && risikoJualEmasToggle.value;
        bool pinjamanSyariah = risikoPinjamanSyariahToggle != null && risikoPinjamanSyariahToggle.value;

        if (!useAsuransi && !bayarBank && !jualKebutuhan && !jualEmas && !pinjamanSyariah)
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

    public bool IsRisikoJualKebutuhanSelected()
    {
        return risikoJualKebutuhanToggle != null && risikoJualKebutuhanToggle.value;
    }

    public bool IsRisikoJualEmasSelected()
    {
        return risikoJualEmasToggle != null && risikoJualEmasToggle.value;
    }

    public bool IsRisikoPinjamanSyariahSelected()
    {
        return risikoPinjamanSyariahToggle != null && risikoPinjamanSyariahToggle.value;
    }

    public bool IsRisikoJualEmasInputValid(int maxEmas, out string warningText)
    {
        int amount = GetRisikoJualEmasAmount();
        if (maxEmas <= 0)
        {
            warningText = "Emas tidak cukup untuk dijual.";
            return false;
        }

        if (amount < 1 || amount > maxEmas)
        {
            warningText = "Jumlah emas harus antara 1 sampai " + maxEmas + ".";
            return false;
        }

        warningText = string.Empty;
        return true;
    }

    public void ShowRisikoCoinDecisionContent(
        string playerName,
        bool canUseAsuransi,
        bool canPayBank,
        bool canJualKebutuhan,
        bool canJualEmas,
        bool canPinjamanSyariah,
        int maxJualEmas)
    {
        risikoCanUseAsuransi = canUseAsuransi;
        risikoCanPayBank = canPayBank;
        risikoCanJualKebutuhan = canJualKebutuhan;
        risikoCanJualEmas = canJualEmas;
        risikoCanPinjamanSyariah = canPinjamanSyariah;
        SetRisikoJualEmasRange(1, maxJualEmas);

        SetRisikoContentVisible(false, true, false);

        if (risikoDecisionPlayerText != null)
        {
            risikoDecisionPlayerText.text = "Keputusan Player: " + playerName;
        }

        risikoUseAsuransiToggle?.SetValueWithoutNotify(false);
        risikoBayarBankToggle?.SetValueWithoutNotify(false);
        risikoJualKebutuhanToggle?.SetValueWithoutNotify(false);
        risikoJualEmasToggle?.SetValueWithoutNotify(false);
        risikoPinjamanSyariahToggle?.SetValueWithoutNotify(false);

        risikoUseAsuransiToggle?.SetEnabled(canUseAsuransi);
        risikoBayarBankToggle?.SetEnabled(canPayBank);
        risikoJualKebutuhanToggle?.SetEnabled(canJualKebutuhan);
        risikoJualEmasToggle?.SetEnabled(canJualEmas);
        risikoPinjamanSyariahToggle?.SetEnabled(canPinjamanSyariah);

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
        bool bayarBankSelected = risikoBayarBankSetupToggle != null && risikoBayarBankSetupToggle.value;
        bool dapatCoinSelected = risikoDapatCoinToggle != null && risikoDapatCoinToggle.value;
        bool perubahanHargaSelected = risikoPerubahanHargaToggle != null && risikoPerubahanHargaToggle.value;
        bool hasMainSelection = investasiSelected || bayarBankSelected || dapatCoinSelected || perubahanHargaSelected;

        HideRisikoKehidupanWarning();

        risikoInvestasiEmasToggle?.SetEnabled(!hasMainSelection || investasiSelected);
        risikoBayarBankSetupToggle?.SetEnabled(!hasMainSelection || bayarBankSelected);
        risikoDapatCoinToggle?.SetEnabled(!hasMainSelection || dapatCoinSelected);
        risikoPerubahanHargaToggle?.SetEnabled(!hasMainSelection || perubahanHargaSelected);

        if (risikoCoinInputContainer != null)
        {
            risikoCoinInputContainer.style.display = bayarBankSelected ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (risikoSemuaPemainToggle != null)
        {
            risikoSemuaPemainToggle.style.display = bayarBankSelected ? DisplayStyle.Flex : DisplayStyle.None;
            if (!bayarBankSelected)
            {
                risikoSemuaPemainToggle.SetValueWithoutNotify(false);
            }
        }

        if (risikoDapatCoinInputContainer != null)
        {
            risikoDapatCoinInputContainer.style.display = dapatCoinSelected ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (risikoDariTiapPemainToggle != null)
        {
            risikoDariTiapPemainToggle.style.display = dapatCoinSelected ? DisplayStyle.Flex : DisplayStyle.None;
            if (!dapatCoinSelected)
            {
                risikoDariTiapPemainToggle.SetValueWithoutNotify(false);
            }
        }

        if (risikoDampakInputContainer != null)
        {
            risikoDampakInputContainer.style.display = perubahanHargaSelected ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void RefreshRisikoCoinDecisionToggleState()
    {
        bool useAsuransi = risikoUseAsuransiToggle != null && risikoUseAsuransiToggle.value;
        bool bayarBank = risikoBayarBankToggle != null && risikoBayarBankToggle.value;
        bool jualKebutuhan = risikoJualKebutuhanToggle != null && risikoJualKebutuhanToggle.value;
        bool jualEmas = risikoJualEmasToggle != null && risikoJualEmasToggle.value;
        bool pinjamanSyariah = risikoPinjamanSyariahToggle != null && risikoPinjamanSyariahToggle.value;
        bool hasDecision = useAsuransi || bayarBank || jualKebutuhan || jualEmas || pinjamanSyariah;
        HideRisikoKehidupanWarning();

        risikoUseAsuransiToggle?.SetEnabled((!hasDecision && risikoCanUseAsuransi) || useAsuransi);
        risikoBayarBankToggle?.SetEnabled((!hasDecision && risikoCanPayBank) || bayarBank);
        risikoJualKebutuhanToggle?.SetEnabled((!hasDecision && risikoCanJualKebutuhan) || jualKebutuhan);
        risikoJualEmasToggle?.SetEnabled((!hasDecision && risikoCanJualEmas) || jualEmas);
        risikoPinjamanSyariahToggle?.SetEnabled((!hasDecision && risikoCanPinjamanSyariah) || pinjamanSyariah);

        if (risikoJualEmasInputContainer != null)
        {
            risikoJualEmasInputContainer.style.display = jualEmas ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
