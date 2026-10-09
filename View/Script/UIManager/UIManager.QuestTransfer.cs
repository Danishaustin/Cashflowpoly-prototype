using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManager
{
    // Tombol Ekspor dan Impor di layar Pilih Tantangan. Kembarannya untuk narasi ada di
    // UIManager.NarasiTransfer. Lapisan pengangkut berkasnya dipakai bersama (PackTransferIO);
    // yang berbeda hanya pembentuk amplop dan pemeriksanya, karena kontrak backend memang memisahkan
    // narrative-packs dan quest-packs.

    private void RegisterQuestTransferCallbacks()
    {
        editQuestPackExportButton?.RegisterCallback<ClickEvent>(evt => { _ = EksporQuestPackAsync(); });
        editQuestPackImportButton?.RegisterCallback<ClickEvent>(evt => MulaiImporQuestPack());
    }

    // ---------------------------------------------------------------- ekspor
    private async Task EksporQuestPackAsync()
    {
        if (isEditQuestLoading)
        {
            return;
        }

        string selectedOption = editQuestPackDropdown?.value;
        if (string.IsNullOrWhiteSpace(selectedOption)
            || !editQuestPackLookup.TryGetValue(selectedOption, out QuestPackData selectedPack))
        {
            ShowErrorPopup("Pilih paket tantangan yang ingin dibagikan.");
            return;
        }

        BeginEditQuestLoading("Menyiapkan berkas paket...");

        QuestDatabase database;
        try
        {
            database = await QuestPackRepository.LoadQuestDatabaseAsync(selectedPack);
        }
        catch (System.Exception ex)
        {
            EndEditQuestLoading();
            ShowErrorPopup("Gagal membaca isi paket. Detail: " + ex.Message);
            return;
        }

        EndEditQuestLoading();

        if (database == null || database.quest == null || database.quest.Count == 0)
        {
            ShowErrorPopup("Paket \"" + selectedPack.name
                + "\" belum punya tantangan, jadi tidak ada yang bisa dibagikan.");
            return;
        }

        string penulis = PlayerPrefs.GetString(NarafinPlayerPrefs.UsernameKey, string.Empty);
        PackTransferIOResult hasil = PackTransferIO.Ekspor(
            QuestPackTransfer.BuildEnvelopeJson(selectedPack, database, penulis),
            QuestPackTransfer.BuildFileName(selectedPack.name),
            "Paket tantangan " + selectedPack.name);

        if (!hasil.Success)
        {
            ShowErrorPopup(hasil.ErrorMessage);
            return;
        }

        ShowSuccessPopup(hasil.Keterangan);
    }

    // ---------------------------------------------------------------- impor
    private void MulaiImporQuestPack()
    {
        if (isEditQuestLoading || PackTransferIO.IsImporSedangBerjalan)
        {
            return;
        }

        PackTransferIO.PilihBerkas(hasil =>
        {
            if (hasil.Dibatalkan)
            {
                return;
            }

            if (!hasil.Success)
            {
                ShowErrorPopup(hasil.ErrorMessage);
                return;
            }

            _ = PasangQuestPackAsync(hasil.Json);
        });
    }

    private async Task PasangQuestPackAsync(string json)
    {
        QuestTransferReadResult dibaca = QuestPackTransfer.Read(json);
        if (!dibaca.Success)
        {
            ShowErrorPopup(dibaca.ErrorMessage);
            return;
        }

        BeginEditQuestLoading("Memasang paket tantangan...");

        QuestPackCreateResult hasil = await QuestPackTransfer.InstallAsync(dibaca.Envelope);

        EndEditQuestLoading();

        if (!hasil.Success || hasil.Pack == null)
        {
            ShowErrorPopup(hasil.ErrorMessage);
            return;
        }

        editQuestActivePack = hasil.Pack;
        await SetupEditQuestPackSelectionAsync();

        string pesan = "Paket \"" + hasil.Pack.name + "\" berhasil dipasang ("
            + dibaca.Envelope.quests.Count + " tantangan).";

        string namaAsal = (dibaca.Envelope.name ?? string.Empty).Trim();
        if (!string.Equals(namaAsal, hasil.Pack.name, System.StringComparison.Ordinal))
        {
            pesan += "\nNamanya disesuaikan dari \"" + namaAsal
                + "\" karena sudah ada paket dengan nama itu.";
        }

        ShowEditQuestCloudWarningOrSuccess(pesan);
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}
