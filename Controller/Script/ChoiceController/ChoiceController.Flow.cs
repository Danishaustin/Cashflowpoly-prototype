using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    private int lastJumatAnnouncementDay = -1;
    private bool isLewatiHariMinggu;
    private int lastSabtuAnnouncementDay = -1;

    // Shared flow helpers used by each choice action.
    private bool Narasi(string aksi, int aksiKe, System.Action onComplete = null)
    {
        Debug.Log("Menampilkan narasi untuk aksi " + aksi);

        return NarasiController.Instance.HandleNarasi(aksi, aksiKe, onComplete);
    }

    private void UpdateMove()
    {
        _ = UpdateMoveAsync();
    }

    // Hari baru ditutup di server dulu, baru giliran/hari klien berlanjut.
    private async Task UpdateMoveAsync()
    {
        int previousTurn = GameState.Instance.turn;
        await PostAkhirGiliranIfDayWillAdvanceAsync(previousTurn);
        if (this == null)
        {
            return;
        }

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
        if (GameState.Instance.IsHariMingguLibur())
        {
            _ = LewatiHariMingguAsync();
            return;
        }

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

    // Minggu libur: sistem mencatat hari libur lalu menutup hari agar hari di server ikut maju.
    private async Task LewatiHariMingguAsync()
    {
        if (isLewatiHariMinggu)
        {
            return;
        }

        isLewatiHariMinggu = true;
        NarafinSessionOperationResult result;
        try
        {
            result = await SendSystemEventNowAsync("HariMingguLibur", "{}");
            if (result.Success && this != null)
            {
                await PostAkhirGiliranForDayEndAsync();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Gagal mencatat hari Minggu libur: " + ex.Message);
            result = CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
        }
        finally
        {
            isLewatiHariMinggu = false;
        }

        if (this == null)
        {
            return;
        }

        // Hari tetap dilewati di klien walau server menolak, agar permainan tidak berhenti di hari libur.
        if (!result.Success && !NarafinRuntimeConfig.UseOfflineMode)
        {
            view.AddSystemTextToDialog("Hari Minggu gagal dicatat di server: " + result.ErrorMessage);
        }

        GameState.Instance.LewatiHariMinggu();
        view.UpdateDay(GameState.Instance.day);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();
        ShowSystemDialogThen("Hari Minggu libur, tidak ada aksi hari ini.\n", ShowNextScheduledChoice);
    }

    public void ShowCurrentDayChoice()
    {
        ShowNextScheduledChoice();
    }
}
