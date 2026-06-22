using System.Collections.Generic;
using UnityEngine;

public partial class NarasiController
{
    // Keeps narasi data ready without re-reading DataManager on every action.
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
        BuildDialogKarakterCache();
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

    private void BuildDialogKarakterCache()
    {
        dialogKarakterList.Clear();

        if (DataManager.Instance.dialogKarakterDict == null)
        {
            return;
        }

        foreach (var d in DataManager.Instance.dialogKarakterDict.Values)
        {
            bool hasPrerequisite = (d.prerequisite != null && d.prerequisite.Count > 0)
                || (d.prerequisiteAksi != null && d.prerequisiteAksi.Count > 0);

            if (!hasPrerequisite)
            {
                continue;
            }

            dialogKarakterList.Add(d);
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
}
