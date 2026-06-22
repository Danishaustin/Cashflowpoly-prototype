using UnityEngine;

public partial class ChoiceController
{
    private const int PinjamanSyariahAmount = 10;

    // Handles taking and returning Pinjaman Syariah.
    private void HandleChoicePinjamanSyariah(string selectedChoice)
    {
        switch (selectedChoice)
        {
            case "AmbilPinjamanSyariah":
                AmbilPinjamanSyariah();
                break;
            case "KembalikanPinjamanSyariah":
                KembalikanPinjamanSyariah();
                break;
            default:
                Debug.Log("Pilihan pinjaman syariah tidak valid");
                view.ShowChoice("Choice1");
                break;
        }
    }

    private void AmbilPinjamanSyariah()
    {
        GameState.Instance.ChangeCoins(PinjamanSyariahAmount);
        GameState.Instance.ChangePinjamanSyariahCards(1);
        view.UpdateCoins(GameState.Instance.Coins);

        string resultText = "Mengambil pinjaman syariah. Mendapatkan " + PinjamanSyariahAmount
            + " koin dan 1 kartu pinjaman syariah. Kartu saat ini: "
            + GameState.Instance.PinjamanSyariahCards + "\n";
        ShowSystemDialogThen(resultText, UpdateMove);
    }

    private void KembalikanPinjamanSyariah()
    {
        if (GameState.Instance.PinjamanSyariahCards <= 0)
        {
            ShowSystemDialogThen("Tidak ada kartu pinjaman syariah yang dimiliki.\n", () => view.ShowChoice("Choice1"));
            return;
        }

        if (GameState.Instance.Coins < PinjamanSyariahAmount)
        {
            ShowSystemDialogThen("Koin tidak cukup untuk mengembalikan pinjaman syariah.\n", () => view.ShowChoice("Choice1"));
            return;
        }

        GameState.Instance.ChangeCoins(-PinjamanSyariahAmount);
        GameState.Instance.ChangePinjamanSyariahCards(-1);
        view.UpdateCoins(GameState.Instance.Coins);

        string resultText = "Mengembalikan pinjaman syariah. Membayar " + PinjamanSyariahAmount
            + " koin dan mengurangi 1 kartu pinjaman syariah. Kartu tersisa: "
            + GameState.Instance.PinjamanSyariahCards + "\n";
        ShowSystemDialogThen(resultText, UpdateMove);
    }
}
