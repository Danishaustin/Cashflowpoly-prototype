using System;

public partial class ChoiceController
{
    // Satu jalur narasi untuk semua aksi: dialog karakter dari paket narasi aktif bila ada,
    // lalu teks hasil aksi. Tanpa dialog yang cocok, cukup teks hasil aksinya saja.
    private void PlayNarasiThen(string aksi, string resultText, Action onComplete)
    {
        int aksiKe = GameState.Instance != null ? GameState.Instance.NextActionKe(aksi) : 0;
        PlayNarasiThen(aksi, aksiKe, resultText, onComplete);
    }

    private void PlayNarasiThen(string aksi, int aksiKe, string resultText, Action onComplete)
    {
        if (NarasiController.Instance != null
            && NarasiController.Instance.PlayDialogKarakterThen(aksi, aksiKe, resultText, onComplete))
        {
            return;
        }

        ShowSystemDialogThen(resultText, onComplete);
    }

    private int GetActivePlayerTurn()
    {
        return GameState.Instance != null ? GameState.Instance.turn : 1;
    }
}
