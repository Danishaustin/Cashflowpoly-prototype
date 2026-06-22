using System;
using System.Collections.Generic;
using System.Linq;

public partial class NarasiController
{
    // Selects the most specific narasi whose action prerequisites are met.
    private NarasiData GetNarasiByPrerequisite(string aksi, int aksiKe)
    {
        var matchingNarasi = narasiList
            .Where(narasi => HasTriggerPrerequisite(narasi, aksi, aksiKe))
            .Where(narasi => IsPrerequisiteMet(narasi))
            .OrderByDescending(CountValidPrerequisites)
            .FirstOrDefault();

        if (matchingNarasi != null)
        {
            return matchingNarasi;
        }

        return narasiList
            .Where(narasi => HasTriggerPrerequisite(narasi, aksi, 0))
            .Where(narasi => IsPrerequisiteMet(narasi))
            .OrderByDescending(CountValidPrerequisites)
            .FirstOrDefault();
    }

    private DialogKarakterData GetDialogKarakterByPrerequisite(string aksi, int aksiKe)
    {
        int player = GameState.Instance != null ? GameState.Instance.turn : 0;

        var matchingDialog = dialogKarakterList
            .Where(dialogKarakter => !HasPlayedDialog(dialogKarakter.id, player))
            .Where(dialogKarakter => HasTriggerPrerequisite(dialogKarakter, aksi, aksiKe))
            .Where(IsPrerequisiteMet)
            .OrderByDescending(CountValidPrerequisites)
            .FirstOrDefault();

        if (matchingDialog != null)
        {
            return matchingDialog;
        }

        return dialogKarakterList
            .Where(dialogKarakter => !HasPlayedDialog(dialogKarakter.id, player))
            .Where(dialogKarakter => HasTriggerPrerequisite(dialogKarakter, aksi, 0))
            .Where(IsPrerequisiteMet)
            .OrderByDescending(CountValidPrerequisites)
            .FirstOrDefault();
    }

    private bool HasTriggerPrerequisite(NarasiData narasi, string aksi, int value)
    {
        if (GameState.Instance == null || narasi.prerequisiteAksi == null)
        {
            return false;
        }

        string normalizedAksi = GameState.Instance.NormalizeActionName(aksi);
        return narasi.prerequisiteAksi.Any(prerequisite =>
            prerequisite != null
            && GameState.Instance.NormalizeActionName(prerequisite.aksi) == normalizedAksi
            && prerequisite.value == value);
    }

    private bool HasTriggerPrerequisite(DialogKarakterData dialogKarakter, string aksi, int value)
    {
        if (GameState.Instance == null)
        {
            return false;
        }

        string normalizedAksi = GameState.Instance.NormalizeActionName(aksi);
        return GetDialogPrerequisites(dialogKarakter).Any(prerequisite =>
            prerequisite != null
            && GameState.Instance.NormalizeActionName(prerequisite.aksi) == normalizedAksi
            && GetAksiValue(prerequisite) == value);
    }

