using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManagerPlay
{
    // Choice click handling and choice container switching.
    private void OnChoiceClicked(ClickEvent evt, VisualElement choiceContainer)
    {
        Button clickedButton = evt.currentTarget as Button;
        if (clickedButton == null)
        {
            return;
        }
        
        string choiceText = string.IsNullOrEmpty(clickedButton.text) ? clickedButton.name : clickedButton.text;
        Debug.Log("Choice clicked: " + choiceText + " (Button name: " + clickedButton.name + ")");
        selectedChoice = clickedButton.name;
        
        transitionCallback = e => OnChoiceHidden(e, choiceContainer);
        choiceContainer.RegisterCallback(transitionCallback);
        SetButtonsEnabled(choiceContainer, false);
        choiceContainer.RemoveFromClassList("show-choice");
    }

    private void OnChoiceHidden(TransitionEndEvent evt, VisualElement choiceContainer)
    {
        // choiceContainer.style.display = DisplayStyle.None;
        choiceContainer.UnregisterCallback(transitionCallback);
        SetButtonsEnabled(choiceContainer, true);

        choiceController.HandleChoice(choiceContainer.name, selectedChoice);
    }

    private void OnChoiceTFClicked(ClickEvent evt, VisualElement choiceContainer)
    {
        Button clickedButton = evt.currentTarget as Button;
        if (clickedButton == null)
        {
            return;
        }
        
        string choiceText = string.IsNullOrEmpty(clickedButton.text) ? clickedButton.name : clickedButton.text;
        Debug.Log("Choice clicked: " + choiceText + " (Button name: " + clickedButton.name + ")");
        selectedChoice = clickedButton.name;
        choiceController.HandleChoice(choiceContainer.name, selectedChoice);
    }

    private void SetButtonsEnabled(VisualElement container, bool enabled)
    {
        foreach (var btn in container.Query<Button>().ToList())
        {
            btn.SetEnabled(enabled);
        }
    }

    public void HideChoiceContainer(string containerName)
    {
        if (choiceContainers == null || !choiceContainers.ContainsKey(containerName))
        {
            return;
        }

        choiceContainers[containerName].RemoveFromClassList("show-choice");
    }

    public void ShowChoice(string id)
    {
        SetChoiceBackground(id);

        switch (id)
        {
            case "Choice1":
                if (GameState.Instance.IsGameOver())
                {
                    HideDialogContainer();
                    choiceController.StartFinalHappinessInput();
                    return;
                }
                HideDialogContainer();
                UpdateMainChoiceButtonStates();
                choiceContainers["Choice1"].AddToClassList("show-choice");
                break;
            case "BahanMasakan":
                if (GameState.Instance.IsBahanTotalAtLimit(GameState.Instance.turn))
                {
                    UpdateMainChoiceButtonStates();
                    choiceContainers["Choice1"].AddToClassList("show-choice");
                    break;
                }
                bahanPage = 0;
                UpdateBahanPage();
                choiceContainers["ChoiceBM"].AddToClassList("show-choice");
                break;
            case "Kebutuhan":
                kebutuhanPage = 0;
                UpdateKebutuhanPage();
                choiceContainers["ChoiceK"].AddToClassList("show-choice");
                break;
            case "JualMasakan":
                jualMasakanPage = 0;
                UpdateJualMasakanPage();
                choiceContainers["ChoiceJM"].AddToClassList("show-choice");
                break;
            case "TujuanFinansial":
                tujuanFinansialPage = 0;
                UpdateTujuanFinansialPage();
                choiceContainers["ChoiceTF"].AddToClassList("show-choice");
                break;
            case "ChoiceTFConfirm":
                choiceContainers["ChoiceTFConfirm"].AddToClassList("show-choice");
                break;
            case "KerjaLepas":
                choiceContainers["ChoiceKL"].AddToClassList("show-choice");
                break;
            case "PinjamanSyariah":
                choiceContainers["ChoicePS"].AddToClassList("show-choice");
                break;
            case "HargaEmas":
                choiceContainers["ChoiceHargaEmas"].AddToClassList("show-choice");
                break;
            case "EmasAction":
                if (emasActionDecisionText != null)
                {
                    string playerName = PlayerPrefs.GetString("PlayerName_" + GameState.Instance.turn, "Player " + GameState.Instance.turn);
                    emasActionDecisionText.text = "Keputusan " + playerName;
                }
                choiceContainers["ChoiceEmasAction"].AddToClassList("show-choice");
                break;
            case "JumlahEmas":
                choiceContainers["ChoiceJumlahEmas"].AddToClassList("show-choice");
                break;
            case "RisikoKehidupan":
                HideDialogContainer();
                choiceContainers["ChoiceRisikoKehidupan"].AddToClassList("show-choice");
                break;
            case "Menabung":
                showOnlyAffordableTujuanFinansial = false;
                choiceContainers["ChoiceMenabung"].AddToClassList("show-choice");
                break;
            case "JumatBerkah":
                choiceContainers["JumatBerkah"].AddToClassList("show-choice");
                GameState.Instance.SetSavingText(GameState.Instance.Coins > 0 ? 1 : 0);
                UpdateJumatBerkahText(GameState.Instance.SavingText);
                break;
            case "ChoiceKJumlah":
                choiceContainers["ChoiceKJumlah"].AddToClassList("show-choice");
                break;
            case "FinalHappiness":
                choiceContainers["ChoiceFinalHappiness"].AddToClassList("show-choice");
                break;
            default:
                break;
        }
    }

    public void ShowGameOverSummary()
    {
        HideDialogContainer();

        string summary = "Permainan selesai!";
        for (int player = 1; player <= GameState.Instance.playerCount; player++)
        {
            string playerName = PlayerPrefs.GetString("PlayerName_" + player, "Player " + player);
            summary += " | " + playerName + ": " + GameState.Instance.GetHappiness(player);
        }

        AddSystemTextToDialog(summary);
        choiceContainers["PermainanSelesai"].AddToClassList("show-choice");
    }

    private void UpdateAsuransiButtonState()
    {
        if (asuransiButton == null || GameState.Instance == null)
        {
            return;
        }

        bool canBuyAsuransi = !GameState.Instance.AsuransiDimiliki;
        asuransiButton.SetEnabled(canBuyAsuransi);
    }

    private void UpdateBahanMasakanButtonState()
    {
        if (bahanMasakanButton == null || GameState.Instance == null)
        {
            return;
        }

        bool canBuyBahan = !GameState.Instance.IsBahanTotalAtLimit(GameState.Instance.turn);
        bahanMasakanButton.SetEnabled(canBuyBahan);
    }

    private void UpdateMainChoiceButtonStates()
    {
        UpdateAsuransiButtonState();
        UpdateBahanMasakanButtonState();
    }

    public void ShowTujuanFinansialConfirmation()
    {
        HideDialogContainer();
        showOnlyAffordableTujuanFinansial = true;
        string playerName = PlayerPrefs.GetString("PlayerName_" + GameState.Instance.turn, "Player " + GameState.Instance.turn);
        if (tfConfirmText != null)
        {
            tfConfirmText.text = "Apakah " + playerName + " ingin membeli Tujuan Finansial?";
        }

        choiceContainers["ChoiceMenabung"].RemoveFromClassList("show-choice");
        ShowChoice("ChoiceTFConfirm");
    }

    public void ShowTujuanFinansialPurchasableOnly()
    {
        showOnlyAffordableTujuanFinansial = true;
        choiceContainers["ChoiceTFConfirm"].RemoveFromClassList("show-choice");
        ShowChoice("TujuanFinansial");
    }
}
