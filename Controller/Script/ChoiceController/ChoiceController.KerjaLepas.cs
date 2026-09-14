using UnityEngine;

public partial class ChoiceController
{
    // Handles Kerja Lepas reward.
    private void HandleChoiceKL()
    {
        int income = GameState.Instance != null ? GameState.Instance.FreelanceIncome : 1;
        GameState.Instance.ChangeCoins(income);
        view.UpdateCoins(GameState.Instance.Coins);
        PostKerjaLepasEvent(GameState.Instance.turn, income);

        string resultText = "Bekerja lepas mendapatkan " + income + " koin\n";
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
