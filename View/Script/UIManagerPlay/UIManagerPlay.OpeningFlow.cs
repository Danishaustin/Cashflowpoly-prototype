using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

public partial class UIManagerPlay
{
    // Opening character, intro dialog, and first choice flow.
    public void ShowPlayerContainer()
    {
        if (choiceContainers != null && choiceContainers.ContainsKey("ChoiceInitialBahan"))
        {
            choiceContainers["ChoiceInitialBahan"].RemoveFromClassList("show-choice");
            choiceContainers["ChoiceInitialBahan"].style.display = DisplayStyle.None;
        }

        if (choiceContainers != null && choiceContainers.ContainsKey("ChoiceTargetKebutuhan"))
        {
            choiceContainers["ChoiceTargetKebutuhan"].RemoveFromClassList("show-choice");
            choiceContainers["ChoiceTargetKebutuhan"].style.display = DisplayStyle.None;
        }

        if (targetKebutuhanOverlay != null)
        {
            targetKebutuhanOverlay.style.display = DisplayStyle.None;
        }

        playerContainer.style.display = DisplayStyle.Flex;
        playerContainer.schedule.Execute(() =>
        {
            playerContainer.AddToClassList("show-character");
        }).StartingIn(1);
    }

    private void ShowInitialBahanChoice()
    {
        ShowInitialBahanChoiceForPlayer(1);
    }

    public void ShowInitialBahanChoiceForPlayer(int player)
    {
        HideDialogContainer();
        SetChoiceBackground("ChoiceInitialBahan");

        if (targetKebutuhanOverlay != null)
        {
            targetKebutuhanOverlay.style.display = DisplayStyle.Flex;
        }

        if (initialBahanTitle != null)
        {
            string playerName = PlayerPrefs.GetString("PlayerName_" + player, "Player " + player);
            initialBahanTitle.text = "Bahan Masakan Awal Untuk " + playerName;
        }

        ResetInitialBahanSelectionButtons();

        if (choiceContainers != null && choiceContainers.ContainsKey("ChoiceInitialBahan"))
        {
            choiceContainers["ChoiceInitialBahan"].style.display = DisplayStyle.Flex;
            if (!choiceContainers["ChoiceInitialBahan"].ClassListContains("show-choice"))
            {
                choiceContainers["ChoiceInitialBahan"].schedule.Execute(() =>
                {
                    choiceContainers["ChoiceInitialBahan"].AddToClassList("show-choice");
                }).StartingIn(100);
            }
        }
    }

    public void TransitionInitialBahanToTargetKebutuhan()
    {
        if (choiceContainers == null || !choiceContainers.ContainsKey("ChoiceInitialBahan"))
        {
            ShowTargetKebutuhanChoiceForPlayer(1);
            return;
        }

        var initialContainer = choiceContainers["ChoiceInitialBahan"];
        initialContainer.RemoveFromClassList("show-choice");
        initialContainer.schedule.Execute(() =>
        {
            initialContainer.style.display = DisplayStyle.None;
            ShowTargetKebutuhanChoiceForPlayer(1);
        }).StartingIn(420);
    }

    private void ShowTargetKebutuhanChoice()
    {
        ShowTargetKebutuhanChoiceForPlayer(1);
    }

    public void ShowTargetKebutuhanChoiceForPlayer(int player)
    {
        HideDialogContainer();
        SetChoiceBackground("ChoiceTargetKebutuhan");
        choiceController?.ResetTargetKebutuhanSelection();

        if (choiceContainers != null && choiceContainers.ContainsKey("ChoiceInitialBahan"))
        {
            choiceContainers["ChoiceInitialBahan"].RemoveFromClassList("show-choice");
            choiceContainers["ChoiceInitialBahan"].style.display = DisplayStyle.None;
        }

        if (targetKebutuhanOverlay != null)
        {
            targetKebutuhanOverlay.style.display = DisplayStyle.Flex;
        }

        if (targetKebutuhanTitle != null)
        {
            targetKebutuhanTitle.text = "Pilih Target Kebutuhan";
        }

        RefreshTargetKebutuhanButtons();
        int targetPlayerCount = GameState.Instance != null ? GameState.Instance.playerCount : 3;
        RefreshTargetKebutuhanSelectionUI(new List<string>(), targetPlayerCount);

        if (choiceContainers != null && choiceContainers.ContainsKey("ChoiceTargetKebutuhan"))
        {
            choiceContainers["ChoiceTargetKebutuhan"].style.display = DisplayStyle.Flex;
            if (!choiceContainers["ChoiceTargetKebutuhan"].ClassListContains("show-choice"))
            {
                choiceContainers["ChoiceTargetKebutuhan"].schedule.Execute(() =>
                {
                    choiceContainers["ChoiceTargetKebutuhan"].AddToClassList("show-choice");
                }).StartingIn(100);
            }
        }
    }

    private void ShowTextContainer(TransitionEndEvent evt)
    {
        if (hasStartedOpeningChoiceFlow)
        {
            return;
        }

        hasStartedOpeningChoiceFlow = true;
        ShowDialogContainer();
        Dialog(dialog, "Sekarang ngapain ya?");
        StartCoroutine(WaitForOpeningChoiceClick());
    }

    private IEnumerator WaitForOpeningChoiceClick()
    {
        yield return null;

        bool canShowChoice = false;
        while (!canShowChoice)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (dialogTween != null && dialogTween.IsActive() && dialogTween.IsPlaying())
                {
                    dialogTween.Complete();
                }
                else
                {
                    canShowChoice = true;
                }
            }

            yield return null;
        }

        ShowChoice1();
    }

    private void ShowChoice1()
    {
        HideDialogContainer();
        SetChoiceBackground("Choice1");
        choiceContainers["Choice1"].style.display = DisplayStyle.Flex;
        choiceContainers["Choice1"].schedule.Execute(() =>
        {
            choiceContainers["Choice1"].AddToClassList("show-choice");
        }).StartingIn(1);
    }
}
