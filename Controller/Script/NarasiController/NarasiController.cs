using System.Collections.Generic;
using UnityEngine;

public partial class NarasiController : MonoBehaviour
{
    public static NarasiController Instance { get; private set; }

    [SerializeField] private UIManagerPlay view;

    private List<DialogKarakterData> dialogKarakterList;
    private Coroutine currentNarasiCoroutine;
    private bool isNarasiCacheReady;
    private bool isPlayingNarasi;

    void Awake()
    {
        Instance = this;
        dialogKarakterList = new List<DialogKarakterData>();
    }

    private int GetActivePlayerTurn()
    {
        return GameState.Instance != null ? GameState.Instance.turn : 1;
    }
}
