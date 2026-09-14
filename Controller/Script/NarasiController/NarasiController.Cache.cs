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

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("DataManager belum siap.");
            return false;
        }

        BuildNarasiCache();
        isNarasiCacheReady = true;
        return true;
    }

    private void BuildNarasiCache()
    {
        narasiList.Clear();
        dialogKarakterList.Clear();

        if (DataManager.Instance.narasiDict != null)
        {
            foreach (var n in DataManager.Instance.narasiDict.Values)
            {
                if (n.prerequisiteAksi == null || n.prerequisiteAksi.Count == 0)
                {
                    continue;
                }

                narasiList.Add(n);
            }
        }

        if (DataManager.Instance.dialogKarakterDict != null)
        {
            foreach (var dialog in DataManager.Instance.dialogKarakterDict.Values)
            {
                if (dialog == null || string.IsNullOrWhiteSpace(dialog.aksi))
                {
                    continue;
                }

                dialogKarakterList.Add(dialog);
            }
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
