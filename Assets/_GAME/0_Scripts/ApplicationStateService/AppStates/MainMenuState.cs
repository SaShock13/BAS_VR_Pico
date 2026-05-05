using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Zenject;

/// <summary>
/// Состояние главного меню (упрощенная версия)
/// </summary>
public class MainMenuState : IAppState,  IDisposable
{
    // Зависимости
    private readonly IAppLogger _logger;
    private IEventBus _eventBus;
    private ISceneLoader _sceneLoader;
    private InputActionAsset _inputActions;

    // Ссылки на объекты сцены 
    private GameObject _menuCanvas;
    private GameObject _animatedDrone;
    private Camera _mainCamera;
    private Transform _xrOrigin;
    //private DroneAssemblyAnimation droneAimation;
    // Компоненты анимации
    private Animator _droneAnimator;
    private bool _isInitialized = false;

    // Свойства состояния
    public AppState StateId => AppState.MainMenu;
    public string StateName => "Главное меню";
    public bool CanBePaused => false;
    public int Priority => 10;

    /// <summary>
    /// Конструктор
    /// </summary>
    [Inject]
    public MainMenuState
        (
        IAppLogger logger,
        InputActionAsset inputActions,
        ISceneLoader sceneLoader,
        IEventBus eventBus
        )
    {
        _logger = logger;  
        _inputActions = inputActions;
        _sceneLoader = sceneLoader;
        _eventBus = eventBus;
    }

