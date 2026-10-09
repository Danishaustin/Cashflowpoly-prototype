using UnityEngine;
using System.Collections.Generic;

public partial class GameState
{
    // Tracks Peduli Donasi totals and rankings per event.
    private int peduliDonasiEventKe;

    // Menjaga agar satu putaran donasi hanya difinalisasi sekali, karena finalisasi kini dipanggil
    // pemanggil lebih dulu dan AdvancePeduliDonasiTurn tetap memanggilnya sebagai jaring pengaman.
    private bool peduliDonasiSudahDifinalisasi;
    private Dictionary<int, int> lastPeduliDonasiAmounts;
    private List<int> currentPeduliDonasiOrder;
    private Dictionary<int, int> playerDonasiJuara1Cards;
    private Dictionary<int, int> playerDonasiJuara2Cards;
    private Dictionary<int, int> playerDonasiJuara3Cards;

    private void InitializePeduliDonasi()
    {
        JuaraPeduliDonasi = new Dictionary<int, List<int>>();
        playerPeduliDonasi = new Dictionary<int, int>();
        currentPeduliDonasiOrder = new List<int>();
        lastPeduliDonasiAmounts = new Dictionary<int, int>();
        playerDonasiJuara1Cards = new Dictionary<int, int>();
        playerDonasiJuara2Cards = new Dictionary<int, int>();
        playerDonasiJuara3Cards = new Dictionary<int, int>();
        peduliDonasiEventKe = 0;
        peduliDonasiSudahDifinalisasi = false;

        for (int player = 1; player <= playerCount; player++)
        {
            playerPeduliDonasi[player] = 0;
            playerDonasiJuara1Cards[player] = 0;
            playerDonasiJuara2Cards[player] = 0;
            playerDonasiJuara3Cards[player] = 0;
        }
    }

    private void EnsurePeduliDonasiPlayer(int player)
    {
        if (JuaraPeduliDonasi == null || playerPeduliDonasi == null)
        {
            InitializePeduliDonasi();
        }

        if (!playerPeduliDonasi.ContainsKey(player))
        {
            playerPeduliDonasi[player] = 0;
        }

        if (!playerDonasiJuara1Cards.ContainsKey(player))
        {
            playerDonasiJuara1Cards[player] = 0;
        }

        if (!playerDonasiJuara2Cards.ContainsKey(player))
        {
            playerDonasiJuara2Cards[player] = 0;
        }

        if (!playerDonasiJuara3Cards.ContainsKey(player))
        {
            playerDonasiJuara3Cards[player] = 0;
        }
    }

    public void CatatPeduliDonasi(int amount)
    {
        EnsurePeduliDonasiPlayer(turn);
        if (!currentPeduliDonasiOrder.Contains(turn))
        {
            currentPeduliDonasiOrder.Add(turn);
        }

        playerPeduliDonasi[turn] += amount;
        JumatBerkah++;
        peduliDonasiSudahDifinalisasi = false;
    }

    public int GetPeduliDonasiTotal(int player)
    {
        EnsurePeduliDonasiPlayer(player);
        return playerPeduliDonasi[player];
    }

    // Donasi pada event Jumat terakhir; dipakai untuk mengumumkan juara ke server.
    public int GetPeduliDonasiTerakhir(int player)
    {
        return lastPeduliDonasiAmounts != null && lastPeduliDonasiAmounts.TryGetValue(player, out int amount) ? amount : 0;
    }

    public bool AdvancePeduliDonasiTurn()
    {
        if (!IsLastPlayerInTurnOrder(turn))
        {
            turn = GetNextPlayerInTurnOrder(turn);
            movesLeft = ActionsPerTurn;
            return false;
        }

        // Jaring pengaman: biasanya pemanggil sudah memfinalisasi lebih dulu agar peringkatnya
        // dapat diumumkan sebelum AkhirGiliran. Panggilan kedua ini tidak berefek.
        FinalisasiPeduliDonasiEvent();
        NextDay();
        return true;
    }

    public int GetDonasiJuaraCardCount(int player, int juaraRank)
    {
        EnsurePeduliDonasiPlayer(player);

        return juaraRank switch
        {
            1 => playerDonasiJuara1Cards[player],
            2 => playerDonasiJuara2Cards[player],
            3 => playerDonasiJuara3Cards[player],
            _ => 0
        };
    }

