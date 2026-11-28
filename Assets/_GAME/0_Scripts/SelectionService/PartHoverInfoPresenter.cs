using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public sealed class PartHoverInfoPresenter : IInitializable, IDisposable
{
    private readonly IEventBus _bus;
    private readonly IPartConfigRepository _configs;
    private readonly PartHoverInfoView _prefab;
    private readonly Transform _viewer;
    private readonly Scene _scene;
    private readonly List<PartInfoHoverEvent> _hovered = new List<PartInfoHoverEvent>(2);
    private PartHoverInfoView _view;
    private IDisposable _subscription;

    public PartHoverInfoPresenter(IEventBus bus, IPartConfigRepository configs,
        PartHoverInfoView prefab, Transform viewer, Scene scene)
    { _bus = bus; _configs = configs; _prefab = prefab; _viewer = viewer; _scene = scene; }

    public void Initialize()
    {
        _view = UnityEngine.Object.Instantiate(_prefab);
        SceneManager.MoveGameObjectToScene(_view.gameObject, _scene);
        _view.Hide();
        _subscription = _bus.Subscribe<PartInfoHoverEvent>(OnHover);
    }

    private void OnHover(PartInfoHoverEvent request)
    {
        for (int i = _hovered.Count - 1; i >= 0; i--)
            if (ReferenceEquals(_hovered[i].Source, request.Source)) _hovered.RemoveAt(i);
        if (request.IsHovered) _hovered.Add(request);
        for (int i = _hovered.Count - 1; i >= 0; i--)
        {
            var hover = _hovered[i];
            if (hover.PreviewAnchor != null && hover.PreviewAnchor.gameObject.activeInHierarchy && !string.IsNullOrEmpty(hover.ConfigId))
            { _view.Show(_configs.Get(hover.ConfigId), hover.PreviewAnchor, _viewer); return; }
        }
        _view.Hide();
    }

    public void Dispose()
    {
        _subscription?.Dispose(); _hovered.Clear();
        if (_view != null) UnityEngine.Object.Destroy(_view.gameObject);
    }
}
