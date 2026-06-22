using System.Linq;
using UnityEngine;

public partial class ChoiceController
{
    // Handles selling cooked recipes from ChoiceJM.
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
            string errorText = "Bahan tidak cukup: " + string.Join(", ", kurangBahan) + "\n";
            ShowSystemDialogThen(errorText, () => view.ShowChoice("Choice1"));
            return;
        }

        foreach (var kv in jumlahBahan)
        {
            GameState.Instance.RemoveBahanFromList(kv.Key, kv.Value);
        }

        var amountCoins = DataManager.Instance.resepDict[selectedChoice].hargaJual;
        GameState.Instance.ChangeCoins(amountCoins);
        GameState.Instance.AddMasakanDijualToList(selectedChoice);
        var amountHappiness = DataManager.Instance.resepDict[selectedChoice].poinKebahagiaan;
        GameState.Instance.ChangeHappiness(amountHappiness);
        view.UpdateCoins(GameState.Instance.Coins);
        view.UpdateHappiness(GameState.Instance.Happiness);

        string resultText = "Menjual " + selectedChoice + " menghasilkan " + amountCoins + " koin\n";
        int aksiKe = GameState.Instance.jmAksiKe;
        GameState.Instance.jmAksiKe++;
        Debug.Log("jmAksiKe: " + GameState.Instance.jmAksiKe);

        if (Narasi("JualMasakan", aksiKe, () =>
        {
            ShowSystemDialogThen(resultText, ShowRisikoKehidupanAfterJualMasakan);
        }))
        {
            return;
        }

        ShowSystemDialogThen(resultText, ShowRisikoKehidupanAfterJualMasakan);
    }
}
