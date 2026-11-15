using UnityEngine;
using UnityEngine.UI;

public static class AmberUiTheme
{
    public static readonly Color Surface = new Color32(37, 25, 12, 224);
    public static readonly Color Gold = new Color32(255, 211, 112, 255);
    public static readonly Color Text = new Color32(255, 226, 162, 255);
    public static readonly Color Muted = new Color32(192, 154, 92, 255);

    public static void StyleControl(Selectable control)
    {
        if (control.targetGraphic == null)
            return;

        var colors = control.colors;
        colors.normalColor = new Color32(79, 54, 22, 255);
        colors.highlightedColor = new Color32(183, 129, 48, 255);
        colors.pressedColor = new Color32(118, 78, 26, 255);
        colors.selectedColor = new Color32(149, 102, 39, 255);
        colors.disabledColor = new Color32(55, 43, 25, 180);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        control.targetGraphic.color = Color.white;
        control.transition = Selectable.Transition.ColorTint;
        control.colors = colors;
    }
}
