using UnityEngine;
using UnityEngine.Playables;
using Zenject;
using static UnityEngine.GraphicsBuffer;

public sealed class HintPresenter : MonoBehaviour
{
    [SerializeField]
    private ScreenHintView _screenView;

    [SerializeField]
    private HighlightHintView _highlightView;




    private IHintService _hintService;


    [SerializeField]
    private WorldHintView _worldViewPrefab;

    private WorldHintView _worldView;

    [SerializeField]
    private ArrowHintView _arrowViewPrefab;

    private ArrowHintView _arrowView;


    [Inject]
    public void Construct(
        IHintService hintService)
    {
        _hintService = hintService;
    }

    private void Awake()
    {
        _hintService.HintShown += OnHintShown;
        _hintService.HintHidden += OnHintHidden;
    }

    private void OnDestroy()
    {
        _hintService.HintShown -= OnHintShown;
        _hintService.HintHidden -= OnHintHidden;
    }

    private void OnHintShown(
        HintInfo hint)
    {
        HideAll();
        
        switch (hint.VisualType)
        {
            case HintVisualType.ScreenText:

                Debug.Log($"OnHintShown HintVisualType.ScreenText {this}");
                _screenView.Show(hint);
                break;

            case HintVisualType.WorldText:


                if (_worldViewPrefab != null)
                {
                    _worldView = Instantiate( _worldViewPrefab);
                    _worldView.Show(hint);
                }
                break;

            case HintVisualType.Highlight:

                ShowHighlight(hint);
                break;

            case HintVisualType.Arrow:
                if (_arrowViewPrefab != null)
                {
                    ShowArrow(hint);
                    ShowHighlight(hint);
                }
                break;
        }
    }

    private void ShowHighlight(HintInfo hint)
    {
        if (_highlightView != null) _highlightView.Show(hint);
    }

    private void ShowArrow(HintInfo hint)
    {
        _arrowView = Instantiate(_arrowViewPrefab);
        _arrowView.Show(hint);
    }

    private void OnHintHidden()
    {
        HideAll();
    }

    private void HideAll()
    {
        if(_screenView!= null)_screenView.Hide();
        if (_worldView != null) _worldView.Hide();
        if (_highlightView != null) _highlightView.Hide();
        if (_arrowView != null) _arrowView.Hide();
    }
}