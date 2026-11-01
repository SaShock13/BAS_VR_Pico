using System;
using Zenject;

// Scene-scoped: the constructor owns its own services after the Single scene load.
public sealed class MenuNavigationService : IInitializable, IDisposable
{
    public const string ConstructorSceneName = "ConstructorScene";
    private readonly IEventBus _eventBus;
    private readonly ISceneLoader _sceneLoader;
    private readonly IAppLogger _logger;
    private IDisposable _subscription;
    private bool _isLoading;
    private bool _disposed;

    public MenuNavigationService(IEventBus eventBus, ISceneLoader sceneLoader, IAppLogger logger)
    {
        _eventBus = eventBus;
        _sceneLoader = sceneLoader;
        _logger = logger;
    }

    public void Initialize()
    {
        _subscription = _eventBus.Subscribe<RequestStartConstructorEvent>(OnStartRequested);
    }

    private async void OnStartRequested(RequestStartConstructorEvent request)
    {
        if (_isLoading || _disposed) return;
        _isLoading = true;
        _eventBus.Publish(new MenuNavigationStatusEvent { IsLoading = true });
        try
        {
            await _sceneLoader.PreloadSceneAsync(ConstructorSceneName, null);
            if (!_disposed)
                await _sceneLoader.ActivatePreloadedScene();
        }
        catch (Exception exception)
        {
            _logger.LogError("[MenuNavigation] " + exception);
            if (!_disposed)
            {
                _isLoading = false;
                _eventBus.Publish(new MenuNavigationStatusEvent { Error = exception.Message });
            }
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _subscription?.Dispose();
    }
}
