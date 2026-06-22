using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManager
{
    // Profile popup and scrim behavior.
    private void OnProfileClicked(ClickEvent evt)
    {
        Debug.Log("Profile button clicked!");
        profileContainer.style.display = DisplayStyle.Flex;
        profilePage.AddToClassList("show-profile");
        scrim.AddToClassList("show-scrim");
    }

    private void OnScrimClicked(ClickEvent evt)
    {
        Debug.Log("Scrim clicked!");
        profilePage.RemoveFromClassList("show-profile");
        scrim.RemoveFromClassList("show-scrim");
        profileContainer.style.display = DisplayStyle.None;
    }
}
