using System;

public partial class ChoiceController
{
    private Action pendingConfirmYes;
    private Action pendingConfirmNo;
    private bool isConfirmPending;

    // Panel konfirmasi dipakai bersama semua aksi: pesan berisi ringkasan aksi, lalu Ya mengirim dan Batal kembali.
    private void AskConfirmation(string message, Action onYes, Action onNo)
    {
        if (isConfirmPending)
        {
            return;
        }

        pendingConfirmYes = onYes;
        pendingConfirmNo = onNo;
        isConfirmPending = true;
        view.ShowActionConfirm(message);
    }

    private void HandleChoiceConfirm(string selectedChoice)
    {
        if (!isConfirmPending)
        {
            return;
        }

        Action onYes = pendingConfirmYes;
        Action onNo = pendingConfirmNo;

        switch (selectedChoice)
        {
            case "ChoiceConfirmYesButton":
                ClearPendingConfirmation();
                onYes?.Invoke();
                return;
            case "ChoiceConfirmNoButton":
                ClearPendingConfirmation();
                onNo?.Invoke();
                return;
        }
    }

    private void ClearPendingConfirmation()
    {
        isConfirmPending = false;
        pendingConfirmYes = null;
        pendingConfirmNo = null;
        view.HideActionConfirm();
    }
}
