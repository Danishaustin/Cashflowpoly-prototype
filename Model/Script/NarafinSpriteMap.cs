using System;
using System.Collections.Generic;

// Satu-satunya tempat yang memetakan identitas katalog ruleset server ke nama berkas sprite lokal.
// Server tidak menyimpan aset, jadi gambarnya tetap lokal; yang berubah adalah kuncinya — id/family
// katalog, bukan nama kartu lokal. Dengan begitu berkas data lokal tidak lagi dibutuhkan untuk gambar.
// Nama berkas mengikuti isi Resources/Sprite/ItemSprite/<kategori>/.
public static class NarafinSpriteMap
{
    private static readonly Dictionary<string, string> IngredientSprites = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "nasi_putih", "Nasi" },
        { "sayur", "Sayuran" },
        { "tahu_tempe", "TahuTempe" },
        { "telur", "Telur" },
        { "daging", "Daging" }
    };

    private static readonly Dictionary<string, string> NeedFamilySprites = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "buku", "Buku" },
        { "baju", "Baju" },
        { "tempat_makan", "Makanan" },
        { "sepatu", "Sepatu" },
        { "tas", "Tas" },
        { "sepeda", "Sepeda" },
        { "gadget", "Kamera" },
        { "tempat_pensil", "AlatTulis" },
        { "boneka", "Boneka" },
        { "gameboy", "GameConsole" },
        { "jam", "JamTangan" },
        { "hiburan", "WahanaBermain" }
    };

    private static readonly Dictionary<string, string> FinancialGoalSprites = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "tujuan_25", "BersamaKeluarga" },
        { "tujuan_28", "KebunBinatang" },
        { "tujuan_30", "Tamasya" },
        { "tujuan_32", "Mobil" },
        { "tujuan_35", "Rumah" }
    };

    private static readonly Dictionary<string, string> MissionSprites = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "misi_boneka", "Target Kebutuhan Boneka" },
        { "misi_jam", "Target Kebutuhan Jam Tangan" },
        { "misi_gameboy", "Target Kebutuhan Game Console" },
        { "misi_hiburan", "Target Kebutuhan Wahana Bermain" }
    };

    public static string GetIngredientSpriteName(string ingredientId, string serverName)
    {
        return Resolve(IngredientSprites, ingredientId, serverName);
    }

    public static string GetNeedSpriteName(string family, string serverName)
    {
        return Resolve(NeedFamilySprites, family, serverName);
    }

    public static string GetFinancialGoalSpriteName(string goalId, string serverName)
    {
        return Resolve(FinancialGoalSprites, goalId, serverName);
    }

    public static string GetMissionSpriteName(string missionId, string serverName)
    {
        return Resolve(MissionSprites, missionId, serverName);
    }

    // Id yang belum terdaftar memakai nama dari server tanpa spasi, seperti perilaku sebelumnya,
    // supaya ruleset buatan sendiri tetap bisa menampilkan gambar bila namanya cocok.
    private static string Resolve(Dictionary<string, string> map, string key, string serverName)
    {
        string cleanKey = (key ?? string.Empty).Trim();
        if (cleanKey.Length > 0 && map.TryGetValue(cleanKey, out string spriteName))
        {
            return spriteName;
        }

        return (serverName ?? string.Empty).Replace(" ", string.Empty);
    }
}
