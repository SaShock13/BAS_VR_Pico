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
        if (_currentHighlightable == null)
            return;

        _currentHighlightable?.SetHintHighlighted(false);

    }
}