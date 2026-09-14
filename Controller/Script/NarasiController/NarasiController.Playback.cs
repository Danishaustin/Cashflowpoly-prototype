using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class NarasiController
{
    // Entry point used by choice actions to play matching narasi before continuing.
    public bool HandleNarasi(string aksi, int aksiKe, System.Action onComplete = null)
    {
        if (!EnsureNarasiCache())
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

        int activePlayerTurn = GetActivePlayerTurn();
        DialogKarakterData selectedDialogKarakter = GetDialogKarakterByPrerequisite(aksi, aksiKe);
        if (selectedDialogKarakter != null)
        {
            var dialogLines = selectedDialogKarakter.lines?
                .Where(line => line != null && !string.IsNullOrWhiteSpace(line.text))
                .ToList();

            if (dialogLines != null && dialogLines.Count > 0)
            {
                if (currentNarasiCoroutine != null)
                {
                    StopCoroutine(currentNarasiCoroutine);
                }

                currentNarasiCoroutine = StartCoroutine(PlayDialogKarakter(selectedDialogKarakter, dialogLines, activePlayerTurn, onComplete));
                return true;
            }
        }

        NarasiData selectedNarasi = GetNarasiByPrerequisite(aksi, aksiKe);
        if (selectedNarasi == null)
        {
            return false;
        }

        if (currentNarasiCoroutine != null)
        {
            StopCoroutine(currentNarasiCoroutine);
        }

        var narasiList = new List<string>()
        {
            selectedNarasi.narasi1,
            selectedNarasi.narasi2,
            selectedNarasi.narasi3
        }
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        if (narasiList.Count == 0)
        {
            return false;
        }

        currentNarasiCoroutine = StartCoroutine(PlayNarasi(narasiList, onComplete));
        return true;
    }

    private IEnumerator PlayDialogKarakter(
        DialogKarakterData dialogKarakter,
        List<DialogKarakterLineData> dialogLines,
        int activePlayerTurn,
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

        MarkDialogPlayed(dialogKarakter.id, activePlayerTurn);
        view.HideNpcContainer();
        view.HidePlayerDialogContainer();
        view.ClearDialogNameOverride();
        currentNarasiCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator PlayNarasi(List<string> narasiList, System.Action onComplete)
    {
        yield return view.PlayDialogSteps(narasiList);
        currentNarasiCoroutine = null;
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
