using UnityEngine;

public partial class ChoiceController
{
    private bool isPendingTujuanFinansialConfirmation;

    // Handles financial goals and saving controls.
    private void HandleChoiceTF(string selectedChoice)
    {
        Debug.Log($"{selectedChoice} dipilih");
        if (DataManager.Instance == null || DataManager.Instance.tujuanFinansialDict == null
            || !DataManager.Instance.tujuanFinansialDict.ContainsKey(selectedChoice))
        {
            return;
        }

        var amount = 0 - DataManager.Instance.tujuanFinansialDict[selectedChoice].hargaBeli;

        if (GameState.Instance.Saving + amount < 0)
        {
            Debug.Log("Tabungan tidak cukup untuk membeli " + selectedChoice);
            view.AddTextToDialog("Tabungan tidak cukup untuk membeli " + selectedChoice + "\n");
            CompleteTujuanFinansialFlow();
            return;
        }

        GameState.Instance.ChangeSaving(amount);
        GameState.Instance.AddTujuanFinansialToList(selectedChoice);
        view.UpdateSaving(GameState.Instance.Saving);

        var amountHappiness = DataManager.Instance.tujuanFinansialDict[selectedChoice].poinKebahagiaan;
        GameState.Instance.ChangeHappiness(amountHappiness);
        view.UpdateHappiness(GameState.Instance.Happiness);

        string resultText = "Membeli tujuan finansial " + selectedChoice + " seharga " + (-amount) + " koin dengan poin kebahagiaan " + amountHappiness + "\n";
        int aksiKe = GameState.Instance.tfAksiKe;
        GameState.Instance.tfAksiKe++;
        Debug.Log("tfAksiKe: " + GameState.Instance.tfAksiKe);

        if (Narasi("TujuanFinansial", aksiKe, () =>
        {
            ShowSystemDialogThen(resultText, CompleteTujuanFinansialFlow);
        }))
        {
            return;
        }

        ShowSystemDialogThen(resultText, CompleteTujuanFinansialFlow);
    }

    private void HandleChoiceMenabung(string selectedChoice)
    {
        Debug.Log($"{selectedChoice} dipilih");

        switch (selectedChoice)
        {
            case "MaxButton":
                GameState.Instance.SetSavingText(15);
                break;
            case "MinButton":
                GameState.Instance.SetSavingText(0);
                break;
            case "IncreaseButton":
                if (GameState.Instance.SavingText < 15)
                {
                    GameState.Instance.ChangeSavingText(1);
                }
                break;
            case "DecreaseButton":
                if (GameState.Instance.SavingText > 0)
                {
                    GameState.Instance.ChangeSavingText(-1);
                }
                break;
            case "ConfirmButton":
                if (GameState.Instance.SavingText <= 0 || GameState.Instance.SavingText > GameState.Instance.Coins)
                {
                    Debug.Log("Jumlah tabungan harus lebih dari 0");
                    view.AddTextToDialog("Jumlah tabungan harus lebih dari 0\n");
                    view.ShowChoice("Choice1");
                    return;
                }
                GameState.Instance.ChangeSaving(GameState.Instance.SavingText);
                GameState.Instance.ChangeCoins(-GameState.Instance.SavingText);
                view.UpdateCoins(GameState.Instance.Coins);
                view.UpdateSaving(GameState.Instance.Saving);
                
                string savingText = "Menabung " + GameState.Instance.SavingText + " koin\n";
                ShowSystemDialogThen(savingText, ContinueAfterMenabung);
                break;
            default:
                Debug.Log("Pilihan tidak valid");
                break;
        }

        Debug.Log("SavingText: " + GameState.Instance.SavingText.ToString());
        view.UpdateSavingText(GameState.Instance.SavingText);
    }

    private void HandleChoiceTFConfirm(string selectedChoice)
    {
        if (!isPendingTujuanFinansialConfirmation)
        {
            view.ShowChoice("Choice1");
            return;
        }

        if (selectedChoice == "TFConfirmYesButton")
        {
            view.ShowTujuanFinansialPurchasableOnly();
            return;
        }

        if (selectedChoice == "TFConfirmNoButton")
        {
            CompleteTujuanFinansialFlow();
        }
    }

    private void ContinueAfterMenabung()
    {
        GameState.Instance.ConsumeMoveWithoutTurnProgress();
        view.UpdateDay(GameState.Instance.day);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();

        if (!HasAffordableTujuanFinansial())
        {
            CompleteTujuanFinansialFlow();
            return;
        }

        isPendingTujuanFinansialConfirmation = true;
        view.ShowTujuanFinansialConfirmation();
    }

    private bool HasAffordableTujuanFinansial()
    {
        if (DataManager.Instance == null || DataManager.Instance.tujuanFinansialDict == null)
        {
            return false;
        }

        foreach (var tujuan in DataManager.Instance.tujuanFinansialDict.Values)
        {
            if (GameState.Instance.Saving >= tujuan.hargaBeli)
            {
                return true;
            }
        }

        return false;
    }

    private void CompleteTujuanFinansialFlow()
    {
        isPendingTujuanFinansialConfirmation = false;
        GameState.Instance.AdvanceTurnIfMovesDepleted();

        view.UpdateDay(GameState.Instance.day);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();

        ShowNextScheduledChoice();
    }

    public void CancelPendingTujuanFinansialConfirmation()
    {
        if (!isPendingTujuanFinansialConfirmation)
        {
            view.ShowChoice("Choice1");
            return;
        }

        CompleteTujuanFinansialFlow();
    }
}
