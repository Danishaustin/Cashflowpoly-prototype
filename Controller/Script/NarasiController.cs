using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class NarasiController : MonoBehaviour
{
    public static NarasiController Instance { get; private set; }
    [SerializeField] private UIManagerPlay view;
    private Dictionary<string, Dictionary<int, List<string>>> narasiAksiDict;
    private Coroutine currentNarasiCoroutine;
    private bool isNarasiCacheReady;

    void Awake()
    {
        Instance = this;
        narasiAksiDict = new Dictionary<string, Dictionary<int, List<string>>>();
    }

    private bool EnsureNarasiCache()
    {
        if (isNarasiCacheReady)
        {
            return true;
        }

        if (DataManager.Instance == null || DataManager.Instance.narasiDict == null)
        {
            Debug.LogWarning("Data narasi belum siap.");
            return false;
        }

        BuildNarasiCache();
        isNarasiCacheReady = true;
        return true;
    }

    private void BuildNarasiCache()
    {
        narasiAksiDict.Clear();

        foreach (var n in DataManager.Instance.narasiDict.Values)
        {
            if (string.IsNullOrWhiteSpace(n.aksi))
            {
                continue;
            }

            if (!narasiAksiDict.ContainsKey(n.aksi))
            {
                narasiAksiDict[n.aksi] = new Dictionary<int, List<string>>();
            }

            if (!narasiAksiDict[n.aksi].ContainsKey(n.aksiKe))
            {
                narasiAksiDict[n.aksi][n.aksiKe] = new List<string>();
            }

            AddNarasiText(n.aksi, n.aksiKe, n.narasi1);
            AddNarasiText(n.aksi, n.aksiKe, n.narasi2);
            AddNarasiText(n.aksi, n.aksiKe, n.narasi3);
        }
    }

    private void AddNarasiText(string aksi, int aksiKe, string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            narasiAksiDict[aksi][aksiKe].Add(text);
        }
    }

    public void ReloadNarasi()
    {
        isNarasiCacheReady = false;
        if (EnsureNarasiCache())
        {
            Debug.Log("Data narasi dimuat ulang.");
        }
    }

    public bool HandleNarasi(string aksi, int aksiKe, System.Action onComplete = null)
    {
        if (!EnsureNarasiCache())
        {
            return false;
        }

        if (!narasiAksiDict.ContainsKey(aksi))
        {
            return false;
        }

        var narasiByAksiKe = narasiAksiDict[aksi];
        int ke = aksiKe;
        if (!narasiByAksiKe.ContainsKey(ke))
        {
            if (!narasiByAksiKe.ContainsKey(0))
            {
                return false;
            }

            ke = 0;
        }


        if (view == null)
        {
            view = FindObjectOfType<UIManagerPlay>();
        }

        if (view == null)
        {
            Debug.LogWarning("UIManagerPlay belum dihubungkan ke NarasiController.");
            return false;
        }

        if (currentNarasiCoroutine != null)
        {
            StopCoroutine(currentNarasiCoroutine);
        }

        var narasiList = narasiByAksiKe[ke]
            .Where(narasi => !string.IsNullOrWhiteSpace(narasi))
            .ToList();

        if (narasiList.Count == 0)
        {
            return false;
        }

        currentNarasiCoroutine = StartCoroutine(PlayNarasi(narasiList, onComplete));
        return true;
    }

    private IEnumerator PlayNarasi(List<string> narasiList, System.Action onComplete)
    {
        yield return view.PlayDialogSteps(narasiList);
        currentNarasiCoroutine = null;
        onComplete?.Invoke();
    }
}
