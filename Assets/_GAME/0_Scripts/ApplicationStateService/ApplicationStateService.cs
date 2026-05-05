using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Zenject;


/// <summary>
/// Реализация сервиса управления состояниями приложения для VR конструктора дронов
/// </summary>
public class ApplicationStateService : IApplicationStateService, IInitializable, IDisposable
{
    // Зависимости
    private readonly IEventBus _eventBus;
    private readonly DiContainer _container;
    private readonly IAppLogger _logger;
    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly ISceneLoader _sceneLoader;


    // Состояния
    private readonly Dictionary<AppState, IAppState> _states = new();
    private IAppState _currentState;
    private AppState _previousState = AppState.None;
    private bool _isTransitioning = false;
    private bool _isPaused = false;

    // История переходов (для отладки и undo)
    private readonly List<StateTransitionRecord> _transitionHistory = new();
    private const int MaxHistorySize = 20;

    // Текущий контекст паузы
    private PauseContext _pauseContext;
    private bool _hasPublishedInitialState = false;

    // События
    public event Action<AppStateChangedEvent> OnAppStateChanged;
    public event Action<PauseStateChangedEvent> OnPauseStateChanged;

    // Свойства
    public AppState CurrentState => _currentState?.StateId ?? AppState.None;
    public AppState PreviousState => _previousState;
    public bool IsPaused => _isPaused;

    /// <summary>
    /// Конструктор с инъекцией зависимостей
    /// </summary>
    [Inject]
    public ApplicationStateService(
        IEventBus eventBus,
        DiContainer container,
        IAppLogger logger,
        IPerformanceMonitor performanceMonitor,
        ISceneLoader sceneLoader
        )
    {
        _eventBus = eventBus;
        _container = container;
        _logger = logger;
        _performanceMonitor = performanceMonitor;
        _sceneLoader = sceneLoader;
       
    }


    private void InjectStates()
    {
        var mainMenuState = _container.Resolve<MainMenuState>();
        var assemblyState = _container.Resolve<AssemblyState>();
        var flightState = _container.Resolve<FlightState>();
        var pauseState = _container.Resolve<ApplicationPauseState>();
        var errorState = _container.Resolve<ErrorState>();
        // Регистрация состояний
        RegisterState(mainMenuState);
        RegisterState(assemblyState);
        RegisterState(flightState);
        RegisterState(pauseState);
        RegisterState(errorState);
    }


    public void Initialize()
    {
        Debug.Log($"[StateService] Initializing... {this}");

        _logger.Log("[StateService] Initializing...");

        InjectStates();

        // 1. Подписка на события
        SubscribeToEvents();

        // 2. Инициализация всех состояний
        InitializeAllStates();

        // 3. Проверка доступности VR оборудования
        CheckVRSetup();

        // 4. Установка начального состояния
        SetInitialState();

        // 5. Запуск мониторинга производительности
        _performanceMonitor.StartMonitoring();

        _logger.Log("[StateService] Initialization complete");
    }

