using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class NarasiController
{
    // Satu-satunya jalur narasi: dialog karakter dari paket narasi aktif, lalu teks hasil aksi.
    // false berarti tidak ada dialog yang cocok sehingga pemanggil menampilkan teks hasil aksinya sendiri.
    public bool PlayDialogKarakterThen(string aksi, int aksiKe, string resultText, System.Action onComplete)
    {
        // Narasi yang sedang berjalan tidak boleh dipotong: onComplete-nya membawa kelanjutan giliran.
        if (isPlayingNarasi || !EnsureNarasiCache())
        {
            return false;
        }

        if (view == null)
        {
            view = FindObjectOfType<UIManagerPlay>();
        }

        if (view == null)
        {
            Debug.LogWarning("UIManagerPlay belum dihubungkan ke NarasiController.");
            return false;
        }

        DialogKarakterData dialogKarakter = GetDialogKarakterByPrerequisite(aksi, aksiKe);
        if (dialogKarakter == null)
        {
            return false;
        }

        List<DialogKarakterLineData> dialogLines = GetPlayableLines(dialogKarakter);
        if (dialogLines.Count == 0)
        {
            return false;
        }

        isPlayingNarasi = true;
        currentNarasiCoroutine = StartCoroutine(PlayDialogKarakter(dialogKarakter, dialogLines, GetActivePlayerTurn(), resultText, onComplete));
        return true;
    }

    private static List<DialogKarakterLineData> GetPlayableLines(DialogKarakterData dialogKarakter)
    {
        if (dialogKarakter?.lines == null)
        {
            return new List<DialogKarakterLineData>();
        }

        return dialogKarakter.lines
            .Where(line => line != null && !string.IsNullOrWhiteSpace(line.text))
            .ToList();
    }

    private IEnumerator PlayDialogKarakter(
        DialogKarakterData dialogKarakter,
        List<DialogKarakterLineData> dialogLines,
        int activePlayerTurn,
        string resultText,
        System.Action onComplete)
    {
        string npcName = string.IsNullOrWhiteSpace(dialogKarakter.npcName) ? "NPC" : dialogKarakter.npcName;
        string playerName = PlayerPrefs.GetString("PlayerName_" + activePlayerTurn, "Player " + activePlayerTurn);

        if (!string.IsNullOrWhiteSpace(dialogKarakter.npcSprite))
        {
            view.UpdateNpcContainerSprite(dialogKarakter.npcSprite);
        }

        foreach (DialogKarakterLineData line in dialogLines)
        {
            bool isNpcSpeaker = string.Equals(line.speaker, "NPC", System.StringComparison.OrdinalIgnoreCase);
            if (isNpcSpeaker)
            {
                view.HidePlayerDialogContainer();
                view.ShowNpcContainer();
                view.SetDialogNameOverride(npcName);
            }
            else
            {
                view.HideNpcContainer();
                view.UpdatePlayerContainerSprite(activePlayerTurn);
                view.ShowPlayerDialogContainer();
                view.SetDialogNameOverride(playerName);
            }

            yield return view.PlayDialogSteps(new List<string> { line.text });
        }

        // Teks hasil aksi tetap ditampilkan sesudah dialog agar pemain tahu akibat aksinya.
        if (!string.IsNullOrWhiteSpace(resultText))
        {
            view.HideNpcContainer();
            view.ShowPlayerDialogContainer();
            view.ClearDialogNameOverride();
            yield return view.PlaySystemDialogSteps(new List<string> { resultText });
        }

        // State quest hanya berubah lewat narasi, jadi efeknya diterapkan setelah dialognya benar-benar diputar.
        if (!string.IsNullOrWhiteSpace(dialogKarakter.questId) && GameState.Instance != null)
        {
            GameState.Instance.SetQuestState(activePlayerTurn, dialogKarakter.questId, dialogKarakter.questState);
        }

        MarkDialogPlayed(dialogKarakter.id, activePlayerTurn);
        view.HideNpcContainer();
        view.HidePlayerDialogContainer();
        view.ClearDialogNameOverride();
        currentNarasiCoroutine = null;
        isPlayingNarasi = false;
        onComplete?.Invoke();
    }

    private bool HasPlayedDialog(string dialogId, int player)
    {
        if (string.IsNullOrWhiteSpace(dialogId) || player < 1)
        {
            return false;
        }

        return PlayerPrefs.GetInt(NarafinSessionScope.GetDialogPlayedKey(dialogId, player), 0) == 1;
    }

    private void MarkDialogPlayed(string dialogId, int player)
    {
        if (string.IsNullOrWhiteSpace(dialogId) || player < 1)
        {
            return;
        }

        PlayerPrefs.SetInt(NarafinSessionScope.GetDialogPlayedKey(dialogId, player), 1);
        PlayerPrefs.Save();
    }
}
