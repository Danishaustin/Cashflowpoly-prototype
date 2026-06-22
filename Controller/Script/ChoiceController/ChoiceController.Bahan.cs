using UnityEngine;

public partial class ChoiceController
{
    // Handles buying cooking ingredients from ChoiceBM.
    private void HandleChoiceBahan(string selectedChoice)
    {
        Debug.Log($"{selectedChoice} dipilih");
        int activePlayer = GameState.Instance.turn;

        if (GameState.Instance.IsBahanTotalAtLimit(activePlayer))
        {
            view.AddTextToDialog("Bahan masakan sudah maksimal 6 item.\n");
            view.ShowChoice("Choice1");
            return;
        }

        if (GameState.Instance.IsBahanAtLimit(activePlayer, selectedChoice))
        {
            view.AddTextToDialog(selectedChoice + " sudah maksimal 3 item.\n");
            view.ShowChoice("BahanMasakan");
            return;
        }

        var amount = 0 - DataManager.Instance.bahanDict[selectedChoice].hargaBeli;

        if (GameState.Instance.Coins + amount < 0)
        {
            Debug.Log("Uang tidak cukup untuk membeli " + selectedChoice);
            view.AddTextToDialog("Uang tidak cukup untuk membeli " + selectedChoice + "\n");
            view.ShowChoice("Choice1");
            return;
        }

        GameState.Instance.AddBahanToList(activePlayer, selectedChoice);
        GameState.Instance.ChangeCoins(amount);
        view.UpdateCoins(GameState.Instance.Coins);

        string resultText = "Membeli " + selectedChoice + " seharga " + (-amount) + " koin\n";
        int aksiKe = GameState.Instance.bmAksiKe;
        GameState.Instance.bmAksiKe++;
        Debug.Log("bmAksiKe: " + GameState.Instance.bmAksiKe);

        if (Narasi("BahanMasakan", aksiKe, () =>
        {
            ShowSystemDialogThen(resultText, UpdateMove);
        }))
        {
            return;
        }

        ShowSystemDialogThen(resultText, UpdateMove);
    }
}
