using System;
using System.Net.Sockets;
using Unity.Android.Gradle;
using Unity.Burst.CompilerServices;
using UnityEngine;
using Zenject;

public sealed class HintScenarioController :
    IHintScenarioController,
    ITickable
{
    private readonly IHintService _hintService;
    private readonly HintScenarioDefinition _definition;
    private readonly IUserActivityService _activityService;

    [Inject] private PartViewRegistry _viewRegistry;
    [Inject] private Clean_AssemblySystem _AssemblySystem;

    private HintContext _context;

    private bool _active;

    private bool _screenShown;
    private bool _worldShown;
    private bool _highlightShown;
    private bool _arrowShown;

    public HintScenarioController(
        IHintService hintService,
        HintScenarioDefinition definition,
        IUserActivityService activityService)
    {
        _hintService = hintService;
        _definition = definition;
        _activityService = activityService;
    }

    public void StartScenario(HintContext context)
    {
        StopScenario();

        _context = context;

        _active = true;

        _screenShown = false;
        _worldShown = false;
        _highlightShown = false;
        _arrowShown = false;
    }

    public void StopScenario()
    {
        _active = false;

        _hintService.Hide();
    }

    public void Tick()
    {
        if (!_active)
            return;

        float idleTime = _activityService.IdleTime;

        if (!_screenShown &&
            idleTime >= _definition.ScreenHintDelay)
        {
            _screenShown = true;

            _hintService.Show(
                new HintInfo(
                    _context.HintText,
                    HintType.Guidance,
                    HintVisualType.ScreenText));
        }

        if (!_worldShown &&
            idleTime >= _definition.WorldHintDelay)
        {
            _worldShown = true;
            var partTransform = ResolvePart(_context.RequiredPartType);
            var socket = ResolveSocket(_context.RequiredSocketType);

            _hintService.Show(
                new HintInfo(
                    _context.HintText,
                    HintType.Guidance,
                    HintVisualType.WorldText,
                    partTransform,
                    socket));
        }

        if (!_highlightShown &&
            idleTime >= _definition.HighlightDelay)
        {
            _highlightShown = true;
            var partTransform = ResolvePart(_context.RequiredPartType);
            _hintService.Show(
                new HintInfo(
                    "",
                    HintType.Guidance,
                    HintVisualType.Highlight,
                    partTransform));
        }

        if (!_arrowShown &&
            idleTime >= _definition.ArrowDelay)
        {
            _arrowShown = true;
                       

            var socket = ResolveSocket(_context.RequiredSocketType);
            var partTransform = ResolvePart(_context.RequiredPartType);


            _hintService.Show(
                new HintInfo(
                    "",
                    HintType.Guidance,
                    HintVisualType.Arrow,
                    partTransform,
                    socket
                    ));
        }
    }

    private Transform ResolvePart(PartType? requiredPartType)
    {
        if (requiredPartType.HasValue)
        {
            var domain = _AssemblySystem.GetUninstalledPartByType(requiredPartType.Value);
            if (domain != null)
            {
                var view = _AssemblySystem.GetViewById(domain.InstanceId);
                if (view != null) { return view.transform; }
            }
        }
        return null; 
    }

    private Transform ResolveSocket(PartType? type)
    {

        foreach (var viewObj in _viewRegistry.GetAllViews())
        {
            if (viewObj == null)
                continue;

            if (!viewObj.TryGetComponent<DronePartView>(out var partView))
                continue;
            SocketView socket = null;
            if (type.HasValue)
            {
                socket = partView.GetSocketByType(type.Value);
            }

            if (socket != null)
                return socket.transform;
        }

        return null;
    }
}