    /// <summary>
    /// Инициализация состояния
    /// </summary>
    public void StateInitialize()
    {
        if (_isInitialized) return;

        _logger.Log("[MainMenuState] Initializing...");

        try
        {
            // 1. Находим объекты сцены (имена должны совпадать с вашей сценой)
            FindSceneObjects();
            
            // 2. Настраиваем начальное состояние объектов
            SetupInitialState();

            _isInitialized = true;
            _logger.Log("[MainMenuState] Initialization complete");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[MainMenuState] Initialization failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Вход в состояние
    /// </summary>
    public async Task EnterAsync(StateTransitionContext context)
    {
        _logger.Log("[MainMenuState] Entering main menu");

        

        try
        {

            //var _pendingLoadOperation = SceneManager.LoadSceneAsync("MainScene", LoadSceneMode.Single);
            //_pendingLoadOperation.allowSceneActivation = false;
            // 1. Предзагрузка главной сцены конструктора
            //PreloadMainScene();

            //var musicEvent = new PlayMusicRequestedEvent { MusicId = SoundId.MenuBackgroundMusic, Loop = true };
            //_eventBus.Publish(musicEvent);
            


            // 2. Включаем анимированную модель дрона
            StartDroneAnimation();             

            // 3. Настраиваем XR Origin для меню (если нужно)
            //ConfigureXRForMenu();

            // 4. Настраиваем камеру/освещение для меню
            //ConfigureCameraAndLighting();

            // 5. Плавное появление (опционально)
            //await PerformEnterAnimationAsync();


            await Task.Delay(1000);
            //LogAllInputAssets();

            _logger.Log("[MainMenuState] Successfully entered main menu");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[MainMenuState] Failed to enter state: {ex.Message}");
            throw;
        }
    }

    private void PreloadMainScene()
    {
        _logger.Log("Preloading main Scene");

        LogAllInputAssets();


        foreach (var actionMap in _inputActions.actionMaps)
        {
            actionMap.Disable();

            Debug.Log($"actionMap  {actionMap.name} disabled by MainMenuState");
        }

        //_sceneLoader.PreloadSceneAsync("MainScene");

    }

    private void LogAllInputAssets()
    {
        // Находим все InputActionAsset в сцене
        var allAssets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
        _logger.Log($"[MainMenuState] Found {allAssets.Length} InputActionAsset(s):");

        foreach (var asset in allAssets)
        {
            _logger.Log($"  - {asset.name} ({asset.actionMaps.Count} maps)");
            foreach (var map in asset.actionMaps)
            {
                _logger.Log($"    * {map.name} (enabled: {map.enabled})");
            }
        }
    }



    /// <summary>
    /// Выход из состояния
    /// </summary>
    public async Task ExitAsync()
    {
        _logger.Log("[MainMenuState] Exiting main menu");

        try
        {
            // 1. Останавливаем анимацию дрона
            StopDroneAnimation();

            // 2. Плавное исчезновение (опционально)
            await PerformExitAnimationAsync();

            // 3. Отключаем объекты меню
            if (_menuCanvas != null)
            {
                _menuCanvas.SetActive(false);
            }

            if (_animatedDrone != null)
            {
                _animatedDrone.SetActive(false);
            }

            _logger.Log("[MainMenuState] Successfully exited main menu");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[MainMenuState] Error during exit: {ex.Message}");
        }
    }

       
    /// <summary>
    /// Обработка паузы
    /// </summary>
    public Task OnPauseAsync()
    {
        // При паузе можно приостановить анимацию
        PauseDroneAnimation();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Обработка возобновления
    /// </summary>
    public Task OnResumeAsync()
    {
        // При возобновлении продолжаем анимацию
        ResumeDroneAnimation();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Освобождение ресурсов
    /// </summary>
    public void Dispose()
    {
        _logger.Log("[MainMenuState] Disposing...");

        // Очищаем ссылки
        _menuCanvas = null;
        _animatedDrone = null;
        _droneAnimator = null;
        _mainCamera = null;
        _xrOrigin = null;

        _isInitialized = false;
        _logger.Log("[MainMenuState] Disposed");
    }

    #region Приватные методы

    /// <summary>
    /// Поиск объектов на сцене
    /// </summary>
    private void FindSceneObjects()
    {
        //droneAimation = GameObject.FindFirstObjectByType<DroneAssemblyAnimation>();

        //Debug.Log($"droneAimation {droneAimation!= null}");
            


        //// Находим Canvas меню (по имени или тегу)
        //_menuCanvas = GameObject.Find("MainMenuCanvas");
        //if (_menuCanvas == null)
        //{
        //    // Альтернативный поиск
        //    _menuCanvas = GameObject.FindGameObjectWithTag("MainMenu");
        //}

        //if (_menuCanvas == null)
        //{
        //    _logger.LogWarning("[MainMenuState] MainMenuCanvas not found in scene");
        //}
        //else
        //{
        //    // Изначально выключаем (включится при входе в состояние)
        //    _menuCanvas.SetActive(false);
        //}

        //// Находим анимированную модель дрона
        //_animatedDrone = GameObject.Find("MenuDroneModel");
        //if (_animatedDrone == null)
        //{
        //    _animatedDrone = GameObject.FindGameObjectWithTag("MenuDrone");
        //}

        //if (_animatedDrone != null)
        //{
        //    // Получаем компонент анимации
        //    _droneAnimator = _animatedDrone.GetComponent<Animator>();
        //    _animatedDrone.SetActive(false); // Выключаем изначально
        //}
        //else
        //{
        //    _logger.LogWarning("[MainMenuState] Menu drone model not found in scene");
        //}

        //// Находим основную камеру
        //_mainCamera = Camera.main;
        //if (_mainCamera == null)
        //{
        //    _mainCamera = GameObject.FindObjectOfType<Camera>();
        //}

        //// Находим XR Origin
        //_xrOrigin = GameObject.Find("XR Origin")?.transform;
        //if (_xrOrigin == null)
        //{
        //    _xrOrigin = GameObject.Find("Camera Offset")?.transform;
        //}

        _logger.Log("[MainMenuState] Scene objects found successfully");
    }

    /// <summary>
    /// Настройка начального состояния объектов
    /// </summary>
    private void SetupInitialState()
    {
        // Убеждаемся, что все объекты меню выключены
        if (_menuCanvas != null)
        {
            _menuCanvas.SetActive(false);
        }

        if (_animatedDrone != null)
        {
            _animatedDrone.SetActive(false);
        }
    }

    /// <summary>
    /// Позиционирование Canvas перед камерой
    /// </summary>
    private void PositionCanvasInFrontOfCamera()
    {
        if (_menuCanvas == null || _mainCamera == null) return;

        var canvasTransform = _menuCanvas.transform;
        var cameraTransform = _mainCamera.transform;

        // Расстояние до Canvas (настраиваемое)
        float distance = 1.5f;

        // Позиция на уровне глаз
        Vector3 targetPosition = cameraTransform.position + cameraTransform.forward * distance;
        targetPosition.y = cameraTransform.position.y - 0.1f; // Чуть ниже уровня глаз

        canvasTransform.position = targetPosition;

        // Поворот к камере (но не по вертикали)
        Vector3 lookDirection = cameraTransform.position - targetPosition;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            canvasTransform.rotation = Quaternion.LookRotation(lookDirection);
        }

        // Настройка размера для лучшей читаемости в VR
        AdjustCanvasScaleForVR();
    }

    /// <summary>
    /// Настройка масштаба Canvas для VR
    /// </summary>
    private void AdjustCanvasScaleForVR()
    {
        var canvas = _menuCanvas.GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            // Оптимальный размер для VR
            var rectTransform = canvas.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(800, 450);
                rectTransform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
            }
        }
    }

    /// <summary>
    /// Обновление позиции Canvas
    /// </summary>
    private void UpdateCanvasPosition()
    {
        // Можно добавить плавное следование за камерой
        if (_menuCanvas != null && _menuCanvas.activeSelf && _mainCamera != null)
        {
            // Простая реализация следования (можно улучшить)
            var canvasTransform = _menuCanvas.transform;
            var cameraTransform = _mainCamera.transform;

            Vector3 targetPosition = cameraTransform.position + cameraTransform.forward * 1.5f;
            targetPosition.y = cameraTransform.position.y - 0.1f;

            // Плавное движение
            canvasTransform.position = Vector3.Lerp(
                canvasTransform.position,
                targetPosition,
                Time.deltaTime * 2f);

            // Плавный поворот
            Vector3 lookDirection = cameraTransform.position - canvasTransform.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                canvasTransform.rotation = Quaternion.Slerp(
                    canvasTransform.rotation,
                    targetRotation,
                    Time.deltaTime * 2f);
            }
        }
    }

