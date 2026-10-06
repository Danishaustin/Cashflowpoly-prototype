using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManager
{
    private const float ErrorPopupHideDelaySeconds = 0.2f;

    // Aksi yang menunggu konfirmasi; dibersihkan tiap popup ditutup supaya tidak jalan belakangan.
    private System.Action pendingConfirmAction;

    private void ShowErrorPopup(string message)
    {
        ShowStatusPopup("Terjadi Kesalahan", message, false);
    }

    private void ShowSuccessPopup(string message)
    {
        ShowStatusPopup("Berhasil", message, true);
    }

    // Konfirmasi memakai kartu popup yang sama; isinya dibangun ulang tiap kali, jadi tidak perlu elemen
    // baru di Home.uxml. Aksinya disimpan dulu dan baru dijalankan sesudah popup ditutup.
    private void ShowConfirmPopup(string title, string message, string confirmText, System.Action onConfirm)
    {
        if (errorPopupOverlay == null || errorPopupCard == null)
        {
            // Lebih aman membatalkan daripada menghapus tanpa konfirmasi.
            Debug.LogWarning("Popup konfirmasi tidak tersedia, penghapusan dibatalkan.");
            return;
        }

        if (errorPopupHideCoroutine != null)
        {
            StopCoroutine(errorPopupHideCoroutine);
            errorPopupHideCoroutine = null;
        }

        pendingConfirmAction = onConfirm;

        string safeTitle = string.IsNullOrWhiteSpace(title) ? "Konfirmasi" : title;
        string safeMessage = string.IsNullOrWhiteSpace(message) ? "Lanjutkan tindakan ini?" : message;
        string safeConfirmText = string.IsNullOrWhiteSpace(confirmText) ? "Lanjutkan" : confirmText;

        RebuildConfirmPopupContent(safeTitle, safeMessage, safeConfirmText);

        errorPopupOverlay.style.display = DisplayStyle.Flex;
        errorPopupOverlay.style.opacity = 1f;
        errorPopupCard.style.opacity = 1f;
        errorPopupOverlay.BringToFront();
        errorPopupCard.BringToFront();
        errorPopupOverlay.RemoveFromClassList("show-error-popup-overlay");
        errorPopupCard.RemoveFromClassList("show-error-popup");

        errorPopupOverlay.schedule.Execute(() =>
        {
            RebuildConfirmPopupContent(safeTitle, safeMessage, safeConfirmText);
            errorPopupOverlay.AddToClassList("show-error-popup-overlay");
            errorPopupCard.AddToClassList("show-error-popup");
        }).ExecuteLater(10);
    }

    private void RebuildConfirmPopupContent(string title, string message, string confirmText)
    {
        if (errorPopupCard == null)
        {
            return;
        }

        errorPopupCard.Clear();

        errorPopupTitle = new Label(title)
        {
            name = "ErrorPopupTitle"
        };
        errorPopupTitle.AddToClassList("error-popup-title");
        ApplyStatusPopupLabelStyle(errorPopupTitle, new Color(255f / 255f, 210f / 255f, 90f / 255f), 48, 72);

        errorPopupMessage = new Label(message)
        {
            name = "ErrorPopupMessage"
        };
        errorPopupMessage.AddToClassList("error-popup-message");
        ApplyStatusPopupLabelStyle(errorPopupMessage, Color.white, 32, 110);
        errorPopupMessage.style.whiteSpace = WhiteSpace.Normal;

        VisualElement buttonRow = new VisualElement
        {
            name = "ErrorPopupConfirmButtonRow"
        };
        buttonRow.style.flexDirection = FlexDirection.Row;
        buttonRow.style.flexWrap = Wrap.Wrap;
        buttonRow.style.justifyContent = Justify.Center;
        buttonRow.style.alignItems = Align.Center;
        buttonRow.style.width = Length.Percent(100);

        buttonRow.Add(BuildConfirmPopupButton("ErrorPopupConfirmButton", confirmText, ConfirmPendingPopupAction));
        buttonRow.Add(BuildConfirmPopupButton("ErrorPopupCancelButton", "Batal", HideErrorPopup));

        errorPopupCard.Add(errorPopupTitle);
        errorPopupCard.Add(errorPopupMessage);
        errorPopupCard.Add(buttonRow);
    }

    // Dua tombol harus muat berdampingan di dalam kartu selebar 560 px dengan padding 38 px.
    private Button BuildConfirmPopupButton(string name, string text, System.Action onClick)
    {
        Button button = new Button(onClick)
        {
            name = name,
            text = text
        };
        button.AddToClassList("home-button");
        button.AddToClassList("error-popup-button");
        button.style.width = 210;
        button.style.marginLeft = 8;
        button.style.marginRight = 8;
        button.style.display = DisplayStyle.Flex;
        button.style.opacity = 1f;
        return button;
    }

    private void ConfirmPendingPopupAction()
    {
        System.Action action = pendingConfirmAction;
        pendingConfirmAction = null;
        HideErrorPopup();
        action?.Invoke();
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
        pendingConfirmAction = null;

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
