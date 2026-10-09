using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// Amplop pertukaran paket narasi: membentuk, memeriksa, dan memasang.
//
// Kelas ini sengaja tidak tahu apa pun tentang berkas, Android, maupun UI. Masukannya string JSON,
// keluarannya paket terpasang atau galat bertipe. Itu bukan kerapian belaka: saat API pack narasi
// sudah ada, "ambil pack dari server lalu pasang" adalah jalur yang sama persis, hanya sumber
// stringnya yang berganti dari berkas menjadi HTTP. Pemeriksa dan pemasang di sini dipakai utuh.
//
// Bentuk amplopnya meniru kontrak backend PUT /api/v1/narrative-packs/{packId}/dialogs, yang memang
// ditulis untuk klien game yang menyimpan pack sebagai satu berkas. Karena itu nama fieldnya
// snake_case dan melanggar kebiasaan C#: JsonUtility memakai nama field apa adanya, dan menyamakan
// bentuknya sekarang jauh lebih murah daripada menulis pengubah bentuk nanti.
public static class NarasiPackTransfer
{
    // Penanda jenis berkas. Berkas JSON lain yang kebetulan dipilih instruktur akan tertolak di sini
    // dengan pesan yang jelas, bukan terbaca separuh lalu memasang paket kosong.
    public const string FormatId = "cashflowpoly.narasi-pack";

    // Dinaikkan setiap bentuk amplop berubah. Versi yang tidak dikenal ditolak, tidak dimigrasikan:
    // selama masih prototipe, data lama memang boleh dianggap tidak berlaku.
    public const int FormatVersion = 1;

    // Batas dari kolom backend: narrative_packs.name varchar(120), narrative_dialogs.dialog_code
    // varchar(120). Ditegakkan sejak di klien supaya berkas yang lolos di sini pasti lolos di server.
    public const int MaxPackNameLength = 120;
    public const int MaxDialogCodeLength = 120;

    public static string BuildFileName(string packName)
    {
        string slug = NarasiPackRepository.ToFileNameSlug(packName);
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "narasi";
        }

