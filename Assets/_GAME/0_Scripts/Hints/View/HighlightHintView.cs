using UnityEngine;
using Zenject;

public class HighlightHintView:MonoBehaviour
{
    [Inject] private readonly PartViewRegistry _repo;

    private IHighlightable? _currentHighlightable;


    public void Show(HintInfo hint)
    {
        Hide();

        if (hint.PartTransform == null)
            return;


        // допустим InstanceID лежит в hint.TargetData
        if (hint.PartTransform.TryGetComponent<IHighlightable>(out var view))
        {
            _currentHighlightable = view;

            _currentHighlightable.SetHintHighlighted(true);
        }
    }

    public void Hide()
    {
        var previous = _currentHighlightable;
        _currentHighlightable = null;
        if (previous == null || (previous is Object obj && obj == null))
            return;
        previous.SetHintHighlighted(false);

    }
}
