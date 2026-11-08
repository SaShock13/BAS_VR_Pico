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
    private IDisposable _environmentSubscription;
    private readonly AssemblyEnvironmentCatalog _catalog;
    private bool _isLoading;
    private bool _disposed;

    public MenuNavigationService(IEventBus eventBus, ISceneLoader sceneLoader, IAppLogger logger,
        [InjectOptional] AssemblyEnvironmentCatalog catalog = null)
    {
        _eventBus = eventBus;
        _sceneLoader = sceneLoader;
        _logger = logger;
        _catalog = catalog;
    }

    public void Initialize()
    {
        _subscription = _eventBus.Subscribe<RequestStartConstructorEvent>(OnStartRequested);
        _environmentSubscription = _eventBus.Subscribe<ConfirmAssemblyEnvironmentRequest>(OnEnvironmentConfirmed);
    }

    private void OnStartRequested(RequestStartConstructorEvent request)
    {
        if (_isLoading || _disposed) return;
        if (_catalog == null) { LoadConstructor(); return; }
        _eventBus.Publish(new OpenAssemblyEnvironmentSelection());
    }

    private void OnEnvironmentConfirmed(ConfirmAssemblyEnvironmentRequest request)
    {
        if (_isLoading || _disposed || _catalog == null) return;
        var entry = _catalog.Find(request.EnvironmentId);
        if (entry == null || string.IsNullOrWhiteSpace(entry.id)
            || string.IsNullOrWhiteSpace(entry.scenePath)
            || !UnityEngine.Application.CanStreamedLevelBeLoaded(entry.scenePath)
            || entry.scenePath == "Assets/Scenes/ConstructorScene.unity")
        {
            _eventBus.Publish(new MenuNavigationStatusEvent { Error = "Локация сборки отсутствует в Build Profiles или настроена неверно" });
            return;
        }
        AssemblyEnvironmentSession.EnvironmentId = entry.id;
        LoadConstructor();
    }

    private async void LoadConstructor()
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
        _environmentSubscription?.Dispose();
    }
}
