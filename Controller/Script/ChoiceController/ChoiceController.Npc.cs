using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class ChoiceController
{
    private sealed class StaticNpcDialogue
    {
        public string Id;
        public string NpcName;
        public string NpcSprite;
        public List<StaticNpcDialogueLine> Lines;
    }

    private sealed class StaticNpcDialogueLine
    {
        public string Speaker;
        public string Text;
    }

    protected bool PlayNpcStaticDialogThen(string aksi, string resultText, Action onComplete)
    {
        if (view == null || DataManager.Instance == null || DataManager.Instance.dialogKarakterDict == null)
        {
            return false;
        }

        StaticNpcDialogue dialogue = GetStaticNpcDialogue(aksi);
        if (dialogue == null || dialogue.Lines == null || dialogue.Lines.Count == 0)
        {
            return false;
        }

        StartCoroutine(PlayNpcStaticDialogRoutine(dialogue, resultText, onComplete));
        return true;
    }

    private StaticNpcDialogue GetStaticNpcDialogue(string aksi)
    {
        if (string.IsNullOrWhiteSpace(aksi) || DataManager.Instance == null || DataManager.Instance.dialogKarakterDict == null)
        {
            return null;
        }

        string normalizedAksi = GameState.Instance != null ? GameState.Instance.NormalizeActionName(aksi) : aksi;
        int activePlayerTurn = GetActivePlayerTurn();

        DialogKarakterData selectedEntry = DataManager.Instance.dialogKarakterDict.Values
            .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.aksi))
            .Where(entry => string.Equals(
                GameState.Instance != null ? GameState.Instance.NormalizeActionName(entry.aksi) : entry.aksi,
                normalizedAksi,
                StringComparison.OrdinalIgnoreCase))
            .Where(entry => !HasPlayedStaticDialog(entry.id, activePlayerTurn))
            .Where(IsStaticDialogPrerequisiteMet)
            .OrderByDescending(entry => entry.aksiValue)
            .ThenByDescending(CountValidStaticPrerequisites)
            .FirstOrDefault();

        if (selectedEntry == null)
        {
            return null;
        }

        return new StaticNpcDialogue
        {
            Id = selectedEntry.id,
            NpcName = string.IsNullOrWhiteSpace(selectedEntry.npcName) ? "NPC" : selectedEntry.npcName,
            NpcSprite = selectedEntry.npcSprite,
            Lines = selectedEntry.lines?
                .Where(line => line != null && !string.IsNullOrWhiteSpace(line.text))
                .Select(line => new StaticNpcDialogueLine
                {
                    Speaker = line.speaker,
                    Text = line.text
                })
                .ToList()
        };
    }

    private IEnumerator PlayNpcStaticDialogRoutine(StaticNpcDialogue dialogue, string resultText, Action onComplete)
    {
        if (view == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        string playerName = GetActivePlayerName();

        if (!string.IsNullOrWhiteSpace(dialogue.NpcSprite))
        {
            view.UpdateNpcContainerSprite(dialogue.NpcSprite);
        }

        foreach (StaticNpcDialogueLine line in dialogue.Lines)
        {
            if (line == null || string.IsNullOrWhiteSpace(line.Text))
            {
                continue;
            }

            bool isNpcSpeaker = string.Equals(line.Speaker, "NPC", StringComparison.OrdinalIgnoreCase);
            if (isNpcSpeaker)
            {
                view.HidePlayerDialogContainer();
                view.ShowNpcContainer();
                view.SetDialogNameOverride(dialogue.NpcName);
            }
            else
            {
                view.HideNpcContainer();
                view.UpdatePlayerContainerSprite(GetActivePlayerTurn());
                view.ShowPlayerDialogContainer();
                view.SetDialogNameOverride(playerName);
            }

            yield return view.PlayDialogSteps(new List<string> { line.Text });
        }

        if (!string.IsNullOrWhiteSpace(resultText))
        {
            view.HideNpcContainer();
            view.ShowPlayerDialogContainer();
            view.ClearDialogNameOverride();
            yield return view.PlaySystemDialogSteps(new List<string> { resultText });
        }

        MarkStaticDialogPlayedForPlayer(dialogue.Id, GetActivePlayerTurn());
        view.HideNpcContainer();
        view.HidePlayerDialogContainer();
        view.ClearDialogNameOverride();
        onComplete?.Invoke();
    }

    private int GetActivePlayerTurn()
    {
        return GameState.Instance != null ? GameState.Instance.turn : 1;
    }

    private string GetActivePlayerName()
    {
        int turn = GetActivePlayerTurn();
        return PlayerPrefs.GetString("PlayerName_" + turn, "Player " + turn);
    }

    private bool HasPlayedStaticDialog(string dialogId, int player)
    {
        if (string.IsNullOrWhiteSpace(dialogId) || player < 1)
        {
            return false;
        }

        return PlayerPrefs.GetInt(NarafinSessionScope.GetStaticDialogPlayedKey(dialogId, player), 0) == 1;
    }

    private void MarkStaticDialogPlayedForPlayer(string dialogId, int player)
    {
        if (string.IsNullOrWhiteSpace(dialogId) || player < 1)
        {
            return;
        }

        PlayerPrefs.SetInt(NarafinSessionScope.GetStaticDialogPlayedKey(dialogId, player), 1);
        PlayerPrefs.Save();
    }

    private bool IsStaticDialogPrerequisiteMet(DialogKarakterData dialogData)
    {
        if (GameState.Instance == null || dialogData == null)
        {
            return false;
        }

        var prerequisites = GetStaticDialogPrerequisites(dialogData);
        if (prerequisites.Count == 0)
        {
            return true;
        }

        foreach (var prerequisite in prerequisites)
        {
            if (!IsStaticDialogPrerequisiteMet(prerequisite))
            {
                return false;
            }
        }

        return true;
    }

    private int CountValidStaticPrerequisites(DialogKarakterData dialogData)
    {
        return GetStaticDialogPrerequisites(dialogData).Count;
    }

    private List<DialogPrerequisiteData> GetStaticDialogPrerequisites(DialogKarakterData dialogData)
    {
        if (dialogData == null || dialogData.prerequisite == null)
        {
            return new List<DialogPrerequisiteData>();
        }

        return dialogData.prerequisite
            .Where(prerequisite => IsActiveStaticDialogPrerequisite(prerequisite))
            .ToList();
    }

    private bool IsActiveStaticDialogPrerequisite(DialogPrerequisiteData prerequisite)
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
                || prerequisite.mingguKe > 0);
    }

    private bool IsStaticDialogPrerequisiteMet(DialogPrerequisiteData prerequisite)
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

        if (prerequisite.mingguKe > 0 && GetCurrentWeek() != prerequisite.mingguKe)
        {
            return false;
        }

        if (prerequisite.hariKe > 0 && GetCurrentDayOfWeek() != prerequisite.hariKe)
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


        return true;
    }

    private int CountBahan(string namaBahan)
    {
        return GameState.Instance.GetBahanCount(GameState.Instance.turn, namaBahan);
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

    private int GetCurrentDayOfWeek()
    {
        return ((GameState.Instance.day - 1) % 7) + 1;
    }

    private int GetCurrentWeek()
    {
        return ((GameState.Instance.day - 1) / 7) + 1;
    }

}



