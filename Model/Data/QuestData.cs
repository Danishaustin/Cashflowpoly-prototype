using System;
using System.Collections.Generic;

// Quest hanya berisi data statis: nama dan perintah. Satu-satunya yang berubah saat bermain adalah state,
// dan state itu diubah lewat narasi.
[Serializable]
public class QuestData
{
    public string id;
    public string nama;
    public string perintah;
}

[Serializable]
public class QuestDatabase
{
    public List<QuestData> quest;
}

[Serializable]
public class QuestPackData
{
    public string id;
    public string name;
    public string file;
}

[Serializable]
public class QuestManifestData
{
    public List<QuestPackData> questPacks;
}

public static class QuestState
{
    public const string BelumAktif = "BELUM_AKTIF";
    public const string Aktif = "AKTIF";
    public const string Selesai = "SELESAI";
    public const string Gagal = "GAGAL";

    public static readonly string[] All = { BelumAktif, Aktif, Selesai, Gagal };

    public static string Normalize(string state)
    {
        switch ((state ?? string.Empty).Trim().ToUpperInvariant())
        {
            case Aktif:
                return Aktif;
            case Selesai:
                return Selesai;
            case Gagal:
                return Gagal;
            default:
                return BelumAktif;
        }
    }

    public static string GetLabel(string state)
    {
        switch (Normalize(state))
        {
            case Aktif:
                return "Aktif";
            case Selesai:
                return "Selesai";
            case Gagal:
                return "Gagal";
            default:
                return "Belum aktif";
        }
    }
}
