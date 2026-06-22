using UnityEngine;
using UnityEngine.UIElements;

public static class UIAspectRatioUtility
{
    private const float BaseWidth = 1920f;
    private const float BaseHeight = 1080f;

    public static void ApplyResponsiveScale(VisualElement root, VisualElement content)
    {
        if (root == null || content == null)
        {
            return;
        }

        root.style.backgroundColor = Color.clear;
        content.style.position = Position.Absolute;
        content.style.flexGrow = 0;
        content.style.width = BaseWidth;
        content.style.height = BaseHeight;
        content.style.left = 0;
        content.style.top = 0;
        content.style.transformOrigin = new TransformOrigin(0, 0, 0);

        ScaleToDevice(root, content);
        root.RegisterCallback<GeometryChangedEvent>(_ => ScaleToDevice(root, content));
    }

    public static void Apply16By9(VisualElement root, VisualElement content)
    {
        ApplyResponsiveScale(root, content);
    }

    private static void ScaleToDevice(VisualElement root, VisualElement content)
    {
        float rootWidth = root.resolvedStyle.width;
        float rootHeight = root.resolvedStyle.height;

        if (rootWidth <= 0f || rootHeight <= 0f)
        {
            root.schedule.Execute(() => ScaleToDevice(root, content)).StartingIn(1);
            return;
        }

        float scaleX = rootWidth / BaseWidth;
        float scaleY = rootHeight / BaseHeight;

        content.style.scale = new StyleScale(new Scale(new Vector3(scaleX, scaleY, 1f)));
    }
}
