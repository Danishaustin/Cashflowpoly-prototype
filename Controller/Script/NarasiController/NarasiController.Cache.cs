using UnityEngine;

public partial class NarasiController
{
    // Menyiapkan dialog dari paket narasi aktif sekali saja, tanpa menyusun ulang indeksnya tiap aksi.
    private bool EnsureNarasiCache()
    {
        if (isNarasiCacheReady)
        {
            return true;
        }

        BuildNarasiCache();
        isNarasiCacheReady = true;
        return true;
    }

    private void BuildNarasiCache()
    {
        dialogKarakterList.Clear();

        if (NarasiSessionContext.DialogKarakterById.Count == 0)
        {
            Debug.LogWarning("Data dialog karakter belum dimuat.");
            return;
        }

        foreach (DialogKarakterData dialog in NarasiSessionContext.DialogKarakterById.Values)
        {
            // Dialog tanpa aksi atau tanpa baris berisi tidak bisa dipicu, jadi tidak perlu ikut dipertimbangkan.
            if (dialog == null || string.IsNullOrWhiteSpace(dialog.aksi) || GetPlayableLines(dialog).Count == 0)
            {
                continue;
            }

            WarnIfDayScopedDialogHasAksiValue(dialog);
            WarnIfQuestIdMissingFromPack(dialog);
            dialogKarakterList.Add(dialog);
        }
    }

    // State quest hidup di luar paket quest, jadi efek maupun prasyarat quest tetap "bekerja" walau
    // id-nya tidak ada di paket yang dipilih — hanya saja quest itu tidak akan pernah tampil di panel.
    // Diperingatkan supaya pasangan paket narasi dan paket quest yang tidak cocok cepat terlihat.
    private static void WarnIfQuestIdMissingFromPack(DialogKarakterData dialog)
    {
        WarnIfQuestIdUnknown(dialog.id, dialog.questId, "efek");

        if (dialog.prerequisite == null)
        {
            return;
        }

        foreach (DialogPrerequisiteData prerequisite in dialog.prerequisite)
        {
            WarnIfQuestIdUnknown(dialog.id, prerequisite?.questId, "prasyarat");
        }
    }

    private static void WarnIfQuestIdUnknown(string dialogId, string questId, string peran)
    {
        if (string.IsNullOrWhiteSpace(questId) || QuestSessionContext.QuestById.ContainsKey(questId))
        {
            return;
        }

        Debug.LogWarning("Dialog " + dialogId + " memakai quest " + peran + " \"" + questId
            + "\" yang tidak ada di paket quest aktif, jadi quest itu tidak akan tampil di panel quest.");
    }

    // IntroHari dan EndingHari selalu dipicu dengan aksiKe 0, jadi dialog lama yang masih memakai nomor hari
    // di aksiValue tidak akan pernah cocok. Diperingatkan di log supaya tidak hilang tanpa penjelasan.
    private static void WarnIfDayScopedDialogHasAksiValue(DialogKarakterData dialog)
    {
        if (dialog.aksiValue == 0)
        {
            return;
        }

        string aksi = (dialog.aksi ?? string.Empty).Trim();
        bool isDayScoped = aksi.Equals("IntroHari", System.StringComparison.OrdinalIgnoreCase)
            || aksi.Equals("EndingHari", System.StringComparison.OrdinalIgnoreCase)
            || aksi.Equals("Intro Hari", System.StringComparison.OrdinalIgnoreCase)
            || aksi.Equals("Ending Hari", System.StringComparison.OrdinalIgnoreCase);

        if (isDayScoped)
        {
            Debug.LogWarning("Dialog " + dialog.id + " (" + aksi + ") memakai aksiValue " + dialog.aksiValue
                + " sehingga tidak akan pernah diputar. Setel aksiValue ke 0 dan pindahkan harinya ke prasyarat hariKe/mingguKe.");
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
