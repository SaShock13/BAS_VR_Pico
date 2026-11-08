using System;
using UnityEngine;
using Zenject;

public sealed class HintScenarioController : IHintScenarioController, ITickable, IInitializable, IDisposable
{
    private readonly IHintService _hintService;
    private readonly HintScenarioDefinition _definition;
    private readonly IUserActivityService _activityService;
    private readonly IEventBus _eventBus;
    [Inject] private PartViewRegistry _viewRegistry;
    [Inject] private Clean_AssemblySystem _AssemblySystem;
    private HintContext _context;
    private bool _active;
    private bool _targetsDirty;
    private Transform _part;
    private Transform _socket;
    private bool _hadPart;
    private bool _hadSocket;
    private bool _canHighlight;
    private HintVisualType? _shownVisual;
    private IDisposable _createdSubscription;
    private IDisposable _deletedSubscription;
    private IDisposable _changedSubscription;
    private IDisposable _rebuiltSubscription;

    public HintScenarioController(IHintService hintService, HintScenarioDefinition definition,
        IUserActivityService activityService, IEventBus eventBus)
    {
        _hintService = hintService;
        _definition = definition;
        _activityService = activityService;
        _eventBus = eventBus;
    }

    public void Initialize()
    {
        _createdSubscription = _eventBus.Subscribe<Clean_PartCreatedEvent>(_ => _targetsDirty = true);
        _deletedSubscription = _eventBus.Subscribe<Clean_PartDeletedEvent>(_ => _targetsDirty = true);
        _changedSubscription = _eventBus.Subscribe<AssemblyChangedEvent>(_ => _targetsDirty = true);
        _rebuiltSubscription = _eventBus.Subscribe<DronesRebuiltEvent>(_ => _targetsDirty = true);
    }

    public void StartScenario(HintContext context)
    {
        StopScenario();
        _context = context;
        _active = true;
        _targetsDirty = true;
    }

    public void StopScenario()
    {
        _active = false;
        _part = null;
        _socket = null;
        _hadPart = _hadSocket = false;
        _shownVisual = null;
        _hintService.Hide();
    }

    public void Tick()
    {
        if (!_active)
            return;
        // No scene searches each frame: refresh after events or destruction of a cached target.
        if (_targetsDirty || (_hadPart && _part == null) || (_hadSocket && _socket == null))
        {
            _targetsDirty = false;
            _part = _context.Target != null ? _context.Target : ResolvePart(_context.RequiredPartType);
            _socket = _context.PointAtTarget ? _context.Target : ResolveSocket(_context.RequiredSocketType);
            _canHighlight = _part != null && _part.TryGetComponent<IHighlightable>(out _);
            _hadPart = _part != null;
            _hadSocket = _socket != null;
            _shownVisual = null;
            _hintService.Hide();
        }
        float idleTime = _activityService.IdleTime;
        HintVisualType? visual = null;
        if (_socket != null && idleTime >= _definition.ArrowDelay)
            visual = HintVisualType.Arrow;
        else if (_part != null && _canHighlight && idleTime >= _definition.HighlightDelay)
            visual = HintVisualType.Highlight;
        else if ((_part != null || _socket != null) && idleTime >= _definition.WorldHintDelay)
            visual = HintVisualType.WorldText;
        else if (idleTime >= _definition.ScreenHintDelay)
            visual = HintVisualType.ScreenText;
        if (!visual.HasValue || visual == _shownVisual)
            return;
        _hintService.Show(new HintInfo(_context.HintText, HintType.Guidance, visual.Value, _part, _socket));
        _shownVisual = visual;
    }

    private Transform ResolvePart(PartType? type)
    {
        if (!type.HasValue)
            return null;
        var domain = _AssemblySystem.GetUninstalledPartByType(type.Value);
        if (domain == null)
            return null;
        var view = _AssemblySystem.GetViewById(domain.InstanceId);
        return view != null ? view.transform : null;
    }

    private Transform ResolveSocket(PartType? type)
    {
        if (!type.HasValue)
            return null;
        foreach (var go in _viewRegistry.GetAllViews())
        {
            if (go == null || !go.TryGetComponent<DronePartView>(out var view))
                continue;
            var socket = view.GetSocketByType(type.Value);
            if (socket != null)
                return socket.transform;
        }
        return null;
    }

    public void Dispose()
    {
        _createdSubscription?.Dispose();
        _deletedSubscription?.Dispose();
        _changedSubscription?.Dispose();
        _rebuiltSubscription?.Dispose();
        StopScenario();
    }
}
