using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// Amplop pertukaran paket tantangan. Kembarannya untuk narasi ada di NarasiPackTransfer, dan
// keduanya sengaja berdiri sendiri, bukan disatukan lewat generik.
//
// Alasannya bukan malas: kontrak backend memang memisahkan keduanya (narrative-packs dengan
// "dialogs"/"dialog_code" dan quest-packs dengan "quests"/"quest_code"), isi payload-nya berbeda
// jenis, dan JsonUtility tidak bisa membentuk tipe generik. Menyatukannya akan menuntut lapisan
// abstraksi yang lebih besar daripada duplikasinya sendiri.
//
// Satu beda nyata dari narasi: paket tantangan tidak merujuk apa pun di luar dirinya. Narasi yang
// menunjuk balik ke tantangan, bukan sebaliknya. Jadi di sini tidak ada daftar rujukan yang perlu
// diperingatkan saat impor.
public static class QuestPackTransfer
{
    public const string FormatId = "cashflowpoly.quest-pack";
    public const int FormatVersion = 1;

    // Batas dari kolom backend: quest_packs.name varchar(120), quests.quest_code varchar(120).
    public const int MaxPackNameLength = 120;
    public const int MaxQuestCodeLength = 120;

    public static string BuildFileName(string packName)
    {
        string slug = NarasiPackRepository.ToFileNameSlug(packName);
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "tantangan";
        }

