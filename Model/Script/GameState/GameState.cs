using UnityEngine;
using System.Collections.Generic;

public partial class GameState : MonoBehaviour
{
    // Singleton
    public static GameState Instance { get; private set; }

    // Initial Player Stats
    private const int InitialCoins = 20;
    private const int InitialHappiness = 0;
    private const int InitialSaving = 0;

    // Player Turn
    public int turn = 1;
    public int playerCount = 3;

    // Active Player Stats
    public int Coins => GetCoins(turn);
    public int Happiness => GetHappiness(turn);
    public int Saving => GetSaving(turn);

    // Player Stat Storage
    private Dictionary<int, int> playerCoins;
    private Dictionary<int, int> playerHappiness;
    private Dictionary<int, int> playerSaving;

    // Pinjaman Syariah
    public int PinjamanSyariahCards => GetPinjamanSyariahCards(turn);
    private Dictionary<int, int> playerPinjamanSyariahCards;

    // Game Progress
    public int day { get; private set; } = 1;
    public int movesLeft { get; private set; } = 2;
    public int finishDay { get; private set; } = 25;
    public bool isGameOver { get; private set; } = false;
    public bool isPaused { get; private set; } = false;

    // Temporary UI State
    public int SavingText { get; private set; } = 0;
    public int HargaEmasText { get; private set; } = 0;
    public int JumlahEmasText { get; private set; } = 0;
    public int JumatBerkah { get; private set; } = 0;
    public string kebutuhanSelected { get; private set; } = "";

    // Investasi Emas
    public int HargaEmasSaatIni { get; private set; } = 0;
    public int Emas => GetEmas(turn);
    private Dictionary<int, int> playerEmas;

    // Target Kebutuhan
    public string TargetKebutuhanTerpilihId => GetTargetKebutuhanId(turn);

    // Peduli Donasi
    public Dictionary<int, List<int>> JuaraPeduliDonasi { get; private set; }
    private Dictionary<int, int> playerPeduliDonasi;

    // Inventory
    public Dictionary<string, int> bahanList => GetBahanList(turn);
    public Dictionary<string, List<string>> kebutuhanList => GetKebutuhanList(turn);
    public bool AsuransiDimiliki => GetAsuransiDimiliki(turn);
    public string tujuanFinansial { get; private set; } = "";
    private Dictionary<int, Dictionary<string, int>> playerBahanList;
    private Dictionary<int, Dictionary<string, List<string>>> playerKebutuhanList;
    private Dictionary<int, List<string>> playerTujuanFinansialList;
    private Dictionary<int, List<string>> playerMasakanDijualList;
    private Dictionary<int, bool> playerAsuransiDimiliki;
    private Dictionary<int, string> playerTargetKebutuhanId;

    // Narasi
    public int jmAksiKe = 1;
    public int tfAksiKe = 1;
    public int bmAksiKe = 1;
    public int klAksiKe = 1;
    public int kAksiKe = 1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        InitializePlayerStats();
        InitializePinjamanSyariahCards();
        InitializeInvestasiEmas();
        InitializePeduliDonasi();
        InitializeInventory();
    }

}
