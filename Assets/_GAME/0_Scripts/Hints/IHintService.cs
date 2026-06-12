using System;

public interface IHintService
{
    event Action<HintInfo> HintShown;

    event Action HintHidden;

    HintInfo? CurrentHint { get; }

    void Show(HintInfo hint);

    void Hide();
}