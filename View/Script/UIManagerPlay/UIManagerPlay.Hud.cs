using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManagerPlay
{
    private static readonly string[] DayNames =
    {
        "Senin",
        "Selasa",
        "Rabu",
        "Kamis",
        "Jumat",
        "Sabtu",
        "Minggu"
    };

    // HUD labels for player stats and counters.
    private void UpdateNameTag()
    {
        string playerNameKey = "PlayerName_" + GameState.Instance.turn;
        string fallbackName = "Player " + GameState.Instance.turn;
        string playerName = PlayerPrefs.GetString(playerNameKey, fallbackName);
        nameTag.text = playerName;
    }

    public void UpdateCoins(int coins)
    {
        Dialog(coinCounter, coins.ToString(), duration: 0.1f);
    }

    public void UpdateHappiness(int happiness)
    {
        Dialog(happinessCounter, happiness.ToString(), duration: 0.1f);
    }

    public void UpdateDay(int day)
    {
        dayCounter.text = "Day " + day;

        int week = ((day - 1) / 7) + 1;
        int dayIndex = Mathf.Abs(day - 1) % DayNames.Length;

        if (weekCounter != null)
        {
            weekCounter.text = "Minggu: " + week;
        }

        if (dayNameCounter != null)
        {
            dayNameCounter.text = DayNames[dayIndex];
        }
    }

    public void UpdatePlayerTurn(int turn)
    {
        playerTurn.text = "Player " + turn;
        RefreshPlayerContainerForTurn(turn);
        UpdateNameTag();
        UpdateMainChoiceButtonStates();
        if (inventoryPanel != null && inventoryPanel.style.display == UnityEngine.UIElements.DisplayStyle.Flex)
        {
            RefreshInventoryPanel();
        }
    }

    public void UpdatePlayerStats()
    {
        UpdateCoins(GameState.Instance.Coins);
        UpdateHappiness(GameState.Instance.Happiness);
        UpdateSaving(GameState.Instance.Saving);
    }

    public void UpdateSaving(int saving)
    {
        savingCounter.text = saving.ToString();
    }

    public void UpdateSavingText(int saving)
    {
        savingText.text = saving.ToString();
    }

    public void UpdateMenabungTitle(string title)
    {
        Label titleLabel = rootElement?.Q<Label>("MenabungTitleText");
        if (titleLabel != null)
        {
            titleLabel.text = title;
        }
    }

    public void UpdateJumatBerkahText(int amount)
    {
        jumatBerkahText.text = amount.ToString();
    }

    public void UpdateKebutuhanText(int amount)
    {
        hargaBeliText.text = amount.ToString();
        SetKebutuhanPriceLabel("Harga Beli");
    }

    public void UpdateKebutuhanVariantText(string kebutuhanName, int harga, int poin)
    {
        hargaBeliText.text = harga.ToString();
        SetKebutuhanPriceLabel("Harga Beli " + kebutuhanName + " (+" + poin + " kebahagiaan)");
    }

    private void SetKebutuhanPriceLabel(string text)
    {
        Label priceLabel = rootElement?.Q<Label>("LabelKText");
        if (priceLabel != null)
        {
            priceLabel.text = text;
        }
    }

    public void UpdateHargaEmasText(int amount)
    {
        hargaEmasText.text = amount.ToString();
    }

    public void UpdateJumlahEmasText(int amount)
    {
        jumlahEmasText.text = amount.ToString();
    }

    public void UpdateFinalHappinessInput(int amount)
    {
        if (finalHappinessInputText != null)
        {
            finalHappinessInputText.text = amount.ToString();
        }
    }

    public void UpdateFinalHappinessPlayer(int player)
    {
        if (finalHappinessPlayerText != null)
        {
            string playerName = PlayerPrefs.GetString("PlayerName_" + player, "Player " + player);
            finalHappinessPlayerText.text = "Player: " + playerName;
        }
    }
}
