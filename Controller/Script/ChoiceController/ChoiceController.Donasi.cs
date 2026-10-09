using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    private bool isSubmittingDonasi;

    // Handles Peduli Donasi / Jumat Berkah: setiap pemain wajib berdonasi tepat satu kali sesuai urutan giliran.
    private void JumatBerkah(string selectedChoice)
    {
        Debug.Log($"{selectedChoice} dipilih");
        if (isSubmittingDonasi)
        {
            return;
        }

        int player = GameState.Instance.turn;
        int minAmount = GetMinDonasi();
        int maxAmount = GetMaxDonasi(player);

        switch (selectedChoice)
        {
            case "MaxButtonJB":
                GameState.Instance.SetSavingText(maxAmount);
                break;
            case "MinButtonJB":
                GameState.Instance.SetSavingText(Mathf.Min(minAmount, maxAmount));
                break;
            case "IncreaseButtonJB":
                if (GameState.Instance.SavingText < maxAmount)
                {
                    GameState.Instance.ChangeSavingText(1);
                }
                break;
            case "DecreaseButtonJB":
                if (GameState.Instance.SavingText > minAmount)
                {
                    GameState.Instance.ChangeSavingText(-1);
                }
                break;
            case "ConfirmButtonJB":
                int amount = GameState.Instance.SavingText;
                if (amount < minAmount || amount > maxAmount)
                {
                    ShowSystemDialogThen("Donasi harus " + minAmount + " sampai " + maxAmount + " koin.", () => view.ShowChoice("JumatBerkah"));
                    return;
                }

                AskConfirmation(
                    "Donasi " + amount + " koin?",
                    () => _ = DonasiAsync(player, amount),
                    () => view.ShowChoice("JumatBerkah"));
                return;
            default:
                Debug.Log("Pilihan tidak valid");
                ShowSystemDialogThen("Pilihan tidak valid.", () => view.ShowChoice("JumatBerkah"));
                return;
        }

        view.UpdateJumatBerkahText(GameState.Instance.SavingText);
    }

    private int GetMinDonasi()
    {
        return Mathf.Max(1, GameState.Instance.DonationMinAmount);
    }

    private int GetMaxDonasi(int player)
    {
        return Mathf.Min(GameState.Instance.GetCoins(player), Mathf.Max(GetMinDonasi(), GameState.Instance.DonationMaxAmount));
    }

    private async Task DonasiAsync(int player, int amount)
    {
        string playerName = GetPlayerName(player);
        NarafinSessionOperationResult result = await SendDonasiEventAsync(
            player,
            "JumatBerkah",
            BuildJumatBerkahPayload(amount),
            0,
            "Mencatat donasi " + playerName + "...");

        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            ShowSystemDialogThen("Donasi " + playerName + " ditolak: " + result.ErrorMessage, () => view.ShowChoice("JumatBerkah"));
            return;
        }

        GameState.Instance.CatatPeduliDonasi(amount);
        GameState.Instance.ChangeCoins(player, -amount);
        view.UpdateCoins(GameState.Instance.GetCoins(player));
        bool isHariBerganti = GameState.Instance.IsLastPlayerInTurnOrder(player);

        // Narasi donasi diputar lebih dulu, selagi giliran masih milik pemain yang berdonasi. Kalau giliran
        // dimajukan dulu, dialognya tampil dengan potret dan prasyarat pemain berikutnya.
        PlayNarasiThen(
            "JumatBerkah",
            playerName + " berdonasi " + amount + " coin.",
            () => _ = SelesaikanLangkahDonasiAsync(isHariBerganti));
    }

    // Penutup satu langkah donasi. Urutannya penting: juara donasi dan poin peringkatnya adalah peristiwa
    // hari Jumat, jadi harus tercatat SEBELUM AkhirGiliran menutup harinya. Kalau dibalik, server sudah
    // berpindah hari dan menolak keduanya dengan "Event harus dicatat pada hari aktif N".
    private async Task SelesaikanLangkahDonasiAsync(bool isHariBerganti)
    {
        string juaraText = string.Empty;

        if (isHariBerganti)
        {
            // Peringkat dihitung DULU. Sebelumnya CatatJuaraPeduliDonasiAsync membacanya di sini
            // padahal baru dihitung belasan baris kemudian oleh AdvancePeduliDonasiTurn, sehingga
            // yang terbaca selalu peringkat Jumat sebelumnya -- dan pada Jumat pertama kosong.
            GameState.Instance.FinalisasiPeduliDonasiEvent();

            juaraText = await CatatJuaraPeduliDonasiAsync();
            if (this == null)
            {
                return;
            }

            await PostAkhirGiliranForDayEndAsync();
            if (this == null)
            {
                return;
            }

            await PlayEndingHariRollingAsync();
            if (this == null)
            {
                return;
            }
        }

        bool isPeduliDonasiSelesai = GameState.Instance.AdvancePeduliDonasiTurn();
        view.UpdateDay(GameState.Instance.day);
        view.UpdatePlayerTurn(GameState.Instance.turn);
        view.UpdatePlayerStats();

        if (!isPeduliDonasiSelesai)
        {
            ShowJumatBerkahOrSkipNoCoins();
            return;
        }

        if (string.IsNullOrEmpty(juaraText))
        {
            ShowNextScheduledChoice();
            return;
        }

        ShowSystemDialogThen(juaraText, ShowNextScheduledChoice);
    }

    // Donasi Jumat wajib; pemain yang koinnya kurang melakukan Kerja Lepas dulu (diterima server sebelum donasi)
    // agar hari Jumat tidak macet.
    private void ShowJumatBerkahOrSkipNoCoins()
    {
        int player = GameState.Instance.turn;
        if (GameState.Instance.GetCoins(player) >= GetMinDonasi())
        {
            view.ShowChoice("JumatBerkah");
            return;
        }

        string playerName = GetPlayerName(player);
        ShowSystemDialogThen(
            playerName + " tidak memiliki cukup koin untuk donasi wajib, jadi melakukan Kerja Lepas terlebih dahulu.",
            () => _ = KerjaLepasSebelumDonasiAsync(player));
    }

    private async Task KerjaLepasSebelumDonasiAsync(int player)
    {
        string playerName = GetPlayerName(player);
        int income = GameState.Instance.FreelanceIncome;
        NarafinSessionOperationResult result = await SendDonasiEventAsync(
            player,
            "KerjaLepas",
            BuildKerjaLepasPayload(income),
            GetCurrentActionSlot(),
            "Mencatat kerja lepas " + playerName + "...");

        if (this == null)
        {
            return;
        }

        if (!result.Success)
        {
            ShowSystemDialogThen(
                "Kerja lepas " + playerName + " ditolak: " + result.ErrorMessage + " Donasi Jumat wajib, jadi coba lagi.",
                ShowJumatBerkahOrSkipNoCoins);
            return;
        }

        GameState.Instance.ChangeCoins(player, income);
        GameState.Instance.ConsumeMoveWithoutTurnProgress();
        view.UpdateCoins(GameState.Instance.GetCoins(player));
        ShowSystemDialogThen(playerName + " bekerja lepas mendapatkan " + income + " koin.", ShowJumatBerkahOrSkipNoCoins);
    }

    private async Task<NarafinSessionOperationResult> SendDonasiEventAsync(int player, string actionType, string payloadJson, int actionSlot, string progressText)
    {
        isSubmittingDonasi = true;
        BeginServerWait(progressText);

        NarafinSessionOperationResult result;
        try
        {
            result = await SendPlayerEventNowAsync(player, actionType, payloadJson, actionSlot);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal mengirim event Jumat: " + ex.Message);
            result = CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
        }
        finally
        {
            isSubmittingDonasi = false;
        }

        await EndServerWaitAsync();
        return result;
    }

    // Peringkat donasi dihitung klien lalu dikirim ke server, karena server tidak menghitungnya sendiri.
    // Mengembalikan teks pengumuman; penampilannya diserahkan ke pemanggil agar urutan event tetap terjaga.
    private async Task<string> CatatJuaraPeduliDonasiAsync()
    {
        List<int> ranking = GameState.Instance.GetLatestPeduliDonasiRanking();
        string juaraText = BuildJuaraPeduliDonasiText();

        if (ranking != null && ranking.Count > 0)
        {
            NarafinSessionOperationResult result;
            try
            {
                result = await SendSystemEventNowAsync("UmumkanJuaraDonasi", BuildUmumkanJuaraDonasiPayload(ranking));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Gagal mengumumkan juara donasi: " + ex.Message);
                result = CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
            }

            if (this == null)
            {
                return string.Empty;
            }

            if (result.Success)
            {
                juaraText += await CatatPoinJuaraDonasiAsync(ranking);
            }
            else
            {
                juaraText += "\nJuara donasi gagal dicatat di server: " + result.ErrorMessage;
            }

            if (this == null)
            {
                return string.Empty;
            }
        }

        return juaraText;
    }

    // Tiap juara mendapat poin kebahagiaan sesuai katalog, dicatat satu event SYSTEM per pemain.
    private async Task<string> CatatPoinJuaraDonasiAsync(List<int> ranking)
    {
        string gagalText = string.Empty;
        int winnerCount = Mathf.Min(3, ranking.Count);

        for (int i = 0; i < winnerCount; i++)
        {
            int player = ranking[i];
            int rank = i + 1;
            int points = NarafinActiveSession.GetDonationRankPoints(NarafinActiveSession.Catalog, rank);
            if (points <= 0)
            {
                continue;
            }

            NarafinSessionOperationResult result;
            try
            {
                result = await SendSystemEventForPlayerNowAsync(player, "PoinPeringkatDonasi", BuildPoinPeringkatDonasiPayload(rank, points));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Gagal mencatat poin juara donasi: " + ex.Message);
                result = CreateEventFailure("EVENT_SEND_FAILED", "Gagal menghubungi server.");
            }

            if (this == null)
            {
                return string.Empty;
            }

            if (result.Success)
            {
                GameState.Instance.SetHappiness(player, GameState.Instance.GetHappiness(player) + points);
                continue;
            }

            gagalText += "\nPoin juara " + rank + " gagal dicatat: " + result.ErrorMessage;
        }

        view.UpdatePlayerStats();
        return gagalText;
    }

    private string BuildJuaraPeduliDonasiText()
    {
        List<int> ranking = GameState.Instance.GetLatestPeduliDonasiRanking();
        if (ranking == null || ranking.Count == 0)
        {
            return string.Empty;
        }

        int topCount = Mathf.Min(3, ranking.Count);
        var parts = new List<string>();
        for (int i = 0; i < topCount; i++)
        {
            string juaraName = GetPlayerName(ranking[i]);
            int points = NarafinActiveSession.GetDonationRankPoints(NarafinActiveSession.Catalog, i + 1);
            parts.Add("Juara " + (i + 1) + ": " + juaraName + (points > 0 ? " (+" + points + " kebahagiaan)" : string.Empty));
        }

        return string.Join(" | ", parts);
    }

    private string GetPlayerName(int player)
    {
        return PlayerPrefs.GetString("PlayerName_" + player, "Player " + player);
    }
}
