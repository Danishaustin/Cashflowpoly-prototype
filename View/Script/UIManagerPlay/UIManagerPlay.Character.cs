using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManagerPlay
{
    private const float DialogCharacterTransitionDuration = 0.25f;
    private const int PlayerTurnChangeDelayMs = 500;

    private string dialogNameOverride;
    private int currentPlayerContainerTurn = -1;

    public void ApplyDialogKarakter(DialogKarakterData dialogKarakter)
    {
        if (dialogKarakter == null)
        {
            return;
        }

        SetDialogNameOverride(string.IsNullOrWhiteSpace(dialogKarakter.npcName) ? "NPC" : dialogKarakter.npcName);

        if (!string.IsNullOrWhiteSpace(dialogKarakter.npcSprite))
        {
            UpdateNpcContainerSprite(dialogKarakter.npcSprite);
        }

        ShowNpcContainer();
    }

    public void UpdatePlayerContainerSprite(int turn)
    {
        if (playerContainer == null)
        {
            return;
        }

        int safeTurn = Mathf.Clamp(turn, 1, 4);
        string spritePath = "Sprite/Character/Player/player" + safeTurn;
        Sprite sprite = Resources.Load<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogWarning("Sprite player tidak ditemukan di Resources: " + spritePath);
            return;
        }

        playerContainer.style.backgroundImage = new StyleBackground(sprite);
        currentPlayerContainerTurn = safeTurn;
    }

    public void RefreshPlayerContainerForTurn(int turn)
    {
        if (playerContainer == null)
        {
            return;
        }

        int safeTurn = Mathf.Clamp(turn, 1, 4);

        if (!playerContainer.ClassListContains("show-character"))
        {
            UpdatePlayerContainerSprite(safeTurn);
            return;
        }

        if (currentPlayerContainerTurn == safeTurn)
        {
            return;
        }

        PlayPlayerContainerExitThen(() =>
        {
            UpdatePlayerContainerSprite(safeTurn);

            if (playerContainer != null)
            {
                playerContainer.style.display = DisplayStyle.Flex;
                playerContainer.AddToClassList("show-character");
            }
        });
    }

    public void ShowPlayerDialogContainer()
    {
        if (playerContainer == null)
        {
            return;
        }

        playerContainer.style.display = DisplayStyle.Flex;
        playerContainer.style.scale = new Scale(new Vector3(-1f, 1f, 1f));
        playerContainer.RemoveFromClassList("show-character");
        playerContainer.schedule.Execute(() =>
        {
            playerContainer.AddToClassList("show-character");
        }).StartingIn(1);
    }

    public void HidePlayerDialogContainer()
    {
        if (playerContainer == null)
        {
            return;
        }

        playerContainer.RemoveFromClassList("show-character");
    }

    public void PlayPlayerContainerExitThen(Action onComplete = null)
    {
        if (playerContainer == null)
        {
            SchedulePlayerTurnCallback(onComplete);
            return;
        }

        if (playerContainer.ClassListContains("show-character"))
        {
            playerContainer.RemoveFromClassList("show-character");
        }

        SchedulePlayerTurnCallback(onComplete);
    }

    private void SchedulePlayerTurnCallback(Action onComplete)
    {
        VisualElement schedulerSource = playerContainer != null ? playerContainer : rootElement;
        if (schedulerSource == null)
        {
            onComplete?.Invoke();
            return;
        }

        schedulerSource.schedule.Execute(() =>
        {
            onComplete?.Invoke();
        }).StartingIn(PlayerTurnChangeDelayMs);
    }

    public void UpdateNpcContainerSprite(string npcSpriteName)
    {
        if (npcContainer == null || string.IsNullOrWhiteSpace(npcSpriteName))
        {
            return;
        }

        string spritePath = "Sprite/Character/NPC/" + npcSpriteName;
        Sprite sprite = Resources.Load<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogWarning("Sprite NPC tidak ditemukan di Resources: " + spritePath);
            return;
        }

        npcContainer.style.backgroundImage = new StyleBackground(sprite);
    }

    public void ShowNpcContainer()
    {
        if (npcContainer == null)
        {
            return;
        }

        npcContainer.style.display = DisplayStyle.Flex;
        npcContainer.RemoveFromClassList("show-npc-character");
        npcContainer.schedule.Execute(() =>
        {
            npcContainer.AddToClassList("show-npc-character");
        }).StartingIn(1);
    }

    public void HideNpcContainer()
    {
        if (npcContainer == null)
        {
            return;
        }

        npcContainer.RemoveFromClassList("show-npc-character");
    }

    public IEnumerator DismissDialogKarakter(DialogKarakterData dialogKarakter)
    {
        HideNpcContainer();
        HidePlayerDialogContainer();
        yield return new WaitForSeconds(DialogCharacterTransitionDuration);
    }

    public IEnumerator DismissAllDialogCharacters()
    {
        bool hasDismissedAnyCharacter = false;

        if (npcContainer != null && npcContainer.ClassListContains("show-npc-character"))
        {
            HideNpcContainer();
            hasDismissedAnyCharacter = true;
        }

        if (playerContainer != null && playerContainer.ClassListContains("show-character"))
        {
            HidePlayerDialogContainer();
            hasDismissedAnyCharacter = true;
        }

        if (hasDismissedAnyCharacter)
        {
            yield return new WaitForSeconds(DialogCharacterTransitionDuration);
        }
    }

    public void SetDialogNameOverride(string characterName)
    {
        dialogNameOverride = characterName;
    }

    public void ClearDialogNameOverride()
    {
        dialogNameOverride = null;
    }
}
