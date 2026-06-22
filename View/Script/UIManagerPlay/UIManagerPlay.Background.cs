using UnityEngine.UIElements;

public partial class UIManagerPlay
{
    // Choice background switching and fade overlay.
    private void SetChoiceBackground(string choiceId)
    {
        if (mainContainer == null)
        {
            return;
        }

        if (!ChoiceBackgroundClasses.TryGetValue(choiceId, out string backgroundClass))
        {
            backgroundClass = "background-city";
        }

        if (activeBackgroundClass == backgroundClass)
        {
            return;
        }

        activeBackgroundClass = backgroundClass;
        backgroundTransitionVersion++;

        if (backgroundTransitionOverlay == null)
        {
            ApplyBackgroundClass(mainContainer, backgroundClass);
            return;
        }

        int transitionVersion = backgroundTransitionVersion;

        backgroundTransitionOverlay.RemoveFromClassList("show-background-transition");
        ApplyBackgroundClass(backgroundTransitionOverlay, backgroundClass);
        backgroundTransitionOverlay.style.display = DisplayStyle.Flex;

        backgroundTransitionOverlay.schedule.Execute(() =>
        {
            if (transitionVersion == backgroundTransitionVersion)
            {
                backgroundTransitionOverlay.AddToClassList("show-background-transition");
            }
        }).StartingIn(1);

        backgroundTransitionOverlay.schedule.Execute(() =>
        {
            if (transitionVersion != backgroundTransitionVersion)
            {
                return;
            }

            ApplyBackgroundClass(mainContainer, backgroundClass);
            backgroundTransitionOverlay.RemoveFromClassList("show-background-transition");
            backgroundTransitionOverlay.style.display = DisplayStyle.None;
        }).StartingIn(BackgroundTransitionDurationMs);
    }

    private void ApplyBackgroundClass(VisualElement element, string backgroundClass)
    {
        foreach (string className in BackgroundClasses)
        {
            element.RemoveFromClassList(className);
        }

        element.AddToClassList(backgroundClass);
    }
}
