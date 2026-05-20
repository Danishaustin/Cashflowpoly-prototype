using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class ChoiceController : MonoBehaviour
{
    public UIManagerPlay view;

    public void HandleChoice(string currentContainer, string selectedChoice)
    {
        switch (currentContainer)
        {
            case "Choice1":
                HandleChoice1(selectedChoice);
                break;
            case "ChoiceBM":
                HandleChoiceBahan(selectedChoice);
                break;
            case "ChoiceJM":
                HandleChoiceJM(selectedChoice);
                break;
            case "ChoiceK":
                HandleChoiceK(selectedChoice);
                break;
            case "ChoiceKL":
                HandleChoiceKL();
                break;
            case "ChoiceTF":
                HandleChoiceTF(selectedChoice);
                break;
            case "ChoiceMenabung":
                HandleChoiceMenabung(selectedChoice);
                break;
            case "JumatBerkah":
                JumatBerkah(selectedChoice);
                break;
            case "ChoiceKJumlah":
                HandleChoiceKJumlah(selectedChoice);
                break;
            default:
                Debug.Log("Pilihan tidak valid");
                break;
        }
    }
    private void HandleChoice1(string selectedChoice)
    {
        switch (selectedChoice)
        {
            case "BahanMasakan":
                view.ShowChoice("BahanMasakan");
                break;
            case "Kebutuhan":
                view.ShowChoice("Kebutuhan");
                break;
            case "JualMasakan":
                view.ShowChoice("JualMasakan");
                break;
            case "TujuanFinansial":
                GameState.Instance.SetSavingText(0);
                view.UpdateSavingText(GameState.Instance.SavingText);
                view.ShowChoice("Menabung");
                break;
            case "KerjaLepas":
                HandleChoiceKL();
                break;
            default:
                Debug.Log("Pilihan tidak valid");
                break;
        }
    }

    private void HandleChoiceBahan(string selectedChoice)
    {
        Debug.Log($"{selectedChoice} dipilih");
        var amount = 0 - DataManager.Instance.bahanDict[selectedChoice].hargaBeli;

        if (GameState.Instance.Coins + amount < 0)
        {
            Debug.Log("Uang tidak cukup untuk membeli " + selectedChoice);
            view.AddTextToDialog("Uang tidak cukup untuk membeli " + selectedChoice + "\n");
            view.ShowChoice("Choice1");
            return;
        }

        GameState.Instance.AddBahanToList(selectedChoice);
        GameState.Instance.ChangeCoins(amount);
        view.UpdateCoins(GameState.Instance.Coins);

        string resultText = "Membeli " + selectedChoice + " seharga " + (-amount) + " koin\n";
        int aksiKe = GameState.Instance.bmAksiKe;
        GameState.Instance.bmAksiKe++;
        Debug.Log("bmAksiKe: " + GameState.Instance.bmAksiKe);

        if (Narasi("BahanMasakan", aksiKe, () =>
        {
            view.AddTextToDialog(resultText);
            UpdateMove();
        }))
        {
            return;
        }

        view.AddTextToDialog(resultText);
        UpdateMove();
    }

    private void HandleChoiceJM(string selectedChoice)
    {
        Debug.Log($"{selectedChoice} dijual");
        var listBahan = DataManager.Instance.resepDict[selectedChoice].bahan;
        var jumlahBahan = listBahan
            .GroupBy(b => b)
            .ToDictionary(g => g.Key, g => g.Count());

        var kurangBahan = GameState.Instance.HasBahan(jumlahBahan);

        if (kurangBahan.Count > 0)
        {
            Debug.Log("Bahan tidak cukup: " + string.Join(", ", kurangBahan));
            view.AddTextToDialog("Bahan tidak cukup: " + string.Join(", ", kurangBahan) + "\n");
            view.ShowChoice("Choice1");
            return;
        }

        foreach (var kv in jumlahBahan)
        {
            GameState.Instance.RemoveBahanFromList(kv.Key, kv.Value);
        }

        var amountCoins = DataManager.Instance.resepDict[selectedChoice].hargaJual;
        GameState.Instance.ChangeCoins(amountCoins);
        var amountHappiness = DataManager.Instance.resepDict[selectedChoice].poinKebahagiaan;
        GameState.Instance.ChangeHappiness(amountHappiness);
        view.UpdateCoins(GameState.Instance.Coins);
        view.UpdateHappiness(GameState.Instance.Happiness);

        string resultText = "Menjual " + selectedChoice + " seharga " + amountCoins + " koin dengan poin kebahagiaan " + amountHappiness + "\n";
        int aksiKe = GameState.Instance.jmAksiKe;
        GameState.Instance.jmAksiKe++;
        Debug.Log("jmAksiKe: " + GameState.Instance.jmAksiKe);

        if (Narasi("JualMasakan", aksiKe, () =>
        {
            view.AddTextToDialog(resultText);
            UpdateMove();
        }))
        {
            return;
        }

        view.AddTextToDialog(resultText);
        UpdateMove();
    }

    private void HandleChoiceK(string selectedChoice)
    {
        Debug.Log($"{selectedChoice} dipilih");
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
                if(GameState.Instance.SavingText < GameState.Instance.Coins)
                {
                    GameState.Instance.ChangeSavingText(1);
                }
                break;
            case "DecreaseButtonK":
                if(GameState.Instance.SavingText > 0)
                {
                    GameState.Instance.ChangeSavingText(-1);
                }
                break;
            case "ConfirmButtonK":
                if(GameState.Instance.SavingText <= 0 || GameState.Instance.SavingText > GameState.Instance.Coins)
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
                    view.AddTextToDialog(resultText);
                    UpdateMove();
                }))
                {
                    return;
                }

                view.AddTextToDialog(resultText);
                UpdateMove();
                break;
            default:
                Debug.Log("Pilihan tidak valid");
                view.AddTextToDialog("Pilihan tidak valid\n");
                break;
        }

        Debug.Log("SavingText: " + GameState.Instance.SavingText.ToString());
        view.UpdateKebutuhanText(GameState.Instance.SavingText);
    }

    private void HandleChoiceKL()
    {
        GameState.Instance.ChangeCoins(1);
        view.UpdateCoins(GameState.Instance.Coins);

        string resultText = "Bekerja lepas mendapatkan 1 koin\n";
        int aksiKe = GameState.Instance.klAksiKe;
        GameState.Instance.klAksiKe++;
        Debug.Log("klAksiKe: " + GameState.Instance.klAksiKe);

        if (Narasi("KerjaLepas", aksiKe, () =>
        {
            view.AddTextToDialog(resultText);
            UpdateMove();
        }))
        {
            return;
        }

        view.AddTextToDialog(resultText);
        UpdateMove();
    }

    private void HandleChoiceTF(string selectedChoice)
    {
        Debug.Log($"{selectedChoice} dipilih");
        var amount = 0 - DataManager.Instance.tujuanFinansialDict[selectedChoice].hargaBeli;

        if (GameState.Instance.Saving + amount < 0)
        {
            Debug.Log("Tabungan tidak cukup untuk membeli " + selectedChoice);
            view.AddTextToDialog("Tabungan tidak cukup untuk membeli " + selectedChoice + "\n");
            view.ShowChoice("Choice1");
            return;
        }

        GameState.Instance.ChangeSaving(amount);
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
            view.AddTextToDialog(resultText);
            UpdateMove();
        }))
        {
            return;
        }

        view.AddTextToDialog(resultText);
        UpdateMove();
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
                if(GameState.Instance.SavingText < 15)
                {
                    GameState.Instance.ChangeSavingText(1);
                }
                break;
            case "DecreaseButton":
                if(GameState.Instance.SavingText > 0)
                {
                    GameState.Instance.ChangeSavingText(-1);
                }
                break;
            case "ConfirmButton":
                if(GameState.Instance.SavingText <= 0 || GameState.Instance.SavingText > GameState.Instance.Coins)
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
                view.AddTextToDialog("Menabung " + GameState.Instance.SavingText + " koin\n");
                UpdateMove();
                break;
            case "TFButton":
                view.ShowChoice("TujuanFinansial");
                break;
            default:
                Debug.Log("Pilihan tidak valid");
                break;
        }

        Debug.Log("SavingText: " + GameState.Instance.SavingText.ToString());
        view.UpdateSavingText(GameState.Instance.SavingText);
    }

    private void JumatBerkah(string selectedChoice)
    {
        Debug.Log($"{selectedChoice} dipilih");

        switch (selectedChoice)
        {
            case "MaxButtonJB":
                GameState.Instance.SetSavingText(GameState.Instance.Coins);
                break;
            case "MinButtonJB":
                GameState.Instance.SetSavingText(0);
                break;
            case "IncreaseButtonJB":
                if(GameState.Instance.SavingText < GameState.Instance.Coins)
                {
                    GameState.Instance.ChangeSavingText(1);
                }
                Debug.Log("IncreaseButtonJB clicked, SavingText: " + GameState.Instance.SavingText.ToString() + ", Coins: " + GameState.Instance.Coins.ToString());
                break;
            case "DecreaseButtonJB":
                if(GameState.Instance.SavingText > 0)
                {
                    GameState.Instance.ChangeSavingText(-1);
                }
                break;
            case "ConfirmButtonJB":
                if(GameState.Instance.SavingText <= 0 || GameState.Instance.SavingText > GameState.Instance.Coins)
                {
                    Debug.Log("Jumlah tidak valid");
                    view.AddTextToDialog("Jumlah tidak valid\n");
                    view.ShowChoice("JumatBerkah");
                    return;
                }
                GameState.Instance.ChangeCoins(-GameState.Instance.SavingText);
                view.UpdateCoins(GameState.Instance.Coins);
                GameState.Instance.NextDay();
                view.UpdateDay(GameState.Instance.day);
                view.UpdatePlayerTurn(GameState.Instance.turn);
                view.UpdatePlayerStats();
                view.ShowChoice("Choice1");
                view.AddTextToDialog("Mengeluarkan uang saat Peduli Donasi dengan jumlah " + GameState.Instance.SavingText + " koin\n", 0.5f, true);
                break;
            default:
                Debug.Log("Pilihan tidak valid");
                view.AddTextToDialog("Pilihan tidak valid\n");
                break;
        }

        Debug.Log("SavingText: " + GameState.Instance.SavingText.ToString());
        view.UpdateJumatBerkahText(GameState.Instance.SavingText);
    }

    private bool Narasi(string aksi, int aksiKe, System.Action onComplete = null)
    {
        Debug.Log("Menampilkan narasi untuk aksi " + aksi);

        return NarasiController.Instance.HandleNarasi(aksi, aksiKe, onComplete);
    }

    private void UpdateMove()
    {
        GameState.Instance.UseMove(); 
        
        view.UpdateDay(GameState.Instance.day);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();

        if (GameState.Instance.IsJumatBerkah())
        {
            view.ShowChoice("JumatBerkah");
        }
        else
        {
            view.ShowChoice("Choice1");
        }
    }
}
