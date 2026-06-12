using UnityEngine;

public readonly struct HintInfo
{
    public readonly string Text;

    public readonly HintType Type;

    public readonly HintVisualType VisualType;

    public readonly Transform PartTransform;

    public readonly Transform SoketTransform;

    public readonly float Duration;

    public HintInfo(
        string text,
        HintType type,
        HintVisualType visualType,
        Transform partTransform = null,
        Transform soketTransform = null,
        float duration = 5f )
    {
        Text = text;
        Type = type;
        VisualType = visualType;
        PartTransform = partTransform;
        SoketTransform = soketTransform;
        Duration = duration;
    }
}