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

        DialogKarakterData selectedDialogKarakter = GetDialogKarakterByPrerequisite(aksi, aksiKe);
        if (selectedDialogKarakter != null)
        {
            if (selectedDialogKarakter.steps != null && selectedDialogKarakter.steps.Count > 0)
            {
                if (currentNarasiCoroutine != null)
                {
                    StopCoroutine(currentNarasiCoroutine);
                }

                currentNarasiCoroutine = StartCoroutine(PlayDialogKarakterSteps(selectedDialogKarakter, onComplete));
                return true;
            }

            var dialogKarakterList = selectedDialogKarakter.dialog?
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToList();

            if (dialogKarakterList != null && dialogKarakterList.Count > 0)
            {
                if (currentNarasiCoroutine != null)
                {
                    StopCoroutine(currentNarasiCoroutine);
                }

                currentNarasiCoroutine = StartCoroutine(PlayDialogKarakter(selectedDialogKarakter, dialogKarakterList, onComplete));
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

    private IEnumerator PlayDialogKarakterSteps(DialogKarakterData dialogKarakter, System.Action onComplete)
    {
        foreach (var step in dialogKarakter.steps)
        {
            if (step == null)
            {
                continue;
            }

            view.ApplyDialogKarakter(step);

            var stepDialogList = step.dialog?
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToList();

            if (stepDialogList != null && stepDialogList.Count > 0)
            {
                yield return view.PlayDialogSteps(stepDialogList);
            }
        }

        MarkDialogPlayed(
            dialogKarakter.id, 
            GameState.Instance != null ? GameState.Instance.turn : 0
        );

        yield return view.DismissAllDialogCharacters();
        view.ClearDialogNameOverride();
        currentNarasiCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator PlayDialogKarakter(DialogKarakterData dialogKarakter, List<string> dialogList, System.Action onComplete)
    {
        view.ApplyDialogKarakter(dialogKarakter);
        yield return view.PlayDialogSteps(dialogList);

        MarkDialogPlayed(
            dialogKarakter.id, 
            GameState.Instance != null ? GameState.Instance.turn : 0
        );

        yield return view.DismissDialogKarakter(dialogKarakter);
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
        return playedDialogs.TryGetValue(player, out var dialogs)
            && dialogs.Contains(dialogId);
    }

    private void MarkDialogPlayed(string dialogId, int player)
    {
        if (!playedDialogs.TryGetValue(player, out var dialogs))
        {
            dialogs = new HashSet<string>();
            playedDialogs[player] = dialogs;
        }

        dialogs.Add(dialogId);
    }
}
