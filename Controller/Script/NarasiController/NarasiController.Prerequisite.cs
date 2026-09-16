using System;
using System.Collections.Generic;
using System.Linq;

public partial class NarasiController
{
    // Dialog paling spesifik yang cocok dengan aksi ke-n; bila tidak ada, dipakai dialog umum (aksiValue 0).
    private DialogKarakterData GetDialogKarakterByPrerequisite(string aksi, int aksiKe)
    {
        DialogKarakterData dialogKarakter = FindDialogKarakter(aksi, aksiKe);
        if (dialogKarakter != null || aksiKe == 0)
        {
            return dialogKarakter;
        }

        return FindDialogKarakter(aksi, 0);
    }

    private DialogKarakterData FindDialogKarakter(string aksi, int aksiValue)
    {
        int activePlayerTurn = GetActivePlayerTurn();
        return dialogKarakterList
            .Where(dialogKarakter => !HasPlayedDialog(dialogKarakter.id, activePlayerTurn))
            .Where(dialogKarakter => HasTriggerPrerequisite(dialogKarakter, aksi, aksiValue))
            .Where(IsPrerequisiteMet)
            .OrderByDescending(CountValidPrerequisites)
            .FirstOrDefault();
    }

    private bool HasTriggerPrerequisite(DialogKarakterData dialogKarakter, string aksi, int value)
    {
        if (GameState.Instance == null || dialogKarakter == null || string.IsNullOrWhiteSpace(dialogKarakter.aksi))
        {
            return false;
        }

        string normalizedAksi = GameState.Instance.NormalizeActionName(aksi);
        string normalizedDialogAksi = GameState.Instance.NormalizeActionName(dialogKarakter.aksi);
        return normalizedDialogAksi == normalizedAksi && dialogKarakter.aksiValue == value;
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

    private int CountValidPrerequisites(DialogKarakterData dialogKarakter)
    {
        return GetDialogPrerequisites(dialogKarakter).Count;
    }

    private List<DialogPrerequisiteData> GetDialogPrerequisites(DialogKarakterData dialogKarakter)
    {
        if (dialogKarakter?.prerequisite == null)
        {
            return new List<DialogPrerequisiteData>();
        }

        return dialogKarakter.prerequisite
            .Where(IsActiveDialogPrerequisite)
            .ToList();
    }

    private bool IsActiveDialogPrerequisite(DialogPrerequisiteData prerequisite)
    {
        return prerequisite != null
            && (prerequisite.uang > 0
                || prerequisite.kebahagiaan > 0
                || prerequisite.tabungan > 0
                || prerequisite.emas > 0
                || prerequisite.kartuPinjaman > 0
                || prerequisite.asuransiDimiliki
                || HasItems(prerequisite.bahanDimiliki)
                || HasItems(prerequisite.kebutuhanDimiliki)
                || HasItems(prerequisite.tujuanFinansialDimiliki)
                || HasItems(prerequisite.masakanDijual)
                || prerequisite.hariKe > 0
                || prerequisite.mingguKe > 0
                || !string.IsNullOrWhiteSpace(prerequisite.questId));
    }

    private bool IsDialogPrerequisiteMet(DialogPrerequisiteData prerequisite)
    {
        if (GameState.Instance == null || prerequisite == null)
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

        if (!string.IsNullOrWhiteSpace(prerequisite.questId)
            && !GameState.Instance.IsQuestInState(GameState.Instance.turn, prerequisite.questId, prerequisite.questState))
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

        if (HasItems(prerequisite.tujuanFinansialDimiliki)
            && !GameState.Instance.HasAllTujuanFinansial(GameState.Instance.turn, prerequisite.tujuanFinansialDimiliki))
        {
            return false;
        }

        if (HasItems(prerequisite.masakanDijual) && !HasAllStrings(
            prerequisite.masakanDijual,
            GameState.Instance.GetMasakanDijualList(GameState.Instance.turn)))
        {
            return false;
        }

        return true;
    }

    private int CountBahan(string namaBahan)
    {
        return GameState.Instance.GetBahanCount(GameState.Instance.turn, namaBahan);
    }

    private int CountKebutuhanByName(string namaKebutuhan)
    {
        return GameState.Instance.CountKebutuhanByName(GameState.Instance.turn, namaKebutuhan);
    }

    private int CountKebutuhanByType(string tipeKebutuhan)
    {
        return GameState.Instance.CountKebutuhanByTipe(GameState.Instance.turn, tipeKebutuhan);
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
        if (requiredItems == null || ownedItems == null)
        {
            return false;
        }

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

    private int GetHariDalamMinggu()
    {
        return ((GameState.Instance.day - 1) % 7) + 1;
    }

    private int GetMingguKe()
    {
        return ((GameState.Instance.day - 1) / 7) + 1;
    }
}
