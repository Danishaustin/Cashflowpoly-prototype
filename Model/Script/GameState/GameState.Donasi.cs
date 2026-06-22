using UnityEngine;
using System.Collections.Generic;

public partial class GameState
{
    // Tracks Peduli Donasi totals and rankings per event.
    private int peduliDonasiEventKe;
    private List<int> currentPeduliDonasiOrder;
    private Dictionary<int, int> playerDonasiJuara1Cards;
    private Dictionary<int, int> playerDonasiJuara2Cards;
    private Dictionary<int, int> playerDonasiJuara3Cards;

    private void InitializePeduliDonasi()
    {
        JuaraPeduliDonasi = new Dictionary<int, List<int>>();
        playerPeduliDonasi = new Dictionary<int, int>();
        currentPeduliDonasiOrder = new List<int>();
        playerDonasiJuara1Cards = new Dictionary<int, int>();
        playerDonasiJuara2Cards = new Dictionary<int, int>();
        playerDonasiJuara3Cards = new Dictionary<int, int>();
        peduliDonasiEventKe = 0;

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
    }

    public int GetPeduliDonasiTotal(int player)
    {
        EnsurePeduliDonasiPlayer(player);
        return playerPeduliDonasi[player];
    }

    public bool AdvancePeduliDonasiTurn()
    {
        if (turn < playerCount)
        {
            turn++;
            movesLeft = 2;
            return false;
        }

        FinalizePeduliDonasiEvent();
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

    private void FinalizePeduliDonasiEvent()
    {
        var ranking = BuildPeduliDonasiRanking();
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
    }

    private List<int> BuildPeduliDonasiRanking()
    {
        var ranking = new List<int>();
        var orderMap = new Dictionary<int, int>();

        for (int i = 0; i < currentPeduliDonasiOrder.Count; i++)
        {
            int player = currentPeduliDonasiOrder[i];
            if (!orderMap.ContainsKey(player))
            {
                orderMap[player] = i;
            }
        }

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
            if (donationCompare != 0)
            {
                return donationCompare;
            }

            bool hasOrderA = orderMap.TryGetValue(a, out int orderA);
            bool hasOrderB = orderMap.TryGetValue(b, out int orderB);
            if (hasOrderA && hasOrderB)
            {
                int orderCompare = orderA.CompareTo(orderB);
                if (orderCompare != 0)
                {
                    return orderCompare;
                }
            }

            if (hasOrderA != hasOrderB)
            {
                return hasOrderA ? -1 : 1;
            }

            return a.CompareTo(b);
        });

        return ranking;
    }
}
