using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManager
{
    // Home overlay panels that are not part of login/profile.
    private void ResetHomeNavigationState()
    {
        addPlayerContainer?.RemoveFromClassList("show-add-player");
        editContainer?.RemoveFromClassList("show-edit");
        editContainer?.RemoveFromClassList("hide-edit-left");
        editPemilihanNarasiContainer?.RemoveFromClassList("show-edit-narasi-select");
        editPemilihanNarasiContainer?.RemoveFromClassList("hide-edit-narasi-select-left");
        editNarasiContainer?.RemoveFromClassList("show-edit-narasi");
        ResetEditQuestNavigationState();
        loginContainer?.RemoveFromClassList("show-login");
        sessionSetupContainer?.RemoveFromClassList("show-session-setup");
        sessionSetupContainer?.RemoveFromClassList("hide-session-setup-left");
        playContainer?.RemoveFromClassList("show-play");

        if (playValidationText != null)
        {
            playValidationText.text = string.Empty;
        }

        if (sessionSetupValidationText != null)
        {
            sessionSetupValidationText.text = string.Empty;
        }

        HidePlayerSuggestions();
        SetHomeButtonsEnabled(true);
    }

    private void OnAddPlayerClicked(ClickEvent evt)
    {
        Debug.Log("Add Player button clicked!");
        if (addPlayerValidationText != null)
        {
            addPlayerValidationText.text = string.Empty;
        }

        addPlayerContainer.style.display = DisplayStyle.Flex;
        addPlayerContainer.AddToClassList("show-add-player");
    }

    private void OnAddPlayerTransitionEnd(TransitionEndEvent evt)
    {
        // Keep the container active while its animation classes handle visibility.
    }

    private void OnLoginTransitionEnd(TransitionEndEvent evt)
    {
        // Reserved for hiding login container after close animation if needed later.
    }
}
