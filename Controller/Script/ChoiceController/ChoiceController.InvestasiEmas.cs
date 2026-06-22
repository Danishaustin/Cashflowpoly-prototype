using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class ChoiceController
{
    private const int HargaEmasMaxInput = 100;
    private const int JumlahEmasMaxInput = 99;

    private string investasiEmasMode = "";
    private bool investasiEmasDariRisiko;
    private int investasiEmasRisikoOriginalTurn;
    private int investasiEmasRisikoOriginalMovesLeft;

    // Handles Saturday gold investment: set price, then each player buys or sells.
    private void ShowInvestasiEmasHargaInput()
    {
        investasiEmasDariRisiko = false;
        GameState.Instance.SetHargaEmasText(Mathf.Max(1, GameState.Instance.HargaEmasSaatIni));
        view.UpdateHargaEmasText(GameState.Instance.HargaEmasText);
        view.ShowChoice("HargaEmas");
    }

    private void ShowInvestasiEmasHargaInputFromRisiko()
    {
        investasiEmasDariRisiko = true;
        investasiEmasRisikoOriginalTurn = GameState.Instance.turn;
        investasiEmasRisikoOriginalMovesLeft = GameState.Instance.movesLeft;
        GameState.Instance.SetTurnAndMoves(1, 2);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();
        GameState.Instance.SetHargaEmasText(Mathf.Max(1, GameState.Instance.HargaEmasSaatIni));
        view.UpdateHargaEmasText(GameState.Instance.HargaEmasText);
        view.ShowChoice("HargaEmas");
    }

    private void ShowInvestasiEmasActionChoice()
    {
        view.ShowChoice("EmasAction");
    }

    private void HandleChoiceHargaEmas(string selectedChoice)
    {
        switch (selectedChoice)
        {
            case "MinButtonHargaEmas":
                GameState.Instance.SetHargaEmasText(1);
                break;
            case "MaxButtonHargaEmas":
                GameState.Instance.SetHargaEmasText(HargaEmasMaxInput);
                break;
            case "IncreaseButtonHargaEmas":
                if (GameState.Instance.HargaEmasText < HargaEmasMaxInput)
                {
                    GameState.Instance.ChangeHargaEmasText(1);
                }
                break;
            case "DecreaseButtonHargaEmas":
                if (GameState.Instance.HargaEmasText > 1)
                {
                    GameState.Instance.ChangeHargaEmasText(-1);
                }
                break;
            case "ConfirmButtonHargaEmas":
                if (GameState.Instance.HargaEmasText <= 0)
                {
                    ShowInvestasiEmasDialogThen("Harga emas harus lebih dari 0.\n", () => view.ShowChoice("HargaEmas"));
                    return;
                }

                GameState.Instance.SetHargaEmasSaatIni(GameState.Instance.HargaEmasText);
                ShowInvestasiEmasDialogThen(
                    "Harga emas hari ini adalah " + GameState.Instance.HargaEmasSaatIni + " koin.\n",
                    ShowInvestasiEmasActionChoice);
                return;
            default:
                Debug.Log("Pilihan harga emas tidak valid");
                break;
        }

        view.UpdateHargaEmasText(GameState.Instance.HargaEmasText);
    }

    private void HandleChoiceEmasAction(string selectedChoice)
    {
        switch (selectedChoice)
        {
            case "BeliEmasInvestasi":
                investasiEmasMode = "Beli";
                GameState.Instance.SetJumlahEmasText(0);
                view.UpdateJumlahEmasText(GameState.Instance.JumlahEmasText);
                view.ShowChoice("JumlahEmas");
                break;
            case "JualEmasInvestasi":
                investasiEmasMode = "Jual";
                GameState.Instance.SetJumlahEmasText(0);
                view.UpdateJumlahEmasText(GameState.Instance.JumlahEmasText);
                view.ShowChoice("JumlahEmas");
                break;
            default:
                Debug.Log("Pilihan aksi emas tidak valid");
                ShowInvestasiEmasActionChoice();
                break;
        }
    }

    private void HandleChoiceJumlahEmas(string selectedChoice)
    {
        switch (selectedChoice)
        {
            case "MinButtonJumlahEmas":
                GameState.Instance.SetJumlahEmasText(0);
                break;
            case "MaxButtonJumlahEmas":
                GameState.Instance.SetJumlahEmasText(JumlahEmasMaxInput);
                break;
            case "IncreaseButtonJumlahEmas":
                if (GameState.Instance.JumlahEmasText < JumlahEmasMaxInput)
                {
                    GameState.Instance.ChangeJumlahEmasText(1);
                }
                break;
            case "DecreaseButtonJumlahEmas":
                if (GameState.Instance.JumlahEmasText > 0)
                {
                    GameState.Instance.ChangeJumlahEmasText(-1);
                }
                break;
            case "ConfirmButtonJumlahEmas":
                ConfirmInvestasiEmasAmount();
                return;
            default:
                Debug.Log("Pilihan jumlah emas tidak valid");
                break;
        }

        view.UpdateJumlahEmasText(GameState.Instance.JumlahEmasText);
    }

    private void ConfirmInvestasiEmasAmount()
    {
        if (GameState.Instance.JumlahEmasText <= 0)
        {
            ShowInvestasiEmasDialogThen("Jumlah emas harus lebih dari 0.\n", ShowInvestasiEmasActionChoice);
            return;
        }

        if (investasiEmasMode == "Beli")
        {
            BuyInvestasiEmas();
            return;
        }

        if (investasiEmasMode == "Jual")
        {
            SellInvestasiEmas();
            return;
        }

        ShowInvestasiEmasActionChoice();
    }

    private void BuyInvestasiEmas()
    {
        int amount = GameState.Instance.JumlahEmasText;
        int cost = GameState.Instance.HargaEmasSaatIni * amount;

        if (GameState.Instance.Coins < cost)
        {
            ShowInvestasiEmasDialogThen(
                "Coin tidak cukup untuk membeli " + amount + " emas.\n",
                ShowInvestasiEmasActionChoice);
            return;
        }

        int player = GameState.Instance.turn;
        GameState.Instance.ChangeCoins(-cost);
        GameState.Instance.ChangeEmas(amount);
        view.UpdateCoins(GameState.Instance.Coins);

        ShowInvestasiEmasDialogThen(
            GetPlayerName(player) + " membeli " + amount + " emas seharga "
            + cost + " koin. Emas saat ini: " + GameState.Instance.Emas + "\n",
            AdvanceInvestasiEmas);
    }

    private void SellInvestasiEmas()
    {
        int amount = GameState.Instance.JumlahEmasText;

        if (GameState.Instance.Emas < amount)
        {
            ShowInvestasiEmasDialogThen(
                "Emas tidak cukup untuk menjual " + amount + " emas.\n",
                ShowInvestasiEmasActionChoice);
            return;
        }

        int player = GameState.Instance.turn;
        int income = GameState.Instance.HargaEmasSaatIni * amount;
        GameState.Instance.ChangeCoins(income);
        GameState.Instance.ChangeEmas(-amount);
        view.UpdateCoins(GameState.Instance.Coins);

        ShowInvestasiEmasDialogThen(
            GetPlayerName(player) + " menjual " + amount + " emas dan mendapatkan "
            + income + " koin. Emas tersisa: " + GameState.Instance.Emas + "\n",
            AdvanceInvestasiEmas);
    }

    private void ShowInvestasiEmasDialogThen(string text, Action onComplete)
    {
        StartCoroutine(ShowInvestasiEmasDialogThenRoutine(text, onComplete));
    }

    private IEnumerator ShowInvestasiEmasDialogThenRoutine(string text, Action onComplete)
    {
        yield return view.PlaySystemDialogSteps(new List<string> { text });
        view.HideDialog();
        onComplete?.Invoke();
    }

    private void AdvanceInvestasiEmas()
    {
        if (investasiEmasDariRisiko)
        {
            AdvanceInvestasiEmasFromRisiko();
            return;
        }

        bool isInvestasiEmasSelesai = GameState.Instance.AdvanceInvestasiEmasTurn();
        view.UpdateDay(GameState.Instance.day);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();

        if (isInvestasiEmasSelesai)
        {
            ShowNextScheduledChoice();
            return;
        }

        ShowInvestasiEmasActionChoice();
    }

    private void AdvanceInvestasiEmasFromRisiko()
    {
        if (GameState.Instance.turn < GameState.Instance.playerCount)
        {
            GameState.Instance.SetTurnAndMoves(GameState.Instance.turn + 1, 2);
            view.UpdatePlayerTurn(GameState.Instance.turn);
            view.UpdatePlayerStats();
            ShowInvestasiEmasActionChoice();
            return;
        }

        investasiEmasDariRisiko = false;
        GameState.Instance.SetTurnAndMoves(investasiEmasRisikoOriginalTurn, investasiEmasRisikoOriginalMovesLeft);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();
        UpdateMove();
    }
}
