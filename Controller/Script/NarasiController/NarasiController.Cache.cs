using UnityEngine;

public partial class NarasiController
{
    // Menyiapkan dialog dari paket narasi aktif sekali saja, tanpa membaca DataManager tiap aksi.
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
        dialogKarakterList.Clear();

        if (DataManager.Instance.dialogKarakterDict == null)
        {
            Debug.LogWarning("Data dialog karakter belum dimuat.");
            return;
        }

        foreach (DialogKarakterData dialog in DataManager.Instance.dialogKarakterDict.Values)
        {
            // Dialog tanpa aksi atau tanpa baris berisi tidak bisa dipicu, jadi tidak perlu ikut dipertimbangkan.
            if (dialog == null || string.IsNullOrWhiteSpace(dialog.aksi) || GetPlayableLines(dialog).Count == 0)
            {
                continue;
            }

            dialogKarakterList.Add(dialog);
        }
    }

    public void ReloadNarasi()
    {
        isNarasiCacheReady = false;
        if (EnsureNarasiCache())
        {
            Debug.Log("Data narasi dimuat ulang (" + dialogKarakterList.Count + " dialog siap pakai).");
        }
    }
}