    public List<int> GetLatestPeduliDonasiRanking()
    {
        if (JuaraPeduliDonasi == null || JuaraPeduliDonasi.Count == 0)
        {
            return new List<int>();
        }

        if (JuaraPeduliDonasi.TryGetValue(peduliDonasiEventKe, out List<int> ranking))
        {
            return new List<int>(ranking);
        }

        return new List<int>();
    }

    // Menghitung peringkat juara Jumat ini dan membagikan kartu juaranya. Harus dipanggil SEBELUM
    // GetLatestPeduliDonasiRanking dibaca; tanpa itu yang terbaca peringkat Jumat sebelumnya.
    public void FinalisasiPeduliDonasiEvent()
    {
        if (peduliDonasiSudahDifinalisasi)
        {
            return;
        }

        peduliDonasiSudahDifinalisasi = true;

        var ranking = BuildPeduliDonasiRanking();
        lastPeduliDonasiAmounts = new Dictionary<int, int>(playerPeduliDonasi);
        peduliDonasiEventKe++;
        JuaraPeduliDonasi[peduliDonasiEventKe] = ranking;

        for (int rank = 1; rank <= 3; rank++)
        {
            if (ranking.Count < rank)
            {
                break;
            }

            int juaraPlayer = ranking[rank - 1];
            EnsurePeduliDonasiPlayer(juaraPlayer);

            switch (rank)
            {
                case 1:
                    playerDonasiJuara1Cards[juaraPlayer]++;
                    break;
                case 2:
                    playerDonasiJuara2Cards[juaraPlayer]++;
                    break;
                case 3:
                    playerDonasiJuara3Cards[juaraPlayer]++;
                    break;
            }
        }

        currentPeduliDonasiOrder.Clear();

        // Juara dihitung per hari Jumat, jadi total donasi tiap pemain dikosongkan untuk Jumat berikutnya.
        foreach (int player in new List<int>(playerPeduliDonasi.Keys))
        {
            playerPeduliDonasi[player] = 0;
        }
    }

    // Donasi Jumat wajib untuk setiap pemain dan server tidak bisa menutup hari Jumat bila ada pemain yang
    // tidak mampu berdonasi. Karena itu pada hari Kamis (dan Jumat sebelum pemain berdonasi) koin tidak boleh
    // dibelanjakan sampai di bawah minimum donasi.
    public int GetDonationReserve(int player)
    {
        if (!FridayEnabled)
        {
            return 0;
        }

        int dayOfWeek = GetDayOfWeek(day);
        bool isThursday = dayOfWeek == 4;
        bool isFridayBeforeDonation = dayOfWeek == 5
            && (currentPeduliDonasiOrder == null || !currentPeduliDonasiOrder.Contains(player));
        return isThursday || isFridayBeforeDonation ? Mathf.Max(0, DonationMinAmount) : 0;
    }

    public int GetSpendableCoins(int player)
    {
        return Mathf.Max(0, GetCoins(player) - GetDonationReserve(player));
    }

    // null bila koin boleh dibelanjakan; selain itu alasan penolakan untuk ditampilkan ke pemain.
    public string GetSpendBlockedMessage(int player, int amount)
    {
        if (GetCoins(player) < amount)
        {
            return "Koin tidak cukup.";
        }

        if (GetSpendableCoins(player) < amount)
        {
            return "Sisakan minimal " + GetDonationReserve(player) + " koin untuk donasi Jumat.";
        }

        return null;
    }

    // Peringkat donasi Jumat. Sesuai rulebook, bila nilainya seri pemenangnya adalah pemain dengan
    // angka Tie Breaker terbesar, yaitu nomor giliran terbesar (#4 > #3 > #2 > #1).
    private List<int> BuildPeduliDonasiRanking()
    {
        var ranking = new List<int>();

        for (int player = 1; player <= playerCount; player++)
        {
            EnsurePeduliDonasiPlayer(player);
            if (playerPeduliDonasi[player] > 0)
            {
                ranking.Add(player);
            }
        }

        ranking.Sort((a, b) =>
        {
            int donationCompare = playerPeduliDonasi[b].CompareTo(playerPeduliDonasi[a]);
            return donationCompare != 0 ? donationCompare : b.CompareTo(a);
        });

        return ranking;
    }
}