    private bool IsPrerequisiteMet(NarasiData narasi)
    {
        if (GameState.Instance == null)
        {
            return false;
        }

        var prerequisites = GetPrerequisites(narasi);
        if (prerequisites.Count == 0)
        {
            return true;
        }

        foreach (var prerequisite in prerequisites)
        {
            if (GameState.Instance.GetActionCount(prerequisite.aksi) < prerequisite.value)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsPrerequisiteMet(DialogKarakterData dialogKarakter)
    {
        if (GameState.Instance == null)
        {
            return false;
        }

        var prerequisites = GetDialogPrerequisites(dialogKarakter);
        if (prerequisites.Count == 0)
        {
            return true;
        }

        foreach (var prerequisite in prerequisites)
        {
            if (!IsDialogPrerequisiteMet(prerequisite))
            {
                return false;
            }
        }

        return true;
    }

    private int CountValidPrerequisites(NarasiData narasi)
    {
        return GetPrerequisites(narasi).Count;
    }

    private int CountValidPrerequisites(DialogKarakterData dialogKarakter)
    {
        return GetDialogPrerequisites(dialogKarakter).Count;
    }

    private List<PrerequisiteAksiData> GetPrerequisites(NarasiData narasi)
    {
        if (narasi.prerequisiteAksi == null)
        {
            return new List<PrerequisiteAksiData>();
        }

        return narasi.prerequisiteAksi
            .Where(prerequisite => prerequisite != null && !string.IsNullOrWhiteSpace(prerequisite.aksi))
            .ToList();
    }

    private List<PrerequisiteAksiData> GetPrerequisites(DialogKarakterData dialogKarakter)
    {
        if (dialogKarakter.prerequisiteAksi == null)
        {
            return new List<PrerequisiteAksiData>();
        }

        return dialogKarakter.prerequisiteAksi
            .Where(prerequisite => prerequisite != null && !string.IsNullOrWhiteSpace(prerequisite.aksi))
            .ToList();
    }

    private List<DialogPrerequisiteData> GetDialogPrerequisites(DialogKarakterData dialogKarakter)
    {
        var prerequisites = new List<DialogPrerequisiteData>();

        if (dialogKarakter.prerequisite != null)
        {
            prerequisites.AddRange(dialogKarakter.prerequisite.Where(IsActiveDialogPrerequisite));
        }

        if (dialogKarakter.prerequisiteAksi != null)
        {
            prerequisites.AddRange(dialogKarakter.prerequisiteAksi
                .Where(prerequisite => prerequisite != null && !string.IsNullOrWhiteSpace(prerequisite.aksi))
                .Select(prerequisite => new DialogPrerequisiteData
                {
                    aksi = prerequisite.aksi,
                    aksiValue = prerequisite.value
                }));
        }

        return prerequisites;
    }

    private bool IsActiveDialogPrerequisite(DialogPrerequisiteData prerequisite)
    {
        return prerequisite != null
            && (!string.IsNullOrWhiteSpace(prerequisite.aksi)
                || prerequisite.uang > 0
                || prerequisite.kebahagiaan > 0
                || prerequisite.tabungan > 0
                || prerequisite.emas > 0
                || prerequisite.kartuPinjaman > 0
                || prerequisite.asuransiDimiliki
                || HasItems(prerequisite.bahanDimiliki)
                || HasItems(prerequisite.kebutuhanDimiliki)
                || HasItems(prerequisite.tujuanFinansialDimiliki)
                || HasItems(prerequisite.masakanDijual)
                || !string.IsNullOrWhiteSpace(prerequisite.stat)
                || !string.IsNullOrWhiteSpace(prerequisite.bahan)
                || !string.IsNullOrWhiteSpace(prerequisite.kebutuhan)
                || !string.IsNullOrWhiteSpace(prerequisite.tipeKebutuhan)
                || !string.IsNullOrWhiteSpace(prerequisite.tujuanFinansial)
                || !string.IsNullOrWhiteSpace(prerequisite.kondisi)
                || prerequisite.hariKe > 0
                || prerequisite.hariDalamMinggu > 0
                || prerequisite.mingguKe > 0
                || prerequisite.turnKe > 0
                || prerequisite.playerCount > 0);
    }

    private bool IsDialogPrerequisiteMet(DialogPrerequisiteData prerequisite)
    {
        if (GameState.Instance == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(prerequisite.aksi)
            && GameState.Instance.GetActionCount(prerequisite.aksi) < GetAksiValue(prerequisite))
        {
            return false;
        }

        if (prerequisite.uang > 0 && GameState.Instance.Coins < prerequisite.uang)
        {
            return false;
        }

        if (prerequisite.kebahagiaan > 0 && GameState.Instance.Happiness < prerequisite.kebahagiaan)
        {
            return false;
        }

        if (prerequisite.tabungan > 0 && GameState.Instance.Saving < prerequisite.tabungan)
        {
            return false;
        }

        if (prerequisite.emas > 0 && GameState.Instance.Emas < prerequisite.emas)
        {
            return false;
        }

        if (prerequisite.kartuPinjaman > 0 && GameState.Instance.PinjamanSyariahCards < prerequisite.kartuPinjaman)
        {
            return false;
        }

        if (prerequisite.mingguKe > 0 && GetMingguKe() != prerequisite.mingguKe)
        {
            return false;
        }

        if (prerequisite.hariKe > 0 && GetHariDalamMinggu() != prerequisite.hariKe)
        {
            return false;
        }

        if (prerequisite.asuransiDimiliki && !GameState.Instance.GetAsuransiDimiliki(GameState.Instance.turn))
        {
            return false;
        }

        if (HasItems(prerequisite.bahanDimiliki) && !HasAllBahan(prerequisite.bahanDimiliki))
        {
            return false;
        }

        if (HasItems(prerequisite.kebutuhanDimiliki) && !HasAllKebutuhan(prerequisite.kebutuhanDimiliki))
        {
            return false;
        }

        if (HasItems(prerequisite.tujuanFinansialDimiliki) && !HasAllStrings(
            prerequisite.tujuanFinansialDimiliki,
            GameState.Instance.GetTujuanFinansialList(GameState.Instance.turn)))
        {
            return false;
        }

        if (HasItems(prerequisite.masakanDijual) && !HasAllStrings(
            prerequisite.masakanDijual,
            GameState.Instance.GetMasakanDijualList(GameState.Instance.turn)))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(prerequisite.stat)
            && !CompareValue(GetDialogStatValue(prerequisite.stat), prerequisite.value, prerequisite.comparison))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(prerequisite.bahan)
            && CountBahan(prerequisite.bahan) < GetRequiredJumlah(prerequisite.jumlah))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(prerequisite.kebutuhan)
            && CountKebutuhanByName(prerequisite.kebutuhan) < GetRequiredJumlah(prerequisite.jumlah))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(prerequisite.tipeKebutuhan)
            && CountKebutuhanByType(prerequisite.tipeKebutuhan) < GetRequiredJumlah(prerequisite.jumlah))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(prerequisite.tujuanFinansial)
            && !string.Equals(GameState.Instance.tujuanFinansial, prerequisite.tujuanFinansial, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (prerequisite.hariDalamMinggu > 0 && GetHariDalamMinggu() != prerequisite.hariDalamMinggu)
        {
            return false;
        }

        if (prerequisite.turnKe > 0 && GameState.Instance.turn != prerequisite.turnKe)
        {
            return false;
        }

        if (prerequisite.playerCount > 0 && GameState.Instance.playerCount != prerequisite.playerCount)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(prerequisite.kondisi)
            && GetDialogCondition(prerequisite.kondisi) != prerequisite.aktif)
        {
            return false;
        }

        return true;
    }

    private int GetAksiValue(DialogPrerequisiteData prerequisite)
    {
        return prerequisite.aksiValue > 0 ? prerequisite.aksiValue : prerequisite.value;
    }

    private int GetDialogStatValue(string stat)
    {
        switch (NormalizeKey(stat))
        {
            case "coin":
            case "coins":
            case "koin":
                return GameState.Instance.Coins;
            case "happiness":
            case "happinesspoint":
            case "kebahagiaan":
                return GameState.Instance.Happiness;
            case "saving":
            case "tabungan":
                return GameState.Instance.Saving;
            case "emas":
            case "gold":
                return GameState.Instance.Emas;
            case "pinjaman":
            case "pinjamansyariah":
            case "kartupinjaman":
                return GameState.Instance.PinjamanSyariahCards;
            case "pedulidonasi":
            case "donasi":
                return GameState.Instance.GetPeduliDonasiTotal(GameState.Instance.turn);
            case "day":
            case "hari":
                return GameState.Instance.day;
            case "movesleft":
            case "moves":
            case "langkah":
                return GameState.Instance.movesLeft;
            case "turn":
            case "player":
                return GameState.Instance.turn;
            case "playercount":
            case "jumlahpemain":
                return GameState.Instance.playerCount;
            case "hargaemas":
            case "hargaemassaatini":
                return GameState.Instance.HargaEmasSaatIni;
            case "jumatberkah":
                return GameState.Instance.JumatBerkah;
            default:
                return 0;
        }
    }

    private bool GetDialogCondition(string kondisi)
    {
        switch (NormalizeKey(kondisi))
        {
            case "jumatberkah":
            case "harijumatberkah":
                return GameState.Instance.IsJumatBerkah();
            case "investasiemas":
            case "hariemas":
                return GameState.Instance.IsInvestasiEmasDay();
            case "gameover":
                return GameState.Instance.IsGameOver();
            case "paused":
            case "pause":
                return GameState.Instance.isPaused;
            default:
                return false;
        }
    }

    private int CountBahan(string namaBahan)
    {
        var bahanList = GameState.Instance.GetBahanList(GameState.Instance.turn);
        return bahanList.TryGetValue(namaBahan, out int jumlah) ? jumlah : 0;
    }

    private int CountKebutuhanByName(string namaKebutuhan)
    {
        var kebutuhanList = GameState.Instance.GetKebutuhanList(GameState.Instance.turn);
        int count = 0;

        foreach (var kebutuhanGroup in kebutuhanList.Values)
        {
            count += kebutuhanGroup.Count(kebutuhan =>
                string.Equals(kebutuhan, namaKebutuhan, StringComparison.OrdinalIgnoreCase));
        }

        return count;
    }

    private int CountKebutuhanByType(string tipeKebutuhan)
    {
        var kebutuhanList = GameState.Instance.GetKebutuhanList(GameState.Instance.turn);
        foreach (var kv in kebutuhanList)
        {
            if (string.Equals(kv.Key, tipeKebutuhan, StringComparison.OrdinalIgnoreCase))
            {
                return kv.Value.Count;
            }
        }

        return 0;
    }

    private bool HasAllBahan(List<string> bahanDimiliki)
    {
        foreach (string bahan in bahanDimiliki.Where(item => !string.IsNullOrWhiteSpace(item)))
        {
            if (CountBahan(bahan) <= 0)
            {
                return false;
            }
        }

        return true;
    }

    private bool HasAllKebutuhan(List<string> kebutuhanDimiliki)
    {
        foreach (string kebutuhan in kebutuhanDimiliki.Where(item => !string.IsNullOrWhiteSpace(item)))
        {
            if (CountKebutuhanByName(kebutuhan) <= 0 && CountKebutuhanByType(kebutuhan) <= 0)
            {
                return false;
            }
        }

        return true;
    }

    private bool HasAllStrings(List<string> requiredItems, List<string> ownedItems)
    {
        foreach (string requiredItem in requiredItems.Where(item => !string.IsNullOrWhiteSpace(item)))
        {
            if (!ownedItems.Any(ownedItem => string.Equals(ownedItem, requiredItem, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasItems(List<string> items)
    {
        return items != null && items.Any(item => !string.IsNullOrWhiteSpace(item));
    }

    private bool CompareValue(int actualValue, int expectedValue, string comparison)
    {
        switch (NormalizeKey(comparison))
        {
            case "equal":
            case "equals":
            case "sama":
            case "==":
            case "=":
                return actualValue == expectedValue;
            case "greater":
            case "lebihbesar":
            case ">":
                return actualValue > expectedValue;
            case "less":
            case "lebihkecil":
            case "<":
                return actualValue < expectedValue;
            case "maximum":
            case "maksimal":
            case "<=":
                return actualValue <= expectedValue;
            case "notequal":
            case "tidaksama":
            case "!=":
                return actualValue != expectedValue;
            case "minimum":
            case "minimal":
            case ">=":
            default:
                return actualValue >= expectedValue;
        }
    }

    private int GetRequiredJumlah(int jumlah)
    {
        return jumlah <= 0 ? 1 : jumlah;
    }

    private int GetHariDalamMinggu()
    {
        return ((GameState.Instance.day - 1) % 7) + 1;
    }

    private int GetMingguKe()
    {
        return ((GameState.Instance.day - 1) / 7) + 1;
    }

    private string NormalizeKey(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
    }
}
