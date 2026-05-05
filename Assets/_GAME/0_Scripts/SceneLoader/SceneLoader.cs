using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

/// <summary>
/// Реализация SceneLoader с предзагрузкой
/// </summary>
public class SceneLoader : ISceneLoader
{
    private readonly IAppLogger _logger;
    private readonly IEventBus _eventBus;
    private AsyncOperation _pendingLoadOperation;
    private string _pendingSceneName;
    private bool _isDisposed;

    [Inject]
    public SceneLoader(IAppLogger logger, IEventBus eventBus)
    {
        _logger = logger;
        _eventBus = eventBus;
    }

    public void Initialize()
    {
        //_eventBus.Subscribe<RequestScenePreloadEvent>(PreloadScene());
        //_eventBus.Subscribe<AppStateChangedEvent>(
        //    handler: OnFlightStateChanged,
        //    filter: e =>
        //        e.NewState == AppState.FlightSimulation ||
        //        e.PreviousState == AppState.FlightSimulation,
        //    priority: EventPriority.Normal
        //);


        _eventBus.Subscribe<RequestPreloadSceneEvent>(
           handler: PreloadScene,
           filter: e =>
               e.TargetSceneName == "MainScene",
           priority: EventPriority.Low
       );

        _logger.Log("[ScenePreloader] Initialized");

    }

    private async void PreloadScene(RequestPreloadSceneEvent @event)
    {
        await PreloadSceneAsync(@event.TargetSceneName,OnScenePreloaded );
    }

    private void OnScenePreloaded()
    {
        _logger.Log($"Preloaded MainScene by subscribtion {this}");
        var scenePreloadEvent = new ScenePreloadedEvent()
        {
            Timestamp = DateTime.UtcNow,
            PreloadedSceneName = "MainScene"
        };

        _eventBus.Publish(scenePreloadEvent);
    }

    /// <summary>
    /// Предзагружает сцену асинхронно для последующей активации.
    /// </summary>
    /// <param name="sceneName">Имя сцены для загрузки.</param>
    /// <param name="callback">Метод обратного вызова, который будет выполнен после успешной загрузки сцены.</param>
    /// <param name="mode">Режим загрузки сцены (по умолчанию: <see cref="LoadSceneMode.Single"/>).</param>
    /// <returns>Задача, представляющая асинхронную операцию предзагрузки.</returns>
    public async Task PreloadSceneAsync(string sceneName, Action callback, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (_pendingLoadOperation != null && !_pendingLoadOperation.isDone)
        {
            _logger.LogWarning($"Already preloading: {_pendingSceneName}");
            return;
        }

        _logger.Log($"Starting preload of: {sceneName}");

        _pendingSceneName = sceneName;
        _pendingLoadOperation = SceneManager.LoadSceneAsync(sceneName, mode);
        _pendingLoadOperation.allowSceneActivation = false;
        //_pendingLoadOperation.completed += callback;
        // Мониторим прогресс
        float lastProgress = 0f;
        while (_pendingLoadOperation.progress < 0.9f)
        {
            float currentProgress = _pendingLoadOperation.progress;
            if (currentProgress > lastProgress + 0.1f)
            {
                _logger.Log($"Preload: {currentProgress * 100:F0}%");
                lastProgress = currentProgress;
            }
            await Task.Yield();
        }
        callback.Invoke();

        _logger.Log($"Scene '{sceneName}' preloaded (ready to activate)"); 
        //var scene = SceneManager.GetSceneByName(sceneName);
        //if (scene.IsValid())
        //{
        //    DisableAllSceneObjects(scene);
        //    _logger.Log($"Scene '{sceneName}' preloaded and disabled");
        //}
    }

    /// <summary>
    /// Отключить все объекты в сцене сразу после загрузки
    /// </summary>
    private void DisableAllSceneObjects(Scene scene)
    {
        var rootObjects = scene.GetRootGameObjects();

        foreach (var root in rootObjects)
        {
            // SetActive(false) вызовет OnDisable() на компонентах
            root.SetActive(false);

        }

        _logger.Log($"Disabled {rootObjects.Length} root objects in scene '{scene.name}'");
    }


    /// <summary>
    /// Активировать предзагруженную сцену
    /// </summary>
    public async Task ActivatePreloadedScene()
    {
        if (_pendingLoadOperation == null)
        {
            _logger.LogError("No scene to activate");
            return;
        }

        if (_pendingLoadOperation.isDone)
        {
            _logger.LogWarning("Scene is already activated");
            return;
        }

        _logger.Log($"Activating: {_pendingSceneName}");

        // 1. Разрешаем активацию
        _pendingLoadOperation.allowSceneActivation = true;

        // 2. Ждем завершения
        while (!_pendingLoadOperation.isDone)
        {
            await Task.Yield();
        }

        // 3. Получаем и активируем сцену
        var scene = SceneManager.GetSceneByName(_pendingSceneName);
        if (scene.IsValid())
        {
            SceneManager.SetActiveScene(scene);
            _logger.Log($"Scene '{_pendingSceneName}' activated");
        }

        // 4. Очищаем
        _pendingLoadOperation = null;
        _pendingSceneName = null;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _logger.Log("[SceneLoader] Disposing...");
        _pendingLoadOperation = null;
        _pendingSceneName = null;
        _isDisposed = true;
        _logger.Log("[SceneLoader] Disposed");
    }
}