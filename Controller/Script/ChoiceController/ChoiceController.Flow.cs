using UnityEngine;

public partial class ChoiceController
{
    private int lastJumatAnnouncementDay = -1;
    private int lastSabtuAnnouncementDay = -1;

    // Shared flow helpers used by each choice action.
    private bool Narasi(string aksi, int aksiKe, System.Action onComplete = null)
    {
        Debug.Log("Menampilkan narasi untuk aksi " + aksi);

        return NarasiController.Instance.HandleNarasi(aksi, aksiKe, onComplete);
    }

    private void UpdateMove()
    {
        int previousTurn = GameState.Instance.turn;
        PostAkhirGiliranIfDayWillAdvance(previousTurn);

        GameState.Instance.UseMove();

        view.UpdateDay(GameState.Instance.day);
        view.UpdatePlayerStats();

        if (previousTurn != GameState.Instance.turn)
        {
            view.PlayPlayerContainerExitThen(() =>
            {
                view.UpdatePlayerTurn(GameState.Instance.turn);
                ShowNextScheduledChoice();
            });
            return;
        }

        view.UpdatePlayerTurn(GameState.Instance.turn);
        ShowNextScheduledChoice();
    }

    private void ShowNextScheduledChoice()
    {
        if (GameState.Instance.IsJumatBerkah())
        {
            if (lastJumatAnnouncementDay != GameState.Instance.day)
            {
                lastJumatAnnouncementDay = GameState.Instance.day;
                ShowSystemDialogThen("Hari Jumat, saatnya melakukan donasi.", ShowJumatBerkahOrSkipNoCoins);
                return;
            }

            ShowJumatBerkahOrSkipNoCoins();
        }
        else if (GameState.Instance.IsInvestasiEmasDay())
        {
            if (lastSabtuAnnouncementDay != GameState.Instance.day)
            {
                lastSabtuAnnouncementDay = GameState.Instance.day;
                ShowSystemDialogThen("Hari Sabtu, saatnya Investasi Emas.", ShowInvestasiEmasHargaInput);
                return;
            }

            ShowInvestasiEmasHargaInput();
        }
        else
        {
            view.ShowChoice("Choice1");
        }
    }

    public void ShowCurrentDayChoice()
    {
        ShowNextScheduledChoice();
    }
}
