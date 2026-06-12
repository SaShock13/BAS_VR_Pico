using System;
using UnityEngine;

public sealed class HintService : IHintService
{
    public event Action<HintInfo> HintShown;

    public event Action HintHidden;

    public HintInfo? CurrentHint { get; private set; }

    public void Show(HintInfo hint)
    {


        Debug.Log($"HintService Show Type {hint.VisualType}");
        CurrentHint = hint;

        HintShown?.Invoke(hint);
    }

    public void Hide()
    {
        CurrentHint = null;

        HintHidden?.Invoke();
    }
}