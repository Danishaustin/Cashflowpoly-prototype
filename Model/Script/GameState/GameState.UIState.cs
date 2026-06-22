public partial class GameState
{
    // Temporary values used by UI controls before an action is confirmed.
    public void SetSavingText(int amount)
    {
        SavingText = amount;
    }

    public void SetHargaEmasText(int amount)
    {
        HargaEmasText = amount;
    }

    public void ChangeHargaEmasText(int amount)
    {
        HargaEmasText += amount;
    }

    public void SetJumlahEmasText(int amount)
    {
        JumlahEmasText = amount;
    }

    public void ChangeJumlahEmasText(int amount)
    {
        JumlahEmasText += amount;
    }

    public void SetKebutuhanSelected(string kebutuhan)
    {
        kebutuhanSelected = kebutuhan;
    }

    public void ChangeSavingText(int amount)
    {
        SavingText += amount;
    }

    public void SetHargaEmasSaatIni(int amount)
    {
        HargaEmasSaatIni = amount;
    }

    public void SetPause(bool pause)
    {
        isPaused = pause;
    }
}