        return slug + "-" + DateTime.Now.ToString("yyyyMMdd") + ".tantangan.json";
    }

    // ---------------------------------------------------------------- membentuk
    public static string BuildEnvelopeJson(QuestPackData pack, QuestDatabase database, string penulis)
    {
        QuestTransferEnvelope envelope = new QuestTransferEnvelope
        {
            format = FormatId,
            format_version = FormatVersion,
            name = pack != null ? (pack.name ?? string.Empty).Trim() : string.Empty,
            penulis = (penulis ?? string.Empty).Trim(),
            diekspor_pada = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
            quests = new List<QuestTransferQuest>()
        };

        List<QuestData> quests = database != null && database.quest != null
            ? database.quest
            : new List<QuestData>();

        // id lokal pack dan nama berkasnya tidak ikut; keduanya hanya berarti di satu perangkat.
        foreach (QuestData quest in quests)
        {
            if (quest == null)
            {
                continue;
            }

            envelope.quests.Add(new QuestTransferQuest
            {
                quest_code = quest.id ?? string.Empty,
                sort_order = envelope.quests.Count + 1,
                payload = quest
            });
        }

        return JsonUtility.ToJson(envelope, true);
    }

    // ---------------------------------------------------------------- memeriksa
    public static QuestTransferReadResult Read(string json)
    {
        QuestTransferReadResult result = new QuestTransferReadResult();

        if (string.IsNullOrWhiteSpace(json))
        {
            result.ErrorMessage = "Berkas kosong.";
            return result;
        }

        QuestTransferEnvelope envelope;
        try
        {
            envelope = JsonUtility.FromJson<QuestTransferEnvelope>(json);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = "Berkas ini bukan JSON yang sah. Detail: " + ex.Message;
            return result;
        }

        if (envelope == null)
        {
            result.ErrorMessage = "Berkas ini bukan berkas paket tantangan.";
            return result;
        }

        // Berkas paket narasi dikenali dan ditolak dengan sebutannya sendiri, karena keduanya sama-sama
        // .json dan sangat mungkin tertukar saat dipilih dari daftar berkas.
        if (string.Equals(envelope.format, NarasiPackTransfer.FormatId, StringComparison.Ordinal))
        {
            result.ErrorMessage = "Berkas ini paket narasi, bukan paket tantangan. "
                + "Imporlah dari layar Pilih Narasi.";
            return result;
        }

        if (!string.Equals(envelope.format, FormatId, StringComparison.Ordinal))
        {
            result.ErrorMessage = "Berkas ini bukan berkas paket tantangan Cashflowpoly.";
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

        if (!QuestPackRepository.IsValidPackName(nama))
        {
            result.ErrorMessage = "Nama paket \"" + nama
                + "\" memuat karakter yang tidak diizinkan. Hanya huruf, angka, spasi, garis bawah, dan strip.";
            return result;
        }

        if (envelope.quests == null || envelope.quests.Count == 0)
        {
            result.ErrorMessage = "Berkas ini tidak memuat satu pun tantangan.";
            return result;
        }

        HashSet<string> kode = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < envelope.quests.Count; i++)
        {
            QuestTransferQuest quest = envelope.quests[i];
            string urutanUntukPesan = "Tantangan ke-" + (i + 1);

            if (quest == null)
            {
                result.ErrorMessage = urutanUntukPesan + " kosong.";
                return result;
            }

            string code = (quest.quest_code ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                result.ErrorMessage = urutanUntukPesan + " tidak punya kode tantangan.";
                return result;
            }

            if (code.Length > MaxQuestCodeLength)
            {
                result.ErrorMessage = "Kode tantangan \"" + code + "\" melebihi " + MaxQuestCodeLength + " karakter.";
                return result;
            }

            if (!kode.Add(code))
            {
                result.ErrorMessage = "Kode tantangan \"" + code + "\" muncul lebih dari sekali di dalam berkas.";
                return result;
            }

            if (quest.payload == null)
            {
                result.ErrorMessage = "Tantangan \"" + code + "\" tidak punya isi.";
                return result;
            }
        }

        result.Success = true;
        result.Envelope = envelope;
        return result;
    }

    // ---------------------------------------------------------------- memasang
    public static async Task<QuestPackCreateResult> InstallAsync(QuestTransferEnvelope envelope)
    {
        QuestPackCreateResult result = new QuestPackCreateResult();

        if (envelope == null || envelope.quests == null)
        {
            result.ErrorMessage = "Tidak ada yang bisa dipasang.";
            return result;
        }

        QuestPackCreateResult dibuat = await QuestPackRepository.CreateNewPackAsync();
        if (!dibuat.Success || dibuat.Pack == null)
        {
            return dibuat;
        }

        QuestPackData pack = dibuat.Pack;

        string namaTujuan = await BuatNamaBelumTerpakaiAsync((envelope.name ?? string.Empty).Trim());
        QuestPackOperationResult ganti = await QuestPackRepository.UpdatePackNameAsync(pack.id, namaTujuan);
        if (!ganti.Success)
        {
            result.ErrorMessage = ganti.ErrorMessage;
            return result;
        }

        pack.name = namaTujuan;

        List<QuestTransferQuest> urut = new List<QuestTransferQuest>(envelope.quests);
        urut.Sort((a, b) => a.sort_order.CompareTo(b.sort_order));

        QuestDatabase database = new QuestDatabase { quest = new List<QuestData>() };
        foreach (QuestTransferQuest quest in urut)
        {
            // quest_code yang mengikat; id di dalam payload disamakan dengannya supaya keduanya tidak
            // bisa berbeda setelah berkas disunting tangan. Narasi menunjuk tantangan lewat kode ini,
            // jadi perbedaan sekecil apa pun di sini memutus rujukannya tanpa galat.
            quest.payload.id = (quest.quest_code ?? string.Empty).Trim();
            database.quest.Add(quest.payload);
        }

        try
        {
            await QuestPackRepository.SaveQuestDatabaseAsync(pack, database, true);
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

    private static async Task<string> BuatNamaBelumTerpakaiAsync(string namaAsal)
    {
        QuestManifestData manifest = await QuestPackRepository.LoadManifestAsync();
        HashSet<string> terpakai = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (manifest != null && manifest.questPacks != null)
        {
            foreach (QuestPackData p in manifest.questPacks)
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
}

// Nama field snake_case mengikuti kontrak backend quest-packs, bukan kebiasaan C#.
[Serializable]
public class QuestTransferEnvelope
{
    public string format;
    public int format_version;
    public string name;
    public string penulis;
    public string diekspor_pada;
    public List<QuestTransferQuest> quests;
}

[Serializable]
public class QuestTransferQuest
{
    public string quest_code;
    public int sort_order;
    public QuestData payload;
}

public class QuestTransferReadResult
{
    public bool Success;
    public string ErrorMessage;
    public QuestTransferEnvelope Envelope;
}
