using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManager
{
    private const float ErrorPopupHideDelaySeconds = 0.2f;

    private void ShowErrorPopup(string message)
    {
        ShowStatusPopup("Terjadi Kesalahan", message, false);
    }

    private void ShowSuccessPopup(string message)
    {
        ShowStatusPopup("Berhasil", message, true);
    }

    private void ShowStatusPopup(string title, string message, bool isSuccess)
    {
        if (errorPopupOverlay == null || errorPopupCard == null)
        {
            return;
        }

        if (errorPopupHideCoroutine != null)
        {
            StopCoroutine(errorPopupHideCoroutine);
            errorPopupHideCoroutine = null;
        }

        string safeTitle = string.IsNullOrWhiteSpace(title) ? "Info" : title;
        string safeMessage = string.IsNullOrWhiteSpace(message)
            ? (isSuccess ? "Proses berhasil." : "Proses gagal. Coba lagi.")
            : message;

        RebuildStatusPopupContent(safeTitle, safeMessage, isSuccess);

        errorPopupOverlay.style.display = DisplayStyle.Flex;
        errorPopupOverlay.style.opacity = 1f;
        errorPopupCard.style.opacity = 1f;
        errorPopupOverlay.BringToFront();
        errorPopupCard.BringToFront();
        errorPopupOverlay.RemoveFromClassList("show-error-popup-overlay");
        errorPopupCard.RemoveFromClassList("show-error-popup");

        errorPopupOverlay.schedule.Execute(() =>
        {
            RebuildStatusPopupContent(safeTitle, safeMessage, isSuccess);
            errorPopupOverlay.AddToClassList("show-error-popup-overlay");
            errorPopupCard.AddToClassList("show-error-popup");
        }).ExecuteLater(10);
    }

    private void RebuildStatusPopupContent(string title, string message, bool isSuccess)
    {
        if (errorPopupCard == null)
        {
            return;
        }

        Button okButton = errorPopupOkButton ?? errorPopupCard.Q<Button>("ErrorPopupOkButton");
        errorPopupCard.Clear();

        errorPopupTitle = new Label(title)
        {
            name = "ErrorPopupTitle"
        };
        errorPopupTitle.AddToClassList("error-popup-title");
        ApplyStatusPopupLabelStyle(errorPopupTitle, isSuccess ? new Color(160f / 255f, 255f / 255f, 170f / 255f) : new Color(255f / 255f, 210f / 255f, 90f / 255f), 48, 72);

        errorPopupMessage = new Label(message)
        {
            name = "ErrorPopupMessage"
        };
        errorPopupMessage.AddToClassList("error-popup-message");
        ApplyStatusPopupLabelStyle(errorPopupMessage, Color.white, 32, 110);
        errorPopupMessage.style.whiteSpace = WhiteSpace.Normal;

        if (okButton == null)
        {
            okButton = new Button(HideErrorPopup)
            {
                name = "ErrorPopupOkButton",
                text = "OK"
            };
            errorPopupOkButton = okButton;
        }
        else
        {
            okButton.text = "OK";
        }

        okButton.AddToClassList("home-button");
        okButton.AddToClassList("error-popup-button");
        okButton.style.display = DisplayStyle.Flex;
        okButton.style.opacity = 1f;

        errorPopupCard.Add(errorPopupTitle);
        errorPopupCard.Add(errorPopupMessage);
        errorPopupCard.Add(okButton);
    }

    private void ApplyStatusPopupLabelStyle(Label label, Color color, int fontSize, int minHeight)
    {
        if (label == null)
        {
            return;
        }

        label.style.display = DisplayStyle.Flex;
        label.style.opacity = 1f;
        label.style.color = color;
        label.style.width = Length.Percent(100);
        label.style.minHeight = minHeight;
        label.style.fontSize = fontSize;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
    }

    private void HideErrorPopup()
    {
        if (errorPopupOverlay == null || errorPopupCard == null)
        {
            return;
        }

        errorPopupOverlay.RemoveFromClassList("show-error-popup-overlay");
        errorPopupCard.RemoveFromClassList("show-error-popup");

        if (errorPopupHideCoroutine != null)
        {
            StopCoroutine(errorPopupHideCoroutine);
        }

        errorPopupHideCoroutine = StartCoroutine(HideErrorPopupAfterAnimation());
    }

    private IEnumerator HideErrorPopupAfterAnimation()
    {
        yield return new WaitForSeconds(ErrorPopupHideDelaySeconds);

        if (errorPopupOverlay != null)
        {
            errorPopupOverlay.style.display = DisplayStyle.None;
        }

        errorPopupHideCoroutine = null;
    }

    private void OnErrorPopupOkClicked(ClickEvent evt)
    {
        HideErrorPopup();
    }
}
