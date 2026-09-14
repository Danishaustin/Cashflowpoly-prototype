using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class NarafinEventCardIdResolver
{
    public static IEnumerable<string> ResolveBahanCardIds(IEnumerable<string> bahanNames)
    {
        List<string> ids = new List<string>();
        if (bahanNames == null)
        {
            return ids;
        }

        foreach (string bahanName in bahanNames)
        {
            ids.Add(ResolveBahanCardId(bahanName));
        }

        return ids;
    }

    public static string ResolveBahanCardId(string bahanName)
    {
        BahanMakananDatabase database = LoadResourceDatabase<BahanMakananDatabase>("Data/bahan");
        if (database != null && database.bahan != null)
        {
            for (int i = 0; i < database.bahan.Count; i++)
            {
                if (database.bahan[i] != null && string.Equals(database.bahan[i].nama, bahanName, StringComparison.OrdinalIgnoreCase))
                {
                    return "ING-" + (i + 1).ToString("000", CultureInfo.InvariantCulture);
                }
            }
        }

        return bahanName;
    }

    public static string ResolveResepCardId(string resepName)
    {
        ResepDatabase database = LoadResourceDatabase<ResepDatabase>("Data/resep");
        if (database != null && database.resep != null)
        {
            for (int i = 0; i < database.resep.Count; i++)
            {
                if (database.resep[i] != null && string.Equals(database.resep[i].nama, resepName, StringComparison.OrdinalIgnoreCase))
                {
                    return "ORD-" + (i + 1).ToString("000", CultureInfo.InvariantCulture);
                }
            }
        }

        return resepName;
    }

    public static string ResolveKebutuhanCardId(string kebutuhanName)
    {
        KebutuhanDatabase database = LoadResourceDatabase<KebutuhanDatabase>("Data/kebutuhan");
        if (database == null || database.kebutuhan == null)
        {
            return kebutuhanName;
        }

        int primerIndex = 0;
        int sekunderIndex = 0;
        int tersierIndex = 0;
        for (int i = 0; i < database.kebutuhan.Count; i++)
        {
            KebutuhanData kebutuhan = database.kebutuhan[i];
            if (kebutuhan == null)
            {
                continue;
            }

            string prefix = "NEED";
            int index;
            if (string.Equals(kebutuhan.tipe, "primer", StringComparison.OrdinalIgnoreCase))
            {
                prefix = "NEED-P";
                index = ++primerIndex;
            }
            else if (string.Equals(kebutuhan.tipe, "sekunder", StringComparison.OrdinalIgnoreCase))
            {
                prefix = "NEED-S";
                index = ++sekunderIndex;
            }
            else if (string.Equals(kebutuhan.tipe, "tersier", StringComparison.OrdinalIgnoreCase))
            {
                prefix = "NEED-T";
                index = ++tersierIndex;
            }
            else
            {
                index = i + 1;
            }

            if (string.Equals(kebutuhan.nama, kebutuhanName, StringComparison.OrdinalIgnoreCase))
            {
                return prefix + "-" + index.ToString("000", CultureInfo.InvariantCulture);
            }
        }

        return kebutuhanName;
    }

    public static string ResolveTujuanFinansialCardId(string tujuanName)
    {
        TujuanFinansialDatabase database = LoadResourceDatabase<TujuanFinansialDatabase>("Data/tujuanFinansial");
        if (database != null && database.tujuanFinansial != null)
        {
            for (int i = 0; i < database.tujuanFinansial.Count; i++)
            {
                if (database.tujuanFinansial[i] != null && string.Equals(database.tujuanFinansial[i].nama, tujuanName, StringComparison.OrdinalIgnoreCase))
                {
                    return "GOAL-" + (i + 1).ToString("000", CultureInfo.InvariantCulture);
                }
            }
        }

        return tujuanName;
    }


    public static string ResolveBahanNameFromCardId(string cardId)
    {
        if (!TryParseIndexedCardId(cardId, "ING", out int index))
        {
            return cardId;
        }

        BahanMakananDatabase database = LoadResourceDatabase<BahanMakananDatabase>("Data/bahan");
        if (database?.bahan == null || index < 1 || index > database.bahan.Count)
        {
            return cardId;
        }

        return database.bahan[index - 1]?.nama ?? cardId;
    }

    public static string ResolveKebutuhanNameFromCardId(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId) || !cardId.StartsWith("NEED-", StringComparison.OrdinalIgnoreCase))
        {
            return cardId;
        }

        string[] parts = cardId.Split('-');
        if (parts.Length < 3 || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int targetIndex))
        {
            return cardId;
        }

        string typeCode = parts[1];
        KebutuhanDatabase database = LoadResourceDatabase<KebutuhanDatabase>("Data/kebutuhan");
        if (database?.kebutuhan == null)
        {
            return cardId;
        }

        int currentIndex = 0;
        foreach (KebutuhanData kebutuhan in database.kebutuhan)
        {
            if (kebutuhan == null || !IsKebutuhanTypeMatch(kebutuhan.tipe, typeCode))
            {
                continue;
            }

            currentIndex++;
            if (currentIndex == targetIndex)
            {
                return kebutuhan.nama ?? cardId;
            }
        }

        return cardId;
    }

    public static string ResolveResepNameFromCardId(string cardId)
    {
        if (!TryParseIndexedCardId(cardId, "ORD", out int index))
        {
            return cardId;
        }

        ResepDatabase database = LoadResourceDatabase<ResepDatabase>("Data/resep");
        if (database?.resep == null || index < 1 || index > database.resep.Count)
        {
            return cardId;
        }

        return database.resep[index - 1]?.nama ?? cardId;
    }

    public static string ResolveTujuanFinansialNameFromCardId(string cardId)
    {
        if (!TryParseIndexedCardId(cardId, "GOAL", out int index))
        {
            return cardId;
        }

        TujuanFinansialDatabase database = LoadResourceDatabase<TujuanFinansialDatabase>("Data/tujuanFinansial");
        if (database?.tujuanFinansial == null || index < 1 || index > database.tujuanFinansial.Count)
        {
            return cardId;
        }

        return database.tujuanFinansial[index - 1]?.nama ?? cardId;
    }

    private static bool TryParseIndexedCardId(string cardId, string prefix, out int index)
    {
        index = 0;
        if (string.IsNullOrWhiteSpace(cardId) || !cardId.StartsWith(prefix + "-", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string number = cardId.Substring(prefix.Length + 1);
        return int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
    }

    private static bool IsKebutuhanTypeMatch(string tipe, string typeCode)
    {
        switch (typeCode?.ToUpperInvariant())
        {
            case "P":
                return string.Equals(tipe, "primer", StringComparison.OrdinalIgnoreCase);
            case "S":
                return string.Equals(tipe, "sekunder", StringComparison.OrdinalIgnoreCase);
            case "T":
                return string.Equals(tipe, "tersier", StringComparison.OrdinalIgnoreCase);
            default:
                return false;
        }
    }

    private static T LoadResourceDatabase<T>(string path) where T : class
    {
        TextAsset asset = Resources.Load<TextAsset>(path);
        if (asset == null || string.IsNullOrWhiteSpace(asset.text))
        {
            return null;
        }

        return JsonUtility.FromJson<T>(asset.text);
    }
}

