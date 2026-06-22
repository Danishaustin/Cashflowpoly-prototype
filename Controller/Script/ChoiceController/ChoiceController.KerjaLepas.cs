using UnityEngine;

public partial class ChoiceController
{
    // Handles Kerja Lepas reward.
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
            ShowSystemDialogThen(resultText, UpdateMove);
        }))
        {
            return;
        }

        ShowSystemDialogThen(resultText, UpdateMove);
    }
}
