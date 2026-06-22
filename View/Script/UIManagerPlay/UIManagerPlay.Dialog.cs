using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

public partial class UIManagerPlay
{
    // Dialog container visibility and typewriter text.
    private void ShowDialogContainer(bool showNameTag = true)
    {
        if (textContainer == null)
        {
            return;
        }

        textContainer.style.display = DisplayStyle.Flex;

        if (nameTag == null)
        {
            return;
        }

        if (!showNameTag)
        {
            nameTag.text = string.Empty;
            return;
        }

        if (!string.IsNullOrWhiteSpace(dialogNameOverride))
        {
            nameTag.text = dialogNameOverride;
            return;
        }

        if (GameState.Instance != null)
        {
            UpdateNameTag();
        }
    }

    private void HideDialogContainer()
    {
        if (textContainer == null)
        {
            return;
        }

        textContainer.style.display = DisplayStyle.None;
    }

    public void HideDialog()
    {
        HideDialogContainer();
    }

    private void Dialog(Label l, string text, float duration = 0.5f)
    {
        if (l == dialog)
        {
            dialogTween?.Kill();
        }

        l.text = string.Empty;
        string m = text;
        Tween tween = DOTween.To(()=> l.text, x => l.text = x, m, duration) .SetEase(Ease.Linear);

        if (l == dialog)
        {
            dialogTween = tween;
        }
    }

    public void AddTextToDialog(string text, float duration = 0.5f, bool newLine = false)
    {
        AddTextToDialog(text, duration, newLine, true);
    }

    public void AddSystemTextToDialog(string text, float duration = 0.5f, bool newLine = false)
    {
        AddTextToDialog(text, duration, newLine, false);
    }

    private void AddTextToDialog(string text, float duration, bool newLine, bool showNameTag)
    {
        ShowDialogContainer(showNameTag);
        Dialog(dialog, text, duration);

        if (newLine)
        {
            string currentText = dialog.text;
            string newText = currentText + text;
            DOTween.To(() => dialog.text, x => dialog.text = x, newText, duration).SetEase(Ease.Linear);
        }
    }

    public IEnumerator PlayDialogSteps(List<string> texts, float duration = 0.5f)
    {
        yield return PlayDialogSteps(texts, duration, true);
    }

    public IEnumerator PlaySystemDialogSteps(List<string> texts, float duration = 0.5f)
    {
        yield return PlayDialogSteps(texts, duration, false);
    }

    private IEnumerator PlayDialogSteps(List<string> texts, float duration, bool showNameTag)
    {
        foreach (var text in texts)
        {
            AddTextToDialog(text, duration, false, showNameTag);
            yield return null;

            bool goToNextText = false;
            while (!goToNextText)
            {
                if (ShouldAdvanceDialogOnClick())
                {
                    if (dialogTween != null && dialogTween.IsActive() && dialogTween.IsPlaying())
                    {
                        dialogTween.Complete();
                    }
                    else
                    {
                        goToNextText = true;
                    }
                }

                yield return null;
            }
        }
    }

    private bool ShouldAdvanceDialogOnClick()
    {
        if (!Input.GetMouseButtonDown(0) || IsInventoryPanelOpen())
        {
            return false;
        }

        return Time.frameCount != ignoredDialogClickFrame && !IsPointerOverButton();
    }

    private bool IsPointerOverButton()
    {
        if (rootElement == null || rootElement.panel == null)
        {
            return false;
        }

        Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(rootElement.panel, Input.mousePosition);
        VisualElement pickedElement = rootElement.panel.Pick(panelPosition);

        while (pickedElement != null)
        {
            if (pickedElement is Button)
            {
                return true;
            }

            pickedElement = pickedElement.parent;
        }

        return false;
    }
}
