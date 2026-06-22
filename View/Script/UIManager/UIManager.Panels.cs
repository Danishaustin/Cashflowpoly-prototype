using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManager
{
    // Home overlay panels that are not part of login/profile.
    private void OnQuestClicked(ClickEvent evt)
    {
        Debug.Log("Quest button clicked!");
        questContainer.style.display = DisplayStyle.Flex;
        questContainer.AddToClassList("show-quest");
    }

    private void OnQuestTransitionEnd(TransitionEndEvent evt)
    {
        // Keep the container active while its animation classes handle visibility.
    }

    private void OnLoginTransitionEnd(TransitionEndEvent evt)
    {
        // Reserved for hiding login container after close animation if needed later.
    }
}
