using UnityEngine;
using System.Collections.Generic;

// Satu kartu pinjaman syariah aktif. LoanId kosong berarti pinjaman lokal yang belum tercatat di server.
public class PinjamanSyariahHolding
{
    public string LoanId;
    public string LoanCode;
    public string ItemName;
    public int Principal;
    public int RepaymentAmount;
}

public partial class GameState
{
    private const int DefaultPinjamanSyariahAmount = 10;

    // Per-player daftar pinjaman aktif, urut dari yang paling lama diambil.
    private void InitializePinjamanSyariahCards()
    {
        playerPinjamanSyariahCards = new Dictionary<int, List<PinjamanSyariahHolding>>();

        for (int player = 1; player <= playerCount; player++)
        {
            playerPinjamanSyariahCards[player] = new List<PinjamanSyariahHolding> { CreateLocalPinjamanSyariah() };
        }
    }

    private void EnsurePinjamanSyariahCards(int player)
    {
        if (playerPinjamanSyariahCards == null)
        {
            InitializePinjamanSyariahCards();
        }

        if (!playerPinjamanSyariahCards.ContainsKey(player))
        {
            playerPinjamanSyariahCards[player] = new List<PinjamanSyariahHolding> { CreateLocalPinjamanSyariah() };
        }
    }

    public int GetPinjamanSyariahCards(int player)
    {
        EnsurePinjamanSyariahCards(player);
        return playerPinjamanSyariahCards[player].Count;
    }

    public PinjamanSyariahHolding GetOldestPinjamanSyariah(int player)
    {
        EnsurePinjamanSyariahCards(player);
        List<PinjamanSyariahHolding> holdings = playerPinjamanSyariahCards[player];
        return holdings.Count > 0 ? holdings[0] : null;
    }

    public void AddPinjamanSyariah(int player, PinjamanSyariahHolding holding)
    {
        if (holding == null)
        {
            return;
        }

        EnsurePinjamanSyariahCards(player);
        playerPinjamanSyariahCards[player].Add(holding);
    }

    public bool RemovePinjamanSyariah(int player, PinjamanSyariahHolding holding)
    {
        EnsurePinjamanSyariahCards(player);
        return playerPinjamanSyariahCards[player].Remove(holding);
    }

    public void SetPinjamanSyariahList(int player, IEnumerable<PinjamanSyariahHolding> holdings)
    {
        EnsurePinjamanSyariahCards(player);
        List<PinjamanSyariahHolding> playerHoldings = playerPinjamanSyariahCards[player];
        playerHoldings.Clear();

        if (holdings == null)
        {
            return;
        }

        foreach (PinjamanSyariahHolding holding in holdings)
        {
            if (holding != null)
            {
                playerHoldings.Add(holding);
            }
        }
    }

    // Dipakai alur lokal (opsi darurat Risiko Kehidupan) yang belum mengirim event pinjaman ke server.
    public void ChangePinjamanSyariahCards(int amount)
    {
        EnsurePinjamanSyariahCards(turn);
        List<PinjamanSyariahHolding> holdings = playerPinjamanSyariahCards[turn];

        for (int i = 0; i < amount; i++)
        {
            holdings.Add(CreateLocalPinjamanSyariah());
        }

        for (int i = 0; i < -amount && holdings.Count > 0; i++)
        {
            holdings.RemoveAt(holdings.Count - 1);
        }
    }

    private static PinjamanSyariahHolding CreateLocalPinjamanSyariah()
    {
        return new PinjamanSyariahHolding
        {
            LoanId = string.Empty,
            LoanCode = string.Empty,
            ItemName = "Pinjaman Syariah",
            Principal = DefaultPinjamanSyariahAmount,
            RepaymentAmount = DefaultPinjamanSyariahAmount
        };
    }
}