    /// <summary>
    /// Запросить изменение состояния
    /// </summary>
    public async Task<StateTransitionResult> RequestStateChangeAsync(
        AppState targetState,
        StateTransitionContext context = null)
    {
        context ??= new StateTransitionContext();

        _logger.Log($"[StateService] Requesting state change: {CurrentState} -> {targetState}, Source: {context.Source}");

        // Проверки
        if (_isTransitioning)
        {
            var error = "Cannot start transition while another transition is in progress";
            _logger.LogError($"[StateService] {error}");
            return StateTransitionResult.Failed(error);
        }

        if (CurrentState == targetState)
        {
            _logger.LogWarning($"[StateService] Already in state {targetState}");
            return StateTransitionResult.Failed($"Already in state {targetState}");
        }

        if (!_states.ContainsKey(targetState))
        {
            var error = $"State {targetState} is not registered";
            _logger.LogError($"[StateService] {error}");
            return StateTransitionResult.Failed(error);
        }

        // Проверка производительности перед переходом
        if (!await CheckPerformanceBeforeTransitionAsync(targetState))
        {
            return StateTransitionResult.Failed("Performance check failed");
        }

        // Создание задачи перехода
        var transitionTask = ExecuteTransitionAsync(targetState, context);

        // Запуск перехода
        _isTransitioning = true;

        try
        {
            var result = await transitionTask;

            if (result.Success())
            {
                context.OnSuccess?.Invoke();
                _logger.Log($"[StateService] Transition completed successfully ");
            }
            else
            {
                context.OnError?.Invoke(new Exception(result.ErrorMessage()));
                _logger.LogError($"[StateService] Transition failed: {result.ErrorMessage()}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[StateService] Transition exception: {ex.Message}");
            context.OnError?.Invoke(ex);

            // Переход в состояние ошибки
            await HandleTransitionExceptionAsync(ex, targetState, context);

            return StateTransitionResult.Failed($"Transition exception: {ex.Message}");
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    /// <summary>
    /// Перевести приложение в режим паузы
    /// </summary>
    public void PauseApplication(PauseInitiator pauseReason = PauseInitiator.User)
    {
        if (_isPaused) return;

        _logger.Log($"[StateService] Pausing application. Reason: {pauseReason}");

        // Сохраняем контекст паузы
        _pauseContext = new PauseContext
        {
            Reason = pauseReason,
            PausedState = CurrentState,
            PauseTime = DateTime.UtcNow
        };

        // Приостановка времени игры
        Time.timeScale = 0f;

        // Приостановка звука
        AudioListener.pause = true;

        // Приостановка физики
        //Physics.autoSimulation = false;

        // Публикация события
        var pauseEvent = new PauseStateChangedEvent
        {
            IsPaused = true,
            Reason = pauseReason,
            PausedState = CurrentState,
            Timestamp = DateTime.UtcNow
        };

        _eventBus.Publish(pauseEvent);
        OnPauseStateChanged?.Invoke(pauseEvent);

        _isPaused = true;

        _logger.Log("[StateService] Application paused");
    }

    /// <summary>
    /// Возобновить работу приложения
    /// </summary>
    public void ResumeApplication()
    {
        if (!_isPaused) return;

        _logger.Log("[StateService] Resuming application");

        // Восстановление времени игры
        Time.timeScale = 1f;

        // Восстановление звука
        AudioListener.pause = false;

        // Восстановление физики
        //Physics.autoSimulation = true;

        // Публикация события
        var resumeEvent = new PauseStateChangedEvent
        {
            IsPaused = false,
            Reason = _pauseContext?.Reason ?? PauseInitiator.User,
            PausedState = _pauseContext?.PausedState ?? AppState.None,
            PauseDuration = DateTime.UtcNow - (_pauseContext?.PauseTime ?? DateTime.UtcNow),
            Timestamp = DateTime.UtcNow
        };

        _eventBus.Publish(resumeEvent);
        OnPauseStateChanged?.Invoke(resumeEvent);

        _isPaused = false;
        _pauseContext = null;

        _logger.Log("[StateService] Application resumed");
    }

    /// <summary>
    /// Освобождение ресурсов
    /// </summary>
    public void Dispose()
    {
        _logger.Log("[StateService] Disposing...");

        // Отписка от событий
        UnsubscribeFromEvents();

        // Очистка состояний
        foreach (var state in _states.Values.OfType<IDisposable>())
        {
            state.Dispose();
        }

        _states.Clear();

        // Остановка мониторинга
        _performanceMonitor.StopMonitoring();

        _logger.Log("[StateService] Disposed");
    }

    #region Приватные методы

    /// <summary>
    /// Регистрация состояния
    /// </summary>
    private void RegisterState(IAppState state)
    {
        if (state == null)
        {
            _logger.LogError($"[StateService] Attempted to register null state");
            return;
        }

        var stateId = state.StateId;

        if (_states.ContainsKey(stateId))
        {
            _logger.LogWarning($"[StateService] State {stateId} is already registered. Overwriting.");
        }

        _states[stateId] = state;
        _logger.Log($"[StateService] Registered state: {stateId}");
    }

    /// <summary>
    /// Подписка на события
    /// </summary>
    private void SubscribeToEvents()
    {
        //// Подписка на запросы изменения состояния
        //_eventBus.Subscribe<StateChangeRequestedEvent>(OnStateChangeRequested, EventPriority.SystemHigh);

        //// Подписка на запросы паузы
        //_eventBus.Subscribe<PauseRequestedEvent>(OnPauseRequested, EventPriority.SystemHigh);
        //_eventBus.Subscribe<ResumeRequestedEvent>(OnResumeRequested, EventPriority.SystemHigh);

        //// Подписка на события от VR оборудования
        //_eventBus.Subscribe<VRHeadsetRemovedEvent>(OnVRHeadsetRemoved, EventPriority.SystemCritical);
        //_eventBus.Subscribe<VRHeadsetPutOnEvent>(OnVRHeadsetPutOn, EventPriority.SystemCritical);

        //// Подписка на события ошибок
        //_eventBus.Subscribe<CriticalErrorEvent>(OnCriticalError, EventPriority.SystemCritical);

        //_logger.Log("[StateService] Subscribed to events");
        _logger.LogWarning("[StateService] NEED To be Subscribed to events");
    }

    /// <summary>
    /// Отписка от событий
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        // нужно сохранить ID подписок и отписаться
        //_logger.Log("[StateService] Unsubscribed from events");
        _logger.LogWarning("[StateService] NEED To be UnSubscribed to events");
    }

    /// <summary>
    /// Инициализация всех состояний
    /// </summary>
    private void InitializeAllStates()
    {
        foreach (var state in _states.Values)
        {
            if (state is IInitializableState initializable)
            {
                try
                {
                    initializable.StateInitialize();
                    _logger.Log($"[StateService] Initialized state: {state.StateId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[StateService] Failed to initialize state {state.StateId}: {ex.Message}");
                }
            }
        }

    }

    /// <summary>
    /// Проверка VR оборудования
    /// </summary>
    private void CheckVRSetup()
    {
        // Проверка наличия VR оборудования
        if (!UnityEngine.XR.XRSettings.enabled)
        {
            _logger.LogWarning("[StateService] VR is not enabled in XR Settings");
        }

        // Проверка трекинга
        // (В реальном проекте здесь может быть проверка доступности контроллеров и т.д.)

        _logger.Log("[StateService] VR setup check completed");
    }

    /// <summary>
    /// Установка начального состояния
    /// </summary>
    private void SetInitialState()
    {
        // Начинаем с главного меню
        var initialState = AppState.MainMenu;

        if (!_states.TryGetValue(initialState, out var state))
        {
            _logger.LogError($"[StateService] Initial state {initialState} not found. Using first available.");
            initialState = _states.Keys.First();
            state = _states[initialState];
        }

        _currentState = state;

        // Вход в начальное состояние
        try
        {
            // Синхронный вызов, т.к. мы в Initialize()
            state.EnterAsync(new StateTransitionContext
            {
                Source = TransitionSource.System,
                Data = "Initial startup"
            }).Wait();

            var requestScenePreloadEvent = new RequestPreloadSceneEvent
            {
                Timestamp = DateTime.UtcNow,
                TargetSceneName = "MainScene"
            };

            _eventBus.Publish(requestScenePreloadEvent);

            _logger.Log($"[StateService] Initial state set to: {initialState}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[StateService] Failed to enter initial state {initialState}: {ex.Message}");

            // Попытка перейти в состояние ошибки
            if (_states.TryGetValue(AppState.Error, out var errorState))
            {
                _currentState = errorState;
                errorState.EnterAsync(new StateTransitionContext
                {
                    Source = TransitionSource.System,
                    Data = $"Failed to enter initial state: {ex.Message}"
                }).Wait();
            }
        }
    }


    //public void Tick()
    //{
    //    if (!_hasPublishedInitialState && _eventBus != null)
    //    {
    //        // Публикуем на первом кадре, когда EventBus точно готов
    //        // Публикуем событие предзагрузки сцены сборки
    //        var requestScenePreloadEvent = new RequestPreloadSceneEvent
    //        {
    //            Timestamp = DateTime.UtcNow,
    //            TargetSceneName = "MainScene"
    //        };

    //        _eventBus.Publish(requestScenePreloadEvent);
    //        _hasPublishedInitialState = true;
    //    }
    //}



    /// <summary>
    /// Выполнение перехода между состояниями
    /// </summary>
    private async Task<StateTransitionResult> ExecuteTransitionAsync(
        AppState targetState,
        StateTransitionContext context)
    {
        var startTime = DateTime.UtcNow;
        var targetStateInstance = _states[targetState];

        _logger.Log($"[StateService] Starting transition: {CurrentState} -> {targetState}");

        // Публикация события начала перехода
        _eventBus.Publish(new StateTransitionStartedEvent
        {
            FromState = CurrentState,
            ToState = targetState,
            Timestamp = startTime,
            Context = context
        });

        try
        {
            // 1. Выход из текущего состояния
            if (_currentState != null)
            {
                _logger.Log($"[StateService] Exiting state: {CurrentState}");
                await _currentState.ExitAsync();
            }

            // 2. Валидация перехода (если не пропущена)
            if (!context.SkipValidation)
            {
                _logger.Log($"[StateService] Validating transition...");
                var validationResult = await ValidateTransitionAsync(CurrentState, targetState, context);
                if (!validationResult.IsValid)
                {
                    return StateTransitionResult.Failed($"Transition validation failed: {validationResult.ErrorMessage}");
                }
            }

            // 3. Подготовка данных перехода
            var transitionData = await PrepareTransitionDataAsync(CurrentState, targetState, context);
            context.Data = transitionData;

            // 4. Вход в новое состояние
            _logger.Log($"[StateService] Entering state: {targetState}");
            await targetStateInstance.EnterAsync(context);

            // 5. Обновление истории состояний
            UpdateTransitionHistory(CurrentState, targetState, context, startTime);

            // 8. Проверка нужна ли смена сцены и Публикация события смены сцены (при необходимости)
            //if (targetState == AppState.MainMenu)
            //{
            //    var requestScenePreloadEvent = new RequestPreloadSceneEvent
            //    {
            //        Timestamp = DateTime.UtcNow,
            //        TargetSceneName = "MainScene"
            //    };
            //}

            // 6. Обновление текущего состояния
            _previousState = CurrentState;
            _currentState = targetStateInstance;

            // 7. Публикация события успешного перехода
            var stateChangedEvent = new AppStateChangedEvent
            {
                PreviousState = _previousState,
                NewState = targetState,
                TransitionDuration = DateTime.UtcNow - startTime,
                Context = context,
                Timestamp = DateTime.UtcNow
            };


            _eventBus.Publish(stateChangedEvent);
            OnAppStateChanged?.Invoke(stateChangedEvent);

            _logger.Log($"[StateService] Transition completed: {_previousState} -> {targetState}");

            return StateTransitionResult.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogError($"[StateService] Transition failed: {ex.Message}");

            // Публикация события ошибки перехода
            _eventBus.Publish(new StateTransitionFailedEvent
            {
                FromState = CurrentState,
                ToState = targetState,
                Error = ex,
                Context = context,
                Timestamp = DateTime.UtcNow
            });

            return StateTransitionResult.Failed($"Transition failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Проверка производительности перед переходом
    /// </summary>
    private async Task<bool> CheckPerformanceBeforeTransitionAsync(AppState targetState)
    {
        // Для VR критично поддерживать стабильный FPS

        // Проверка текущего FPS
        var currentFPS = _performanceMonitor.CurrentFPS;
        var minRequiredFPS = GetMinFPSForState(targetState);

        if (currentFPS < minRequiredFPS * 0.7f) // 70% от требуемого
        {
            _logger.LogWarning($"[StateService] Low FPS ({currentFPS:F1}) for transition to {targetState}. Required: {minRequiredFPS}");

            // Попытка оптимизации
            await TryOptimizeBeforeTransitionAsync(targetState);

            // Повторная проверка
            currentFPS = _performanceMonitor.CurrentFPS;
            if (currentFPS < minRequiredFPS * 0.8f)
            {
                _logger.LogError($"[StateService] Cannot transition to {targetState} due to low FPS: {currentFPS:F1}");
                return false;
            }
        }

        // Проверка памяти (особенно важно для Quest 2)
        var memoryUsage = _performanceMonitor.MemoryUsageMB;
        var maxMemoryForState = GetMaxMemoryForState(targetState);

        if (memoryUsage > maxMemoryForState * 0.9f)
        {
            _logger.LogWarning($"[StateService] High memory usage ({memoryUsage:F1}MB) for {targetState}. Max: {maxMemoryForState}MB");

            // Попытка освободить память
            await TryFreeMemoryBeforeTransitionAsync(targetState);

            // Повторная проверка
            memoryUsage = _performanceMonitor.MemoryUsageMB;
            if (memoryUsage > maxMemoryForState)
            {
                _logger.LogError($"[StateService] Cannot transition to {targetState} due to high memory usage: {memoryUsage:F1}MB");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Валидация перехода между состояниями
    /// </summary>
    private async Task<TransitionValidationResult> ValidateTransitionAsync(
        AppState fromState,
        AppState toState,
        StateTransitionContext context)
    {
        // Базовые проверки

        // 1. Проверка возможности перехода
        if (!IsTransitionAllowed(fromState, toState))
        {
            return TransitionValidationResult.Failed($"Transition from {fromState} to {toState} is not allowed");
        }

        // 2. Специфичные проверки для переходов в режим полета
        if (toState == AppState.Flight)
        {
            return await ValidateFlightTransitionAsync(context);
        }

        // 3. Специфичные проверки для переходов в режим сборки
        if (toState == AppState.Assembly)
        {
            return await ValidateAssemblyTransitionAsync(context);
        }

        return TransitionValidationResult.Success();
    }

    /// <summary>
    /// Проверка перехода в режим полета
    /// </summary>
    private async Task<TransitionValidationResult> ValidateFlightTransitionAsync(StateTransitionContext context)
    {
        //try
        //{
        //    // Получение сервиса валидации через контейнер
        //    var validationService = _container.Resolve<IValidationService>();

        //    // Проверка готовности сборки к полету
        //    var assemblySnapshot = context.Data as AssemblySnapshot;
        //    if (assemblySnapshot == null)
        //    {
        //        return TransitionValidationResult.Failed("Assembly snapshot is required for flight transition");
        //    }

        //    var validationResult = await validationService.ValidateForFlightAsync(assemblySnapshot);

        //    if (!validationResult.IsValid)
        //    {
        //        var errorMessage = string.Join(", ", validationResult.Errors.Select(e => e.Message));
        //        return TransitionValidationResult.Failed($"Assembly validation failed: {errorMessage}");
        //    }

        //    return TransitionValidationResult.Success();
        //}
        //catch (Exception ex)
        //{
        //    _logger.LogError($"[StateService] Flight transition validation error: {ex.Message}");
        //    return TransitionValidationResult.Failed($"Validation error: {ex.Message}");
        //}
        await Task.Delay(1);  // заглушка
        return TransitionValidationResult.Success();
    }

    /// <summary>
    /// Проверка перехода в режим сборки
    /// </summary>
    private async Task<TransitionValidationResult> ValidateAssemblyTransitionAsync(StateTransitionContext context)
    {
        // Проверка доступности VR оборудования - Нужно только на устройстве                   
        //if (!UnityEngine.XR.XRSettings.enabled)
        //{
        //    return TransitionValidationResult.Failed("VR is not enabled");
        //}

        // Проверка доступности контроллеров
        // (Может быть проверка трекинга контроллеров)
        await Task.Delay(1);  // заглушка 
        return TransitionValidationResult.Success();
    }

    /// <summary>
    /// Подготовка данных для перехода
    /// </summary>
    private async Task<object> PrepareTransitionDataAsync(
        AppState fromState,
        AppState toState,
        StateTransitionContext context)
    {
        // Подготовка специфичных данных для разных переходов

        //switch (fromState)
        //{
        //    case AppState.Assembly when toState == AppState.Flight:
        //        // Получение снимка сборки
        //        var assemblyService = _container.Resolve<IAssemblyService>();
        //        return await assemblyService.CreateSnapshotAsync();

        //    case AppState.Flight when toState == AppState.Assembly:
        //        // Получение данных полета для восстановления
        //        var flightService = _container.Resolve<IFlightService>();
        //        return new FlightTransitionData
        //        {
        //            Telemetry = flightService.GetCurrentTelemetry(),
        //            Position = flightService.GetDronePosition(),
        //            Rotation = flightService.GetDroneRotation()
        //        };

        //    default:
        //        return context.Data; // Возвращаем исходные данные
        //}
        await Task.Delay(1);  // заглушка
        return context.Data; // todo ПОКА ЧТО Возвращаем исходные данные без изменений
    }

    /// <summary>
    /// Обновление истории переходов
    /// </summary>
    private void UpdateTransitionHistory(
        AppState fromState,
        AppState toState,
        StateTransitionContext context,
        DateTime startTime)
    {
        var record = new StateTransitionRecord
        {
            FromState = fromState,
            ToState = toState,
            StartTime = startTime,
            EndTime = DateTime.UtcNow,
            Source = context.Source,
            Success = true
        };

        _transitionHistory.Add(record);

        // Ограничение размера истории
        if (_transitionHistory.Count > MaxHistorySize)
        {
            _transitionHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// Обработка исключения при переходе
    /// </summary>
    private async Task HandleTransitionExceptionAsync(
        Exception exception,
        AppState targetState,
        StateTransitionContext context)
    {
        _logger.LogError($"[StateService] Handling transition exception to {targetState}: {exception.Message}");

        // Публикация события критической ошибки
        _eventBus.Publish(new CriticalErrorEvent
        {
            Error = exception,
            Context = $"Transition from {CurrentState} to {targetState}",
            Timestamp = DateTime.UtcNow
        });

        // Попытка перейти в состояние ошибки
        if (_states.TryGetValue(AppState.Error, out var errorState) && _currentState != errorState)
        {
            try
            {
                await errorState.EnterAsync(new StateTransitionContext
                {
                    Source = TransitionSource.System,
                    Data = new ErrorTransitionData
                    {
                        FailedTransition = targetState,
                        Exception = exception,
                        OriginalContext = context
                    }
                });

                _previousState = CurrentState;
                _currentState = errorState;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[StateService] Failed to enter error state: {ex.Message}");
            }
        }
    }




    /// <summary>
    /// Получение минимального FPS для состояния
    /// </summary>
    private int GetMinFPSForState(AppState state)
    {
        return state switch
        {
            AppState.Flight => 72,    // Для полета нужен стабильный высокий FPS
            AppState.Assembly => 60,  // Для сборки можно чуть ниже
            AppState.MainMenu => 45,  // Для меню достаточно 45 FPS
            _ => 30
        };
    }

    /// <summary>
    /// Получение максимального использования памяти для состояния
    /// </summary>
    private float GetMaxMemoryForState(AppState state)
    {
        // Quest 2 имеет ограниченную память (~4GB доступно для приложения)
        return state switch
        {
            AppState.Flight => 1500f,    // Полет требует больше памяти для физики
            AppState.Assembly => 1200f,  // Сборка требует память для деталей
            AppState.MainMenu => 800f,   // Меню требует меньше памяти
            _ => 1000f
        };
    }

    /// <summary>
    /// Проверка разрешенности перехода
    /// </summary>
    private bool IsTransitionAllowed(AppState fromState, AppState toState)
    {
        // Матрица разрешенных переходов
        var allowedTransitions = new Dictionary<AppState, AppState[]>
        {
            [AppState.MainMenu] = new[] { AppState.Assembly, AppState.Error },
            [AppState.Assembly] = new[] { AppState.Flight, AppState.MainMenu, AppState.Paused, AppState.Error },
            [AppState.Flight] = new[] { AppState.Assembly, AppState.Paused, AppState.Error },
            [AppState.Paused] = new[] { AppState.Assembly, AppState.Flight, AppState.MainMenu },
            [AppState.Error] = new[] { AppState.MainMenu, AppState.Assembly },
            [AppState.Loading] = new[] { AppState.MainMenu, AppState.Assembly, AppState.Flight }
        };

        if (!allowedTransitions.ContainsKey(fromState))
            return false;

        return allowedTransitions[fromState].Contains(toState);
    }

    /// <summary>
    /// Попытка оптимизации перед переходом
    /// </summary>
    private async Task TryOptimizeBeforeTransitionAsync(AppState targetState)
    {
        _logger.Log($"[StateService] Attempting optimization for transition to {targetState}");

        //// Освобождение неиспользуемых ресурсов
        //var assetProvider = _container.Resolve<IAssetProvider>();
        //assetProvider.ClearUnusedAssets();

        //// Упрощение графики если нужно
        //if (targetState == AppState.Flight)
        //{
        //    // Для полета можно временно упростить графику
        //    AdjustGraphicsForFlight();
        //}

        await Task.Delay(100); // Даем время на оптимизацию
    }

    /// <summary>
    /// Попытка освободить память перед переходом
    /// </summary>
    private async Task TryFreeMemoryBeforeTransitionAsync(AppState targetState)
    {
        _logger.Log($"[StateService] Attempting to free memory for transition to {targetState}");

        //// Принудительный сбор мусора
        //GC.Collect();
        //GC.WaitForPendingFinalizers();

        //// Очистка кеша ассетов
        //var assetProvider = _container.Resolve<IAssetProvider>();
        //assetProvider.ClearCache();

        await Task.Delay(200); // Даем время на освобождение памяти
    }

    /// <summary>
    /// Настройка графики для режима полета
    /// </summary>
    private void AdjustGraphicsForFlight()
    {
        // В реальном проекте здесь может быть:
        // - Уменьшение качества текстур
        // - Отключение некоторых эффектов
        // - Уменьшение дальности прорисовки

        _logger.Log("[StateService] Adjusted graphics for flight mode");
    }

    #endregion

    #region Обработчики событий

    //private void OnStateChangeRequested(StateChangeRequestedEvent e)
    //{
    //    _ = RequestStateChangeAsync(e.TargetState, e.Context);
    //}

    //private void OnPauseRequested(PauseRequestedEvent e)
    //{
    //    PauseApplication(e.Reason);
    //}

    //private void OnResumeRequested(ResumeRequestedEvent e)
    //{
    //    ResumeApplication();
    //}

    //private void OnVRHeadsetRemoved(VRHeadsetRemovedEvent e)
    //{
    //    _logger.Log("[StateService] VR headset removed, pausing application");
    //    PauseApplication(PauseReason.HeadsetRemoved);
    //}

    //private void OnVRHeadsetPutOn(VRHeadsetPutOnEvent e)
    //{
    //    _logger.Log("[StateService] VR headset put on, resuming application");
    //    ResumeApplication();
    //}

    //private void OnCriticalError(CriticalErrorEvent e)
    //{
    //    _logger.LogError($"[StateService] Critical error: {e.Error.Message}");

    //    // Переход в состояние ошибки
    //    _ = RequestStateChangeAsync(AppState.Error, new StateTransitionContext
    //    {
    //        Source = TransitionSource.System,
    //        Data = e
    //    });
    //}

    #endregion

    #region Вспомогательные классы

    /// <summary>
    /// Запись о переходе для истории
    /// </summary>
    private class StateTransitionRecord
    {
        public AppState FromState { get; set; }
        public AppState ToState { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TransitionSource Source { get; set; }
        public bool Success { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
    }

    /// <summary>
    /// Контекст паузы
    /// </summary>
    private class PauseContext
    {
        public PauseInitiator Reason { get; set; }
        public AppState PausedState { get; set; }
        public DateTime PauseTime { get; set; }
    }

    /// <summary>
    /// Данные для перехода из полета в сборку
    /// </summary>
    private class FlightTransitionData
    {
        //public FlightTelemetry Telemetry { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
    }

    /// <summary>
    /// Данные для перехода в состояние ошибки
    /// </summary>
    private class ErrorTransitionData
    {
        public AppState FailedTransition { get; set; }
        public Exception Exception { get; set; }
        public StateTransitionContext OriginalContext { get; set; }
    }

    #endregion
}

/// <summary>
/// Результат валидации перехода
/// </summary>
public class TransitionValidationResult
{
    public bool IsValid { get; private set; }
    public string ErrorMessage { get; private set; }

    public static TransitionValidationResult Success() => new() { IsValid = true };
    public static TransitionValidationResult Failed(string error) => new() { IsValid = false, ErrorMessage = error };
}
