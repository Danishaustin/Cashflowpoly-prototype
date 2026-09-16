using System;
using System.Collections.Generic;

public partial class GameState
{
    // Nama kebutuhan lokal (sprite, prasyarat narasi) yang berbeda dari family kartu ruleset.
    private static readonly Dictionary<string, string> LocalKebutuhanFamilies = new Dictionary<string, string>
    {
        { "Makanan", "tempat_makan" },
        { "AlatTulis", "tempat_pensil" },
        { "Kamera", "gadget" },
        { "GameConsole", "gameboy" },
        { "JamTangan", "jam" },
        { "WahanaBermain", "hiburan" }
    };

    // Kebutuhan dikelompokkan per tipe; isinya card_id ruleset session (atau nama lokal tanpa katalog).
    public void AddKebutuhanToList(int player, string cardIdAtauNama, string tipe)
    {
        Dictionary<string, List<string>> kebutuhan = GetKebutuhanList(player);
        if (!kebutuhan.ContainsKey(tipe))
        {
            kebutuhan[tipe] = new List<string>();
        }

        kebutuhan[tipe].Add(cardIdAtauNama);
    }

    public string GetKebutuhanDisplayName(string cardIdAtauNama)
    {
        NarafinSetupNeed need = NarafinActiveSession.FindNeed(NarafinActiveSession.Catalog, cardIdAtauNama);
        if (need != null && !string.IsNullOrWhiteSpace(need.nama))
        {
            return need.nama;
        }

        return cardIdAtauNama ?? string.Empty;
    }

    public int CountKebutuhanByName(int player, string namaAtauFamily)
    {
        string targetKey = ToKebutuhanFamilyKey(namaAtauFamily);
        if (targetKey.Length == 0)
        {
            return 0;
        }

        int count = 0;
        foreach (List<string> kebutuhanGroup in GetKebutuhanList(player).Values)
        {
            foreach (string kebutuhan in kebutuhanGroup)
            {
                if (MatchesKebutuhan(kebutuhan, targetKey))
                {
                    count++;
                }
            }
        }

        return count;
    }

    public int CountKebutuhanByTipe(int player, string tipe)
    {
        foreach (KeyValuePair<string, List<string>> kebutuhanGroup in GetKebutuhanList(player))
        {
            if (string.Equals(kebutuhanGroup.Key, tipe, StringComparison.OrdinalIgnoreCase))
            {
                return kebutuhanGroup.Value.Count;
            }
        }

        return 0;
    }

    // Kartu yang dijual lewat opsi darurat: kartu terakhir dari tipe tertinggi (tersier, sekunder, lalu primer).
    public string GetKebutuhanUntukDijual(int player)
    {
        Dictionary<string, List<string>> kebutuhan = GetKebutuhanList(player);
        foreach (string tipe in new[] { "tersier", "sekunder", "primer" })
        {
            foreach (KeyValuePair<string, List<string>> kebutuhanGroup in kebutuhan)
            {
                if (string.Equals(kebutuhanGroup.Key, tipe, StringComparison.OrdinalIgnoreCase)
                    && kebutuhanGroup.Value != null
                    && kebutuhanGroup.Value.Count > 0)
                {
                    return kebutuhanGroup.Value[kebutuhanGroup.Value.Count - 1];
                }
            }
        }

        foreach (List<string> kebutuhanGroup in kebutuhan.Values)
        {
            if (kebutuhanGroup != null && kebutuhanGroup.Count > 0)
            {
                return kebutuhanGroup[kebutuhanGroup.Count - 1];
            }
        }

        return string.Empty;
    }

    public bool RemoveKebutuhan(int player, string cardIdAtauNama)
    {
        foreach (List<string> kebutuhanGroup in GetKebutuhanList(player).Values)
        {
            if (kebutuhanGroup != null && kebutuhanGroup.Remove(cardIdAtauNama))
            {
                return true;
            }
        }

        return false;
    }

    // Nama file sprite lokal untuk sebuah family kartu kebutuhan, mis. "gameboy" -> "GameConsole".
    public static string GetKebutuhanSpriteName(string family, string nama)
    {
        string familyKey = NarafinActiveSession.NormalizeName(family);
        foreach (KeyValuePair<string, string> localFamily in LocalKebutuhanFamilies)
        {
            if (NarafinActiveSession.NormalizeName(localFamily.Value) == familyKey)
            {
                return localFamily.Key;
            }
        }

        return (nama ?? string.Empty).Replace(" ", string.Empty);
    }

    private static bool MatchesKebutuhan(string kebutuhan, string targetKey)
    {
        if (ToKebutuhanFamilyKey(kebutuhan) == targetKey)
        {
            return true;
        }

        NarafinSetupNeed need = NarafinActiveSession.FindNeed(NarafinActiveSession.Catalog, kebutuhan);
        return need != null
            && (ToKebutuhanFamilyKey(need.nama) == targetKey
                || ToKebutuhanFamilyKey(NarafinActiveSession.GetNeedFamily(need)) == targetKey);
    }

    // Menyamakan nama lokal, nama kartu, dan family ke satu kunci, mis. "JamTangan", "Jam", "jam" -> "jam".
    private static string ToKebutuhanFamilyKey(string value)
    {
        string key = NarafinActiveSession.NormalizeName(value);
        foreach (KeyValuePair<string, string> localFamily in LocalKebutuhanFamilies)
        {
            if (NarafinActiveSession.NormalizeName(localFamily.Key) == key)
            {
                return NarafinActiveSession.NormalizeName(localFamily.Value);
            }
        }

        return key;
    }
}
