using System;
using UnityEngine;

public partial class ChoiceController
{
    // Handles selecting and paying for kebutuhan.
    private void HandleChoiceK(string selectedChoice)
    {
        Debug.Log($"{selectedChoice} dipilih");
        if (DataManager.Instance != null && DataManager.Instance.kebutuhanDict != null
            && DataManager.Instance.kebutuhanDict.TryGetValue(selectedChoice, out KebutuhanData kebutuhanData))
        {
            bool isPrimer = string.Equals(kebutuhanData.tipe, "primer", System.StringComparison.OrdinalIgnoreCase);
            bool hasPrimer = GameState.Instance.HasKebutuhanPrimer(GameState.Instance.turn);
            if (!isPrimer && !hasPrimer)
            {
                view.AddTextToDialog("Harus membeli kebutuhan primer dulu.\n");
                view.ShowChoice("Kebutuhan");
                return;
            }
        }

        GameState.Instance.SetKebutuhanSelected(selectedChoice);
        view.ShowChoice("ChoiceKJumlah");
        GameState.Instance.SetSavingText(0);
        view.UpdateKebutuhanText(GameState.Instance.SavingText);
    }

    private void HandleChoiceKJumlah(string selectedChoice)
    {
        Debug.Log($"{selectedChoice} dipilih");

        switch (selectedChoice)
        {
            case "MaxButtonK":
                GameState.Instance.SetSavingText(GameState.Instance.Coins);
                break;
            case "MinButtonK":
                GameState.Instance.SetSavingText(0);
                break;
            case "IncreaseButtonK":
                if (GameState.Instance.SavingText < GameState.Instance.Coins)
                {
                    GameState.Instance.ChangeSavingText(1);
                }
                break;
            case "DecreaseButtonK":
                if (GameState.Instance.SavingText > 0)
                {
                    GameState.Instance.ChangeSavingText(-1);
                }
                break;
            case "ConfirmButtonK":
                if (GameState.Instance.SavingText <= 0 || GameState.Instance.SavingText > GameState.Instance.Coins)
                {
                    Debug.Log("Jumlah tidak valid");
                    view.AddTextToDialog("Jumlah tidak valid\n");
                    view.ShowChoice("Choice1");
                    return;
                }

                var tipeKebutuhan = DataManager.Instance.kebutuhanDict[GameState.Instance.kebutuhanSelected].tipe;
                GameState.Instance.AddKebutuhanToList(GameState.Instance.kebutuhanSelected, tipeKebutuhan);
                GameState.Instance.ChangeCoins(-GameState.Instance.SavingText);
                GameState.Instance.ChangeHappiness(GameState.Instance.SavingText - 1);
                view.UpdateCoins(GameState.Instance.Coins);
                view.UpdateHappiness(GameState.Instance.Happiness);
                Debug.Log("Kebutuhan " + tipeKebutuhan + " yang dimiliki: " + string.Join(", ", GameState.Instance.kebutuhanList[tipeKebutuhan]));

                string resultText = "Membeli kebutuhan " + GameState.Instance.kebutuhanSelected + " seharga " + GameState.Instance.SavingText + " koin dengan poin kebahagiaan " + (GameState.Instance.SavingText - 1) + "\n";
                int aksiKe = GameState.Instance.kAksiKe;
                GameState.Instance.kAksiKe++;
                Debug.Log("kAksiKe: " + GameState.Instance.kAksiKe);

                if (Narasi("Kebutuhan", aksiKe, () =>
                {
                    ShowSystemDialogThen(resultText, UpdateMove);
                }))
                {
                    return;
                }

                ShowSystemDialogThen(resultText, UpdateMove);
                break;
            default:
                Debug.Log("Pilihan tidak valid");
                view.AddTextToDialog("Pilihan tidak valid\n");
                break;
        }

        Debug.Log("SavingText: " + GameState.Instance.SavingText.ToString());
        view.UpdateKebutuhanText(GameState.Instance.SavingText);
    }
}
