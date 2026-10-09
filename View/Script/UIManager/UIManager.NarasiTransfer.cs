using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManager
{
    // Tombol Ekspor dan Impor di layar Pilih Narasi. Ditaruh di sini, bukan di layar penyuntingan,
    // karena keduanya bekerja pada paket: ekspor membagikan paket yang dipilih, impor menambah paket
    // baru ke daftar yang sama.
    //
    // Ekspor dipisah dari impor sampai ke tingkat metode. Saat penyuntingan narasi nanti pindah
    // sepenuhnya ke dasbor web, ekspor tidak punya guna lagi dan cukup dicabut dengan menghapus satu
    // metode dan satu tombol; impor tetap tinggal karena ia jalur yang sama dipakai untuk memasang
    // paket yang datang dari server.

    private void RegisterNarasiTransferCallbacks()
    {
        editNarasiPackExportButton?.RegisterCallback<ClickEvent>(evt => { _ = EksporNarasiPackAsync(); });
        editNarasiPackImportButton?.RegisterCallback<ClickEvent>(evt => MulaiImporNarasiPack());
    }

    // ---------------------------------------------------------------- ekspor
    private async Task EksporNarasiPackAsync()
    {
        if (isEditNarasiLoading)
        {
            return;
        }

        string selectedOption = editNarasiPackDropdown?.value;
        if (string.IsNullOrWhiteSpace(selectedOption)
            || !editNarasiPackLookup.TryGetValue(selectedOption, out NarasiPackData selectedPack))
        {
            ShowErrorPopup("Pilih paket narasi yang ingin dibagikan.");
            return;
        }

        BeginEditNarasiLoading("Menyiapkan berkas paket...");

        DialogKarakterDatabase database;
        try
        {
            database = await NarasiPackRepository.LoadDialogDatabaseAsync(selectedPack);
        }
        catch (System.Exception ex)
        {
            EndEditNarasiLoading();
            ShowErrorPopup("Gagal membaca isi paket. Detail: " + ex.Message);
            return;
        }

        EndEditNarasiLoading();

        if (database == null || database.dialogKarakter == null || database.dialogKarakter.Count == 0)
        {
            ShowErrorPopup("Paket \"" + selectedPack.name + "\" belum punya dialog, jadi tidak ada yang bisa dibagikan.");
            return;
        }

        // Nama penulis diambil dari sesi login yang sedang berjalan; amplop ikut membawanya supaya
        // penerima tahu paket itu datang dari siapa.
        string penulis = PlayerPrefs.GetString(NarafinPlayerPrefs.UsernameKey, string.Empty);
        PackTransferIOResult hasil = PackTransferIO.Ekspor(
            NarasiPackTransfer.BuildEnvelopeJson(selectedPack, database, penulis),
            NarasiPackTransfer.BuildFileName(selectedPack.name),
            "Paket narasi " + selectedPack.name);

        if (!hasil.Success)
        {
            ShowErrorPopup(hasil.ErrorMessage);
            return;
        }

        ShowSuccessPopup(hasil.Keterangan);
    }

    // ---------------------------------------------------------------- impor
    private void MulaiImporNarasiPack()
    {
        if (isEditNarasiLoading || PackTransferIO.IsImporSedangBerjalan)
        {
            return;
        }

        PackTransferIO.PilihBerkas(hasil =>
        {
            // Pembatalan bukan kegagalan, jadi tidak ada popup merah.
            if (hasil.Dibatalkan)
            {
                return;
            }

            if (!hasil.Success)
            {
                ShowErrorPopup(hasil.ErrorMessage);
                return;
            }

            _ = PasangNarasiPackAsync(hasil.Json);
        });
    }

    private async Task PasangNarasiPackAsync(string json)
    {
        NarasiTransferReadResult dibaca = NarasiPackTransfer.Read(json);
        if (!dibaca.Success)
        {
            ShowErrorPopup(dibaca.ErrorMessage);
            return;
        }

        BeginEditNarasiLoading("Memasang paket narasi...");

        NarasiPackCreateResult hasil = await NarasiPackTransfer.InstallAsync(dibaca.Envelope);

        EndEditNarasiLoading();

        if (!hasil.Success || hasil.Pack == null)
        {
            ShowErrorPopup(hasil.ErrorMessage);
            return;
        }

        editNarasiActivePack = hasil.Pack;
        await SetupEditNarasiPackSelectionAsync();

        ShowEditNarasiCloudWarningOrSuccess(BuatPesanHasilImpor(hasil.Pack, dibaca));
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    private string BuatPesanHasilImpor(NarasiPackData pack, NarasiTransferReadResult dibaca)
    {
        List<string> baris = new List<string>
        {
            "Paket \"" + pack.name + "\" berhasil dipasang ("
                + dibaca.Envelope.dialogs.Count + " dialog)."
        };

        // Nama bisa berubah saat dipasang bila sudah ada paket bernama sama. Lebih baik dikatakan
        // daripada instruktur mencari nama yang ia harapkan dan tidak menemukannya.
        string namaAsal = (dibaca.Envelope.name ?? string.Empty).Trim();
        if (!string.Equals(namaAsal, pack.name, System.StringComparison.Ordinal))
        {
            baris.Add("Namanya disesuaikan dari \"" + namaAsal + "\" karena sudah ada paket dengan nama itu.");
        }

        // Prasyarat dan efek quest menunjuk tantangan lewat kodenya, dan paket tantangan dipilih
        // terpisah dari paket narasi. Kalau kode itu tidak ada di paket tantangan penerimanya,
        // dialognya tidak akan pernah muncul -- tanpa galat apa pun. Karena itu disebutkan.
        if (dibaca.QuestIdsDirujuk != null && dibaca.QuestIdsDirujuk.Count > 0)
        {
            baris.Add("Paket ini merujuk " + dibaca.QuestIdsDirujuk.Count
                + " tantangan: " + string.Join(", ", dibaca.QuestIdsDirujuk)
                + ". Pastikan paket tantangan yang memuatnya ikut dipilih saat bermain.");
        }

        return string.Join("\n", baris);
    }
}
