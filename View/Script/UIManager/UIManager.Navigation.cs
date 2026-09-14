using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManager
{
    // General home menu commands.
    private void OnEditClicked(ClickEvent evt)
    {
        Debug.Log("Edit button clicked!");
        editContainer?.AddToClassList("show-edit");
    }

    private void OnBackEditClicked(ClickEvent evt)
    {
        Debug.Log("Back Edit button clicked!");
        editContainer?.RemoveFromClassList("show-edit");
        editContainer?.RemoveFromClassList("hide-edit-left");
        editPemilihanNarasiContainer?.RemoveFromClassList("show-edit-narasi-select");
        editPemilihanNarasiContainer?.RemoveFromClassList("hide-edit-narasi-select-left");
        editNarasiContainer?.RemoveFromClassList("show-edit-narasi");
    }

    private async void OnEditNarasiClicked(ClickEvent evt)
    {
        Debug.Log("Edit Narasi button clicked!");
        if (isEditNarasiLoading)
        {
            return;
        }

        NarasiPackRepository.ClearLastCloudWarning();
        BeginEditNarasiLoading("Memuat daftar narasi...");

        try
        {
            await SetupEditNarasiPackSelectionAsync();
            editContainer?.AddToClassList("hide-edit-left");
            editPemilihanNarasiContainer?.RemoveFromClassList("hide-edit-narasi-select-left");
            editPemilihanNarasiContainer?.AddToClassList("show-edit-narasi-select");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Gagal membuka menu edit narasi: " + ex.Message);
            ShowErrorPopup("Gagal membuka menu edit narasi. Detail: " + ex.Message);
        }
        finally
        {
            EndEditNarasiLoading();
        }

        ShowEditNarasiCloudWarningIfNeeded();
    }

    private void OnBackEditPemilihanNarasiClicked(ClickEvent evt)
    {
        Debug.Log("Back Edit Pemilihan Narasi clicked!");
        editPemilihanNarasiContainer?.RemoveFromClassList("show-edit-narasi-select");
        editPemilihanNarasiContainer?.RemoveFromClassList("hide-edit-narasi-select-left");
        editContainer?.RemoveFromClassList("hide-edit-left");
        editContainer?.AddToClassList("show-edit");
    }

    private async void OnNextEditPemilihanNarasiClicked(ClickEvent evt)
    {
        Debug.Log("Next Edit Pemilihan Narasi clicked!");
        if (isEditNarasiLoading)
        {
            return;
        }

        NarasiPackRepository.ClearLastCloudWarning();
        BeginEditNarasiLoading("Memuat isi narasi...");

        try
        {
            if (!await SelectNarasiPackForEditingAsync())
            {
                return;
            }

            editPemilihanNarasiContainer?.AddToClassList("hide-edit-narasi-select-left");
            editNarasiContainer?.AddToClassList("show-edit-narasi");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Gagal memuat isi narasi: " + ex.Message);
            ShowErrorPopup("Gagal memuat isi narasi. Detail: " + ex.Message);
        }
        finally
        {
            EndEditNarasiLoading();
        }

        ShowEditNarasiCloudWarningIfNeeded();
    }

    private async void OnBackEditNarasiClicked(ClickEvent evt)
    {
        Debug.Log("Back Edit Narasi button clicked!");
        if (isEditNarasiLoading)
        {
            return;
        }

        NarasiPackRepository.ClearLastCloudWarning();
        BeginEditNarasiLoading("Memuat ulang daftar narasi...");

        try
        {
            await SetupEditNarasiPackSelectionAsync();
            editNarasiContainer?.RemoveFromClassList("show-edit-narasi");
            editPemilihanNarasiContainer?.RemoveFromClassList("hide-edit-narasi-select-left");
            editPemilihanNarasiContainer?.AddToClassList("show-edit-narasi-select");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Gagal kembali ke daftar narasi: " + ex.Message);
            ShowErrorPopup("Gagal kembali ke daftar narasi. Detail: " + ex.Message);
        }
        finally
        {
            EndEditNarasiLoading();
        }

        ShowEditNarasiCloudWarningIfNeeded();
    }

    private void OnEditNarasiNewClicked(ClickEvent evt)
    {
        Debug.Log("Edit Narasi New clicked!");
        BeginNewEditNarasiDialog();
    }

    private async void OnEditNarasiDeleteClicked(ClickEvent evt)
    {
        Debug.Log("Edit Narasi Delete clicked!");
        if (isEditNarasiLoading)
        {
            return;
        }

        NarasiPackRepository.ClearLastCloudWarning();
        BeginEditNarasiLoading("Menghapus dialog...");

        try
        {
            await DeleteSelectedEditNarasiDialogAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Gagal menghapus dialog narasi: " + ex.Message);
            ShowErrorPopup("Gagal menghapus dialog narasi. Detail: " + ex.Message);
        }
        finally
        {
            EndEditNarasiLoading();
        }
    }

    private void OnEditNarasiAddLineClicked(ClickEvent evt)
    {
        Debug.Log("Edit Narasi Add Line clicked!");
        AddEditNarasiLineInput();
    }

    private async void OnEditNarasiPackNewClicked(ClickEvent evt)
    {
        Debug.Log("Edit Narasi Pack New clicked!");
        if (isEditNarasiLoading)
        {
            return;
        }

        NarasiPackRepository.ClearLastCloudWarning();
        BeginEditNarasiLoading("Membuat paket narasi...");

        try
        {
            await CreateNewNarasiPackAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Gagal membuat paket narasi: " + ex.Message);
            ShowErrorPopup("Gagal membuat paket narasi. Detail: " + ex.Message);
        }
        finally
        {
            EndEditNarasiLoading();
        }
    }

    private async void OnEditNarasiPackDeleteClicked(ClickEvent evt)
    {
        Debug.Log("Edit Narasi Pack Delete clicked!");
        if (isEditNarasiLoading)
        {
            return;
        }

        NarasiPackRepository.ClearLastCloudWarning();
        BeginEditNarasiLoading("Menghapus paket narasi...");

        try
        {
            await DeleteSelectedNarasiPackAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Gagal menghapus paket narasi: " + ex.Message);
            ShowErrorPopup("Gagal menghapus paket narasi. Detail: " + ex.Message);
        }
        finally
        {
            EndEditNarasiLoading();
        }
    }

    private void OnEditNarasiResetClicked(ClickEvent evt)
    {
        Debug.Log("Edit Narasi Reset clicked!");
        ResetEditNarasiForm();
    }

    private async void OnEditNarasiSaveClicked(ClickEvent evt)
    {
        Debug.Log("Edit Narasi Save clicked!");
        if (isEditNarasiLoading)
        {
            return;
        }

        NarasiPackRepository.ClearLastCloudWarning();
        BeginEditNarasiLoading("Menyimpan narasi...");

        try
        {
            await SaveEditNarasiFormAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Gagal menyimpan narasi: " + ex.Message);
            ShowErrorPopup("Gagal menyimpan narasi. Detail: " + ex.Message);
        }
        finally
        {
            EndEditNarasiLoading();
        }
    }

    private void OnEditAssetClicked(ClickEvent evt)
    {
        Debug.Log("Edit Asset button clicked!");
        ShowSuccessPopup("Menu Edit Asset siap dikembangkan.");
    }

    private void OnExitClicked(ClickEvent evt)
    {
        Debug.Log("Exit button clicked!");
        Application.Quit();
    }

    private void OnBackClicked(ClickEvent evt, VisualElement container, string className)
    {
        Debug.Log("Back button clicked!");
        container.RemoveFromClassList(className);
    }
}



