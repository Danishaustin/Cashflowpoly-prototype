using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManager
{
    // General home menu commands.
    private void OnEditRulesetClicked(ClickEvent evt)
    {
        Debug.Log("Edit Ruleset button clicked!");
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