        return slug + "-" + DateTime.Now.ToString("yyyyMMdd") + ".narasi.json";
    }

    // ---------------------------------------------------------------- membentuk
    public static string BuildEnvelopeJson(NarasiPackData pack, DialogKarakterDatabase database, string penulis)
    {
        NarasiTransferEnvelope envelope = new NarasiTransferEnvelope
        {
            format = FormatId,
            format_version = FormatVersion,
            name = pack != null ? (pack.name ?? string.Empty).Trim() : string.Empty,
            penulis = (penulis ?? string.Empty).Trim(),
            diekspor_pada = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
            dialogs = new List<NarasiTransferDialog>()
        };

        List<DialogKarakterData> dialogs = database != null && database.dialogKarakter != null
            ? database.dialogKarakter
            : new List<DialogKarakterData>();

        // id lokal pack dan nama berkasnya SENGAJA tidak ikut. Keduanya hanya berarti di satu
        // perangkat, dan backend memakai uuid miliknya sendiri. Yang dibawa cukup nama pack dan kode
        // tiap dialog, yang sama artinya di mana pun.
        for (int i = 0; i < dialogs.Count; i++)
        {
            DialogKarakterData dialog = dialogs[i];
            if (dialog == null)
            {
                continue;
            }

            envelope.dialogs.Add(new NarasiTransferDialog
            {
                dialog_code = dialog.id ?? string.Empty,
                sort_order = envelope.dialogs.Count + 1,
                payload = dialog
            });
        }

        return JsonUtility.ToJson(envelope, true);
    }

    // ---------------------------------------------------------------- memeriksa
    //
    // JsonUtility tidak melempar galat untuk field yang tidak cocok; ia hanya meninggalkannya null
    // atau nol. Jadi pemeriksaan di bawah tidak boleh bersandar pada pengecualian, dan harus
    // memeriksa tiap bagian yang wajib ada satu per satu.
    public static NarasiTransferReadResult Read(string json)
    {
        NarasiTransferReadResult result = new NarasiTransferReadResult();

        if (string.IsNullOrWhiteSpace(json))
        {
            result.ErrorMessage = "Berkas kosong.";
            return result;
        }

        NarasiTransferEnvelope envelope;
        try
        {
            envelope = JsonUtility.FromJson<NarasiTransferEnvelope>(json);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = "Berkas ini bukan JSON yang sah. Detail: " + ex.Message;
            return result;
        }

        if (envelope == null)
        {
            result.ErrorMessage = "Berkas ini bukan berkas paket narasi.";
            return result;
        }

        // Berkas paket tantangan dikenali dan ditolak dengan sebutannya sendiri, karena keduanya
        // sama-sama .json dan sangat mungkin tertukar saat dipilih dari daftar berkas.
        if (string.Equals(envelope.format, QuestPackTransfer.FormatId, StringComparison.Ordinal))
        {
            result.ErrorMessage = "Berkas ini paket tantangan, bukan paket narasi. "
                + "Imporlah dari layar Pilih Tantangan.";
            return result;
        }

        if (!string.Equals(envelope.format, FormatId, StringComparison.Ordinal))
        {
            result.ErrorMessage = "Berkas ini bukan berkas paket narasi Cashflowpoly.";
            return result;
        }

        if (envelope.format_version != FormatVersion)
        {
            result.ErrorMessage = "Versi format berkas " + envelope.format_version
                + " tidak didukung versi aplikasi ini (versi " + FormatVersion + ").";
            return result;
        }

        string nama = (envelope.name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nama))
        {
            result.ErrorMessage = "Nama paket di dalam berkas kosong.";
            return result;
        }

        if (nama.Length > MaxPackNameLength)
        {
            result.ErrorMessage = "Nama paket di dalam berkas melebihi " + MaxPackNameLength + " karakter.";
            return result;
        }

        if (!NarasiPackRepository.IsValidPackName(nama))
        {
            result.ErrorMessage = "Nama paket \"" + nama
                + "\" memuat karakter yang tidak diizinkan. Hanya huruf, angka, spasi, garis bawah, dan strip.";
            return result;
        }

        if (envelope.dialogs == null || envelope.dialogs.Count == 0)
        {
            result.ErrorMessage = "Berkas ini tidak memuat satu pun dialog.";
            return result;
        }

        // Kode ganda ditolak, sama seperti server menolaknya 400 dan menyebut kodenya. Lebih baik
        // ketahuan di sini daripada memasang paket yang nanti gagal disinkronkan.
        HashSet<string> kode = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> questDirujuk = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < envelope.dialogs.Count; i++)
        {
            NarasiTransferDialog dialog = envelope.dialogs[i];
            string urutanUntukPesan = "Dialog ke-" + (i + 1);

            if (dialog == null)
            {
                result.ErrorMessage = urutanUntukPesan + " kosong.";
                return result;
            }

            string code = (dialog.dialog_code ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                result.ErrorMessage = urutanUntukPesan + " tidak punya kode dialog.";
                return result;
            }

            if (code.Length > MaxDialogCodeLength)
            {
                result.ErrorMessage = "Kode dialog \"" + code + "\" melebihi " + MaxDialogCodeLength + " karakter.";
                return result;
            }

            if (!kode.Add(code))
            {
                result.ErrorMessage = "Kode dialog \"" + code + "\" muncul lebih dari sekali di dalam berkas.";
                return result;
            }

            if (dialog.payload == null)
            {
                result.ErrorMessage = "Dialog \"" + code + "\" tidak punya isi.";
                return result;
            }

            KumpulkanQuestDirujuk(dialog.payload, questDirujuk);
        }

        result.Success = true;
        result.Envelope = envelope;
        result.QuestIdsDirujuk = new List<string>(questDirujuk);
        result.QuestIdsDirujuk.Sort(StringComparer.Ordinal);
        return result;
    }

    // Dialog boleh menuntut sebuah quest berada pada state tertentu, dan boleh mengubah state quest
    // setelah diputar. Rujukan itu memakai kode quest, bukan isinya, dan pack quest dipilih terpisah
    // dari pack narasi. Jadi paket narasi yang pindah perangkat bisa saja merujuk quest yang tidak
    // dimiliki penerimanya -- prasyaratnya lalu tidak pernah terpenuhi, diam-diam. Daftar ini
    // dikumpulkan supaya penerimanya bisa diberi tahu, bukan supaya impornya digagalkan.
    private static void KumpulkanQuestDirujuk(DialogKarakterData dialog, HashSet<string> keluaran)
    {
        if (!string.IsNullOrWhiteSpace(dialog.questId))
        {
            keluaran.Add(dialog.questId.Trim());
        }

        if (dialog.prerequisite == null)
        {
            return;
        }

        foreach (DialogPrerequisiteData syarat in dialog.prerequisite)
        {
            if (syarat != null && !string.IsNullOrWhiteSpace(syarat.questId))
            {
                keluaran.Add(syarat.questId.Trim());
            }
        }
    }

    // ---------------------------------------------------------------- memasang
    //
    // Selalu menjadi paket BARU milik sendiri, tidak pernah menimpa yang sudah ada. Ini semantik yang
    // sama dengan POST /narrative-packs/{packId}/adopt di kontrak backend, dan juga satu-satunya
    // pilihan yang aman: berkas yang datang tidak membawa riwayat, jadi tidak ada cara mengetahui
    // apakah ia lebih baru daripada paket bernama sama yang sudah dimiliki.
    public static async Task<NarasiPackCreateResult> InstallAsync(NarasiTransferEnvelope envelope)
    {
        NarasiPackCreateResult result = new NarasiPackCreateResult();

        if (envelope == null || envelope.dialogs == null)
        {
            result.ErrorMessage = "Tidak ada yang bisa dipasang.";
            return result;
        }

        NarasiPackCreateResult dibuat = await NarasiPackRepository.CreateNewPackAsync();
        if (!dibuat.Success || dibuat.Pack == null)
        {
            return dibuat;
        }

        NarasiPackData pack = dibuat.Pack;

        string namaTujuan = await BuatNamaBelumTerpakaiAsync((envelope.name ?? string.Empty).Trim());
        NarasiPackOperationResult ganti = await NarasiPackRepository.UpdatePackNameAsync(pack.id, namaTujuan);
        if (!ganti.Success)
        {
            result.ErrorMessage = ganti.ErrorMessage;
            return result;
        }

        pack.name = namaTujuan;

        List<NarasiTransferDialog> urut = new List<NarasiTransferDialog>(envelope.dialogs);
        urut.Sort((a, b) => a.sort_order.CompareTo(b.sort_order));

        DialogKarakterDatabase database = new DialogKarakterDatabase
        {
            dialogKarakter = new List<DialogKarakterData>()
        };

        foreach (NarasiTransferDialog dialog in urut)
        {
            // dialog_code adalah yang mengikat; id di dalam payload disamakan dengannya supaya
            // keduanya tidak bisa berbeda setelah berkas disunting tangan.
            dialog.payload.id = (dialog.dialog_code ?? string.Empty).Trim();
            database.dialogKarakter.Add(dialog.payload);
        }

        try
        {
            await NarasiPackRepository.SaveDialogDatabaseAsync(pack, database, true);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = "Paket dibuat tetapi isinya gagal disimpan. Detail: " + ex.Message;
            return result;
        }

        result.Success = true;
        result.Pack = pack;
        return result;
    }

    // Nama pack unik per pemilik di backend (uq_narrative_packs_owner_name). Lokal belum
    // menegakkannya, tetapi kalau dibiarkan ganda sekarang, konflik itu baru meledak saat
    // penyelarasan ke server nanti. Lebih murah dicegah di sini.
    private static async Task<string> BuatNamaBelumTerpakaiAsync(string namaAsal)
    {
        NarasiManifestData manifest = await NarasiPackRepository.LoadManifestAsync();
        HashSet<string> terpakai = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (manifest != null && manifest.narasiPacks != null)
        {
            foreach (NarasiPackData p in manifest.narasiPacks)
            {
                if (p != null && !string.IsNullOrWhiteSpace(p.name))
                {
                    terpakai.Add(p.name.Trim());
                }
            }
        }

        if (!terpakai.Contains(namaAsal))
        {
            return Potong(namaAsal);
        }

        // Akhiran memakai strip dan spasi saja: IsValidPackName menolak tanda kurung, jadi
        // "(salinan)" justru akan membuat penggantian nama gagal.
        for (int n = 2; n < 1000; n++)
        {
            string calon = Potong(namaAsal + " - salinan " + n);
            if (!terpakai.Contains(calon))
            {
                return calon;
            }
        }

        return Potong(namaAsal + " - salinan");
    }

    private static string Potong(string nama)
    {
        return nama.Length <= MaxPackNameLength ? nama : nama.Substring(0, MaxPackNameLength).TrimEnd();
    }

    public static byte[] ToBytes(string json)
    {
        return new UTF8Encoding(false).GetBytes(json ?? string.Empty);
    }
}

// Nama field snake_case mengikuti kontrak backend, bukan kebiasaan C#; lihat catatan di kelas atas.
[Serializable]
public class NarasiTransferEnvelope
{
    public string format;
    public int format_version;
    public string name;
    public string penulis;
    public string diekspor_pada;
    public List<NarasiTransferDialog> dialogs;
}

[Serializable]
public class NarasiTransferDialog
{
    public string dialog_code;
    public int sort_order;
    public DialogKarakterData payload;
}

public class NarasiTransferReadResult
{
    public bool Success;
    public string ErrorMessage;
    public NarasiTransferEnvelope Envelope;

    // Kode quest yang dirujuk isi paket ini. Bukan galat, melainkan bahan peringatan bagi penerima.
    public List<string> QuestIdsDirujuk = new List<string>();
}
