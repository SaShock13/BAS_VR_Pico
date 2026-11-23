using System;
using System.Collections.Generic;
using Zenject;

public class PartHighlightService : IInitializable, IDisposable
{
    private readonly AssemblyGrabModeService _grabMode;
    private readonly Dictionary<object, DronePartView> _hovered = new Dictionary<object, DronePartView>();
    private readonly HashSet<DronePartView> _highlighted = new HashSet<DronePartView>();
    private readonly HashSet<DronePartView> _desired = new HashSet<DronePartView>();
    private readonly List<DronePartView> _targets = new List<DronePartView>();

    public PartHighlightService(AssemblyGrabModeService grabMode) => _grabMode = grabMode;
    public void Initialize() => _grabMode.RoutesChanged += Refresh;

    public void Enter(DronePartView view, object source = null)
    {
        source ??= this;
        if (_hovered.TryGetValue(source, out var previous) && previous == view) return;
        _hovered[source] = view;
        Refresh();
    }

    public void Exit(object source = null)
    {
        if (_hovered.Remove(source ?? this)) Refresh();
    }

    private void Refresh()
    {
        _desired.Clear();
        foreach (var view in _hovered.Values)
        {
            _grabMode.GetHighlightTargets(view, _targets);
            foreach (var target in _targets) _desired.Add(target);
        }
        foreach (var view in _highlighted)
            if (view != null && !_desired.Contains(view)) view.Highlight(false);
        foreach (var view in _desired)
            if (view != null && !_highlighted.Contains(view)) view.Highlight(true);
        _highlighted.Clear();
        _highlighted.UnionWith(_desired);
    }

    public void Dispose()
    {
        _grabMode.RoutesChanged -= Refresh;
        _hovered.Clear();
        Refresh();
    }
}
