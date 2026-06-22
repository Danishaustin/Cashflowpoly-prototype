using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManagerPlay
{
    private const float DialogCharacterTransitionDuration = 0.25f;

    private string dialogNameOverride;
    private VisualElement leftDialogCharacter;

    public void ApplyDialogKarakter(DialogKarakterData dialogKarakter)
    {
        if (dialogKarakter == null)
        {
            return;
        }

        ApplyDialogKarakter(
            dialogKarakter.namaKarakter,
            dialogKarakter.spritePath,
            dialogKarakter.menghadapKanan,
            dialogKarakter.aksiKarakter);
    }

    public void ApplyDialogKarakter(DialogKarakterStepData step)
    {
        if (step == null)
        {
            return;
        }

        bool useLeftCharacter = step.posisiKarakterDiKiri || step.menghadapKanan;
        ApplyDialogKarakter(step.namaKarakter, step.spritePath, useLeftCharacter, step.aksiKarakter);
    }

    private void ApplyDialogKarakter(string namaKarakter, string spritePath, bool menghadapKanan, string aksiKarakter)
    {
        SetDialogNameOverride(namaKarakter);

        if (playerContainer == null)
        {
            return;
        }

        string action = aksiKarakter ?? string.Empty;
        if (menghadapKanan)
        {
            if (action == "Menghilang")
            {
                HideLeftDialogCharacter();
                return;
            }

            ShowLeftDialogCharacter(spritePath);
            return;
        }

        if (action == "Menghilang")
        {
            playerContainer.RemoveFromClassList("show-character");
            return;
        }

        playerContainer.style.display = DisplayStyle.Flex;
        playerContainer.AddToClassList("show-character");
        playerContainer.style.scale = new Scale(new Vector3(menghadapKanan ? 1f : -1f, 1f, 1f));

        if (string.IsNullOrWhiteSpace(spritePath))
        {
            return;
        }

        Sprite sprite = Resources.Load<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogWarning("Sprite karakter tidak ditemukan di Resources: " + spritePath);
            return;
        }

        playerContainer.style.backgroundImage = new StyleBackground(sprite);
    }

    private void ShowLeftDialogCharacter(string spritePath)
    {
        VisualElement character = GetOrCreateLeftDialogCharacter();
        character.RemoveFromClassList("show-dialog-character-left");
        character.style.display = DisplayStyle.Flex;
        character.style.scale = new Scale(new Vector3(1f, 1f, 1f));
        ApplyCharacterSprite(character, spritePath);

        character.schedule.Execute(() =>
        {
            character.AddToClassList("show-dialog-character-left");
        }).StartingIn(1);
    }

    private VisualElement GetOrCreateLeftDialogCharacter()
    {
        if (leftDialogCharacter != null)
        {
            return leftDialogCharacter;
        }

        VisualElement parent = playerContainer != null ? playerContainer.parent : rootElement;
        leftDialogCharacter = new VisualElement
        {
            name = "LeftDialogCharacter",
            pickingMode = PickingMode.Ignore
        };
        leftDialogCharacter.AddToClassList("character");
        leftDialogCharacter.AddToClassList("dialog-character-left");
        PlaceDialogCharacterBehindDialogBox(parent, leftDialogCharacter);

        return leftDialogCharacter;
    }

    private void PlaceDialogCharacterBehindDialogBox(VisualElement parent, VisualElement character)
    {
        if (parent == null || character == null)
        {
            return;
        }

        if (textContainer == null)
        {
            parent.Add(character);
            return;
        }

        int textContainerIndex = parent.IndexOf(textContainer);
        if (textContainerIndex >= 0)
        {
            parent.Insert(textContainerIndex, character);
            return;
        }

        parent.Add(character);
    }

    private void ApplyCharacterSprite(VisualElement character, string spritePath)
    {
        if (character == null || string.IsNullOrWhiteSpace(spritePath))
        {
            return;
        }

        Sprite sprite = Resources.Load<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogWarning("Sprite karakter tidak ditemukan di Resources: " + spritePath);
            return;
        }

        character.style.backgroundImage = new StyleBackground(sprite);
    }

    public IEnumerator DismissDialogKarakter(DialogKarakterData dialogKarakter)
    {
        if (dialogKarakter == null || !dialogKarakter.menghadapKanan || dialogKarakter.aksiKarakter != "Kemunculan")
        {
            yield break;
        }

        if (leftDialogCharacter == null)
        {
            yield break;
        }

        HideLeftDialogCharacter();
        yield return new WaitForSeconds(DialogCharacterTransitionDuration);

        leftDialogCharacter.RemoveFromHierarchy();
        leftDialogCharacter = null;
    }

    public IEnumerator DismissAllDialogCharacters()
    {
        if (leftDialogCharacter != null)
        {
            HideLeftDialogCharacter();
            yield return new WaitForSeconds(DialogCharacterTransitionDuration);
            leftDialogCharacter.RemoveFromHierarchy();
            leftDialogCharacter = null;
        }
    }

    private void HideLeftDialogCharacter()
    {
        leftDialogCharacter?.RemoveFromClassList("show-dialog-character-left");
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