    /// <summary>
    /// Запуск анимации дрона
    /// </summary>
    private void StartDroneAnimation()
    {
        //if (droneAimation == null) return;

        //droneAimation.StartEndlessAnimation();            
    }

    /// <summary>
    /// Остановка анимации дрона
    /// </summary>
    private void StopDroneAnimation()
    {
        //if (droneAimation != null)
        //{
        //    droneAimation.StopAnimation();
        //}
    }
       

    /// <summary>
    /// Приостановка анимации дрона
    /// </summary>
    private void PauseDroneAnimation()
    {
        if (_droneAnimator != null)
        {
            _droneAnimator.speed = 0f;
        }
    }

    /// <summary>
    /// Возобновление анимации дрона
    /// </summary>
    private void ResumeDroneAnimation()
    {
        if (_droneAnimator != null)
        {
            _droneAnimator.speed = 1f;
        }
    }

    /// <summary>
    /// Настройка XR Origin для меню
    /// </summary>
    private void ConfigureXRForMenu()
    {
        // Здесь можно настроить параметры XR для меню:
        // - Скорость перемещения
        // - Настройки комфорта
        // - И т.д.

        _logger.Log("[MainMenuState] XR configured for menu mode");
    }

    /// <summary>
    /// Настройка камеры и освещения
    /// </summary>
    private void ConfigureCameraAndLighting()
    {
        // Можно настроить пост-обработку для меню
        // Или включить/выключить определенные источники света

        _logger.Log("[MainMenuState] Camera and lighting configured for menu");
    }

    /// <summary>
    /// Анимация входа
    /// </summary>
    private async Task PerformEnterAnimationAsync()
    {
        // Простая fade-in анимация
        var canvasGroup = _menuCanvas?.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;

            float duration = 0.5f;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsedTime / duration);
                await Task.Yield();
            }

            canvasGroup.alpha = 1f;
        }
    }

    /// <summary>
    /// Анимация выхода
    /// </summary>
    private async Task PerformExitAnimationAsync()
    {
        // Простая fade-out анимация
        var canvasGroup = _menuCanvas?.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            float duration = 0.3f;
            float elapsedTime = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / duration);
                await Task.Yield();
            }

            canvasGroup.alpha = 0f;
        }
    }
   

    #endregion
}
