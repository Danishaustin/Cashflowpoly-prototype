using System.Collections.Generic;
using UnityEngine;

public partial class NarasiController : MonoBehaviour
{
    public static NarasiController Instance { get; private set; }

    [SerializeField] private UIManagerPlay view;

    private List<NarasiData> narasiList;
    private List<DialogKarakterData> dialogKarakterList;
    private Coroutine currentNarasiCoroutine;
    private bool isNarasiCacheReady;

    void Awake()
    {
        Instance = this;
        narasiList = new List<NarasiData>();
        dialogKarakterList = new List<DialogKarakterData>();
    }

    private int GetActivePlayerTurn()
    {
        return GameState.Instance != null ? GameState.Instance.turn : 1;
    }
}
