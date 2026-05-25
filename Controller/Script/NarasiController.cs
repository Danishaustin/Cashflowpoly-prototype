using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class NarasiController : MonoBehaviour
{
    public static NarasiController Instance { get; private set; }
    [SerializeField] private UIManagerPlay view;
    private List<NarasiData> narasiList;
    private Coroutine currentNarasiCoroutine;
    private bool isNarasiCacheReady;

    void Awake()
    {
        Instance = this;
        narasiList = new List<NarasiData>();
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
        narasiList.Clear();

        foreach (var n in DataManager.Instance.narasiDict.Values)
        {
            if (n.prerequisiteAksi == null || n.prerequisiteAksi.Count == 0)
            {
                continue;
            }

            narasiList.Add(n);
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

        NarasiData selectedNarasi = GetNarasiByPrerequisite(aksi, aksiKe);
        if (selectedNarasi == null)
        {
            return false;
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

        var narasiList = new List<string>()
        {
            selectedNarasi.narasi1,
            selectedNarasi.narasi2,
            selectedNarasi.narasi3
        }
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        if (narasiList.Count == 0)
        {
            return false;
        }

        currentNarasiCoroutine = StartCoroutine(PlayNarasi(narasiList, onComplete));
        return true;
    }

    private NarasiData GetNarasiByPrerequisite(string aksi, int aksiKe)
    {
        var matchingNarasi = narasiList
            .Where(narasi => HasTriggerPrerequisite(narasi, aksi, aksiKe))
            .Where(narasi => IsPrerequisiteMet(narasi))
            .OrderByDescending(CountValidPrerequisites)
            .FirstOrDefault();

        if (matchingNarasi != null)
        {
            return matchingNarasi;
        }

        return narasiList
            .Where(narasi => HasTriggerPrerequisite(narasi, aksi, 0))
            .Where(narasi => IsPrerequisiteMet(narasi))
            .OrderByDescending(CountValidPrerequisites)
            .FirstOrDefault();
    }

    private bool HasTriggerPrerequisite(NarasiData narasi, string aksi, int value)
    {
        if (GameState.Instance == null || narasi.prerequisiteAksi == null)
        {
            return false;
        }

        string normalizedAksi = GameState.Instance.NormalizeActionName(aksi);
        return narasi.prerequisiteAksi.Any(prerequisite =>
            prerequisite != null
            && GameState.Instance.NormalizeActionName(prerequisite.aksi) == normalizedAksi
            && prerequisite.value == value);
    }

    private bool IsPrerequisiteMet(NarasiData narasi)
    {
        if (GameState.Instance == null)
        {
            return false;
        }

        var prerequisites = GetPrerequisites(narasi);
        if (prerequisites.Count == 0)
        {
            return true;
        }

        foreach (var prerequisite in prerequisites)
        {
            if (GameState.Instance.GetActionCount(prerequisite.aksi) < prerequisite.value)
            {
                return false;
            }
        }

        return true;
    }

    private int CountValidPrerequisites(NarasiData narasi)
    {
        return GetPrerequisites(narasi).Count;
    }

    private List<PrerequisiteAksiData> GetPrerequisites(NarasiData narasi)
    {
        if (narasi.prerequisiteAksi == null)
        {
            return new List<PrerequisiteAksiData>();
        }

        return narasi.prerequisiteAksi
            .Where(prerequisite => prerequisite != null && !string.IsNullOrWhiteSpace(prerequisite.aksi))
            .ToList();
    }

    private IEnumerator PlayNarasi(List<string> narasiList, System.Action onComplete)
    {
        yield return view.PlayDialogSteps(narasiList);
        currentNarasiCoroutine = null;
        onComplete?.Invoke();
    }
}
