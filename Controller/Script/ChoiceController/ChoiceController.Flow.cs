using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    private int lastJumatAnnouncementDay = -1;
    private bool isLewatiHariMinggu;
    private int lastSabtuAnnouncementDay = -1;

    private int lastMingguAnnouncementDay = -1;
    private int lastSabtuLiburAnnouncementDay = -1;
    private bool isLewatiHariSabtu;

    // Intro Hari bergulir: satu rangkaian per hari yang menyapa tiap pemain sesuai urutan giliran.
    // Giliran digeser sementara agar prasyarat, sprite, nama, dan efek quest menunjuk pemain yang disapa,
    // lalu dipulihkan — pola yang sama dipakai alur risiko kehidupan dan emas-dari-risiko.
    private int lastIntroHariDay = -1;
    private int introHariOriginalTurn;
    private int introHariOriginalMovesLeft;
    private bool isIntroHariRolling;
    private readonly List<int> introHariQueue = new List<int>();

    // Shared flow helpers used by each choice action.
    private void UpdateMove()
    {
        _ = UpdateMoveAsync();
    }

    // Hari baru ditutup di server dulu, baru giliran/hari klien berlanjut.
    private async Task UpdateMoveAsync()
    {
        int previousTurn = GameState.Instance.turn;
        bool willDayAdvance = WillDayAdvanceAfterAction(previousTurn);
        await PostAkhirGiliranIfDayWillAdvanceAsync(previousTurn);
        if (this == null)
        {
            return;
        }

        // Narasi akhir hari diputar setelah aksi terakhir dan sebelum hari berganti.
        if (willDayAdvance)
        {
            await PlayEndingHariRollingAsync();
            if (this == null)
            {
                return;
            }
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
        // Sesi yang ditutup backend tidak bisa dilanjutkan, jadi permainan dihentikan di sini.
        if (NarafinActiveSession.IsSessionClosedByServer)
        {
            if (NarafinActiveSession.ConsumeSessionClosedNotice())
            {
                view.HideAllChoiceContainers();
                ShowSystemDialogThen("Sesi sudah ditutup server, jadi permainan dihentikan. " + NarafinActiveSession.SessionClosedMessage + "\n", null);
            }

            return;
        }

        if (NarafinActiveSession.ConsumeOfflineNotice())
        {
            ShowSystemDialogThen("Server tidak bisa dihubungi. Permainan dilanjutkan dalam mode offline; sisa sesi tidak tercatat di server.\n", ShowNextScheduledChoice);
            return;
        }

        // Diperiksa sebelum cabang hari khusus, supaya donasi Jumat atau emas Sabtu tidak menimpa akhir permainan.
        if (GameState.Instance.IsGameOver())
        {
            ShowGameFinished();
            return;
        }

        // Sabtu yang tidak dipakai ruleset (mis. mode Pemula) dilewati seperti hari Minggu libur.
        if (GameState.Instance.IsHariSabtuLibur())
        {
            if (lastSabtuLiburAnnouncementDay != GameState.Instance.day)
            {
                lastSabtuLiburAnnouncementDay = GameState.Instance.day;
                ShowSystemDialogThen("Hari Sabtu libur, tidak ada aksi hari ini.\n", ShowNextScheduledChoice);
                return;
            }

            if (TryPlayIntroHari())
            {
                return;
            }

            _ = LewatiHariSabtuAsync();
            return;
        }

        // Hari Minggu libur: keterangan sistem dulu, lalu narasi awal hari, baru harinya dilewati.
        if (GameState.Instance.IsHariMingguLibur())
        {
            if (lastMingguAnnouncementDay != GameState.Instance.day)
            {
                lastMingguAnnouncementDay = GameState.Instance.day;
                ShowSystemDialogThen("Hari Minggu libur, tidak ada aksi hari ini.\n", ShowNextScheduledChoice);
                return;
            }

            if (TryPlayIntroHari())
            {
                return;
            }

            _ = LewatiHariMingguAsync();
            return;
        }

        if (GameState.Instance.IsJumatBerkah())
        {
            if (lastJumatAnnouncementDay != GameState.Instance.day)
            {
                lastJumatAnnouncementDay = GameState.Instance.day;
                ShowSystemDialogThen("Hari Jumat, saatnya melakukan donasi.", ShowNextScheduledChoice);
                return;
            }

            if (TryPlayIntroHari())
            {
                return;
            }

            ShowJumatBerkahOrSkipNoCoins();
        }
        else if (GameState.Instance.IsInvestasiEmasDay())
        {
            if (lastSabtuAnnouncementDay != GameState.Instance.day)
            {
                lastSabtuAnnouncementDay = GameState.Instance.day;
                ShowSystemDialogThen("Hari Sabtu, saatnya Investasi Emas.", ShowNextScheduledChoice);
                return;
            }

            if (TryPlayIntroHari())
            {
                return;
            }

            ShowInvestasiEmasHargaInput();
        }
        else
        {
            if (TryPlayIntroHari())
            {
                return;
            }

            view.ShowChoice("Choice1");
        }
    }

    // Narasi awal hari diputar sekali per hari, sesudah keterangan hari khusus dan sebelum isi harinya.
    // true berarti narasinya sedang diputar dan kelanjutannya diserahkan ke onComplete.
    private bool TryPlayIntroHari()
    {
        // ShowNextScheduledChoice dipanggil ulang dari onComplete rangkaian ini, jadi perlu penjaga masuk ganda.
        if (isIntroHariRolling || lastIntroHariDay == GameState.Instance.day)
        {
            return false;
        }

        lastIntroHariDay = GameState.Instance.day;
        introHariOriginalTurn = GameState.Instance.turn;
        introHariOriginalMovesLeft = GameState.Instance.movesLeft;

        introHariQueue.Clear();
        int player = GameState.Instance.GetFirstPlayerInTurnOrder();
        for (int i = 0; i < GameState.Instance.playerCount; i++)
        {
            introHariQueue.Add(player);
            player = GameState.Instance.GetNextPlayerInTurnOrder(player);
        }

        isIntroHariRolling = true;
        return PlayNextIntroHari();
    }

    // true berarti ada narasi yang sedang diputar dan kelanjutannya diserahkan ke OnIntroHariSelesai.
    // false berarti rangkaiannya habis, giliran sudah dipulihkan, dan pemanggil boleh lanjut seperti biasa.
    private bool PlayNextIntroHari()
    {
        while (introHariQueue.Count > 0)
        {
            int player = introHariQueue[0];
            introHariQueue.RemoveAt(0);

            GameState.Instance.SetTurnAndMoves(player, introHariOriginalMovesLeft);
            view.UpdatePlayerTurn(player);
            view.UpdatePlayerStats();

            if (PlayNarasiIfAnyThen("IntroHari", 0, OnIntroHariSelesai))
            {
                return true;
            }
        }

        FinishIntroHariRolling();
        return false;
    }

    private void OnIntroHariSelesai()
    {
        if (PlayNextIntroHari())
        {
            return;
        }

        ShowNextScheduledChoice();
    }

    // Ending Hari juga bergulir: tiap pemain disapa sesuai urutan giliran sebelum hari berganti.
    // Sama seperti Intro Hari, giliran digeser sementara supaya prasyarat dan tampilan menunjuk pemain
    // yang disapa, lalu dipulihkan agar penutupan hari tetap memakai giliran yang sebenarnya.
    private async Task PlayEndingHariRollingAsync()
    {
        if (GameState.Instance == null)
        {
            return;
        }

        int originalTurn = GameState.Instance.turn;
        int originalMovesLeft = GameState.Instance.movesLeft;

        try
        {
            int player = GameState.Instance.GetFirstPlayerInTurnOrder();
            int playerCount = GameState.Instance.playerCount;
            for (int i = 0; i < playerCount; i++)
            {
                GameState.Instance.SetTurnAndMoves(player, originalMovesLeft);
                view.UpdatePlayerTurn(player);
                view.UpdatePlayerStats();

                await PlayNarasiIfAnyAsync("EndingHari", 0);
                if (this == null)
                {
                    return;
                }

                player = GameState.Instance.GetNextPlayerInTurnOrder(player);
            }
        }
        finally
        {
            if (this != null && GameState.Instance != null)
            {
                GameState.Instance.SetTurnAndMoves(originalTurn, originalMovesLeft);
                view.UpdatePlayerTurn(GameState.Instance.turn);
                view.UpdatePlayerStats();
            }
        }
    }

    private void FinishIntroHariRolling()
    {
        isIntroHariRolling = false;
        introHariQueue.Clear();

        // Giliran dan sisa aksi dipulihkan apa pun hasilnya, termasuk bila tidak ada dialog yang cocok.
        GameState.Instance.SetTurnAndMoves(introHariOriginalTurn, introHariOriginalMovesLeft);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();
    }

    // Sabtu libur hanya menutup hari di server; tidak ada aksi khusus seperti HariMingguLibur.
    // Hari libur tidak mengirim event apa pun. Server memegang kalender dan sudah melompati Sabtu dan
    // Minggu sendiri begitu hari kerja terakhir ditutup, jadi AkhirGiliran maupun HariMingguLibur untuk
    // hari-hari itu ditolak dengan "Event harus dicatat pada hari aktif N". Layar dan narasi hari libur
    // tetap dijalankan karena murni urusan klien.
    private async Task LewatiHariSabtuAsync()
    {
        if (isLewatiHariSabtu)
        {
            return;
        }

        isLewatiHariSabtu = true;
        try
        {
            await PlayEndingHariRollingAsync();
        }
        finally
        {
            isLewatiHariSabtu = false;
        }

        if (this == null)
        {
            return;
        }

        GameState.Instance.LewatiHariSabtu();
        view.UpdateDay(GameState.Instance.day);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();
        ShowNextScheduledChoice();
    }

    // Minggu libur: sistem mencatat hari libur lalu menutup hari agar hari di server ikut maju.
    private async Task LewatiHariMingguAsync()
    {
        if (isLewatiHariMinggu)
        {
            return;
        }

        isLewatiHariMinggu = true;
        try
        {
            await PlayEndingHariRollingAsync();
        }
        finally
        {
            isLewatiHariMinggu = false;
        }

        if (this == null)
        {
            return;
        }

        GameState.Instance.LewatiHariMinggu();
        view.UpdateDay(GameState.Instance.day);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();
        ShowNextScheduledChoice();
    }

    public void ShowCurrentDayChoice()
    {
        ShowNextScheduledChoice();
    }
}
