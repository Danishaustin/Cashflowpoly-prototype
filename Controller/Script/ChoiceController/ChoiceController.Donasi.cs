using System.Collections.Generic;
using UnityEngine;

public partial class ChoiceController
{
    // Handles Peduli Donasi / Jumat Berkah multi-player flow.
    private void JumatBerkah(string selectedChoice)
    {
        Debug.Log($"{selectedChoice} dipilih");

        switch (selectedChoice)
        {
            case "MaxButtonJB":
                GameState.Instance.SetSavingText(GameState.Instance.Coins);
                break;
            case "MinButtonJB":
                GameState.Instance.SetSavingText(GameState.Instance.Coins > 0 ? 1 : 0);
                break;
            case "IncreaseButtonJB":
                if (GameState.Instance.SavingText < GameState.Instance.Coins)
                {
                    GameState.Instance.ChangeSavingText(1);
                }
                Debug.Log("IncreaseButtonJB clicked, SavingText: " + GameState.Instance.SavingText.ToString() + ", Coins: " + GameState.Instance.Coins.ToString());
                break;
            case "DecreaseButtonJB":
                if (GameState.Instance.SavingText > 1)
                {
                    GameState.Instance.ChangeSavingText(-1);
                }
                break;
            case "ConfirmButtonJB":
                if (GameState.Instance.SavingText < 1 || GameState.Instance.SavingText > GameState.Instance.Coins)
                {
                    Debug.Log("Jumlah donasi tidak valid");
                    ShowSystemDialogThen("Minimal donasi adalah 1 coin.", () => view.ShowChoice("JumatBerkah"));
                    return;
                }
                int peduliDonasiPlayer = GameState.Instance.turn;
                int peduliDonasiAmount = GameState.Instance.SavingText;
                string peduliDonasiPlayerName = GetPlayerName(peduliDonasiPlayer);
                GameState.Instance.CatatPeduliDonasi(GameState.Instance.SavingText);
                GameState.Instance.ChangeCoins(-GameState.Instance.SavingText);
                view.UpdateCoins(GameState.Instance.Coins);
                bool isPeduliDonasiSelesai = GameState.Instance.AdvancePeduliDonasiTurn();
                view.UpdateDay(GameState.Instance.day);
                view.UpdatePlayerTurn(GameState.Instance.turn);
                view.UpdatePlayerStats();
                string donasiText = peduliDonasiPlayerName + " berdonasi " + peduliDonasiAmount + " coin.";
                ShowSystemDialogThen(donasiText, () => ContinueAfterPeduliDonasiStep(isPeduliDonasiSelesai));
                break;
            default:
                Debug.Log("Pilihan tidak valid");
                ShowSystemDialogThen("Pilihan tidak valid.", () => view.ShowChoice("JumatBerkah"));
                break;
        }

        Debug.Log("SavingText: " + GameState.Instance.SavingText.ToString());
        view.UpdateJumatBerkahText(GameState.Instance.SavingText);
    }

    private void ShowJumatBerkahOrSkipNoCoins()
    {
        if (GameState.Instance.Coins <= 0)
        {
            ShowSystemDialogThen(GetPlayerName(GameState.Instance.turn) + " tidak memiliki uang yang cukup.", ContinueAfterSkipPeduliDonasiNoCoins);
            return;
        }

        view.ShowChoice("JumatBerkah");
    }

    private void ContinueAfterSkipPeduliDonasiNoCoins()
    {
        GameState.Instance.CatatPeduliDonasi(0);
        bool isPeduliDonasiSelesai = GameState.Instance.AdvancePeduliDonasiTurn();

        view.UpdateDay(GameState.Instance.day);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();

        ContinueAfterPeduliDonasiStep(isPeduliDonasiSelesai);
    }

    private void ContinueAfterPeduliDonasiStep(bool isPeduliDonasiSelesai)
    {
        if (!isPeduliDonasiSelesai)
        {
            ShowJumatBerkahOrSkipNoCoins();
            return;
        }

        string juaraText = BuildJuaraPeduliDonasiText();
        if (string.IsNullOrEmpty(juaraText))
        {
            ShowNextScheduledChoice();
            return;
        }

        ShowSystemDialogThen(juaraText, ShowNextScheduledChoice);
    }

    private string BuildJuaraPeduliDonasiText()
    {
        List<int> ranking = GameState.Instance.GetLatestPeduliDonasiRanking();
        if (ranking == null || ranking.Count == 0)
        {
            return string.Empty;
        }

        int topCount = Mathf.Min(3, ranking.Count);
        var parts = new List<string>();
        for (int i = 0; i < topCount; i++)
        {
            string juaraName = GetPlayerName(ranking[i]);
            parts.Add("Juara " + (i + 1) + ": " + juaraName);
        }

        return string.Join(" | ", parts);
    }

    private string GetPlayerName(int player)
    {
        return PlayerPrefs.GetString("PlayerName_" + player, "Player " + player);
    }
}
