using UnityEngine;

public partial class ChoiceController
{
    private const int HargaAsuransi = 1;

    private void HandleChoiceAsuransi()
    {
        if (GameState.Instance.AsuransiDimiliki)
        {
            view.AddTextToDialog("Kamu sudah memiliki kartu asuransi.\n");
            view.ShowChoice("Choice1");
            return;
        }

        if (GameState.Instance.Coins < HargaAsuransi)
        {
            view.AddTextToDialog("Uang tidak cukup untuk membeli asuransi.\n");
            view.ShowChoice("Choice1");
            return;
        }

        GameState.Instance.ChangeCoins(-HargaAsuransi);
        GameState.Instance.SetAsuransiDimiliki(true);
        view.UpdateCoins(GameState.Instance.Coins);

        string resultText = "Membeli kartu asuransi seharga " + HargaAsuransi + " coin.\n";
        if (Narasi("MembeliAsuransi", 0, () =>
        {
            ShowSystemDialogThen(resultText, UpdateMove);
        }))
        {
            return;
        }

        ShowSystemDialogThen(resultText, UpdateMove);
    }
}
