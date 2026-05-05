using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

/// <summary>
/// Реализация шины событий для межсистемной коммуникации в VR конструкторе дронов
/// </summary>
public class EventBus : IEventBus, IInitializable, IDisposable
{
    // Зависимости
    private readonly IAppLogger _logger;
    //private readonly IPerformanceMonitor _performanceMonitor;
    private readonly DiContainer _container;

    // Структуры для хранения подписчиков
    private readonly ConcurrentDictionary<Type, List<SubscriptionInfo>> _subscriptions = new();
    private readonly ConcurrentDictionary<Type, object> _eventHistory = new();
    private readonly ConcurrentBag<IDisposable> _disposables = new();

    // Статистика и мониторинг
    private readonly ConcurrentDictionary<Type, EventStatistics> _statistics = new();
    private readonly ConcurrentQueue<EventLogEntry> _eventLog = new();
    private const int MaxEventLogSize = 1000;
    private const int MaxHistoryPerEvent = 10;

    // Настройки
    private bool _isLoggingEnabled = true;
    private bool _isHistoryEnabled = true;
    private bool _isAsyncProcessingEnabled = true;
    private int _maxAsyncProcessingTimeMs = 1000;

    // Флаги состояния
    private bool _isInitialized = false;
    private bool _isDisposing = false;

    // Очередь для асинхронной обработки
    private readonly ConcurrentQueue<AsyncEventTask> _asyncEventQueue = new();
    private CancellationTokenSource _processingCts;
    private Task _processingTask;

    /// <summary>
    /// Конструктор
    /// </summary>
    [Inject]
    public EventBus(
        IAppLogger logger,
        //IPerformanceMonitor performanceMonitor,
        DiContainer container)
    {
        _logger = logger;
        //_performanceMonitor = performanceMonitor;
        _container = container;
    }

    /// <summary>
    /// Инициализация шины событий
    /// </summary>
    [Inject]
    public void Initialize()
    {
        if (_isInitialized) return;

        _logger.Log("[EventBus] Initializing...");

        // Инициализация структур данных
        InitializeDefaultSubscriptions();

        // Запуск обработчика асинхронных событий
        StartAsyncProcessing();

        // Подписка на события производительности
        //SubscribeToPerformanceEvents();

        _isInitialized = true;

        _logger.Log("[EventBus] Initialization complete");
    }

    /// <summary>
    /// Подписаться на события типа TEvent
    /// </summary>
    public IDisposable Subscribe<TEvent>(
        Action<TEvent> handler,
        EventPriority priority = EventPriority.Normal)
        where TEvent : IAppEvent
    {
        return Subscribe(handler, null, priority);
    }
       



    /// <summary>
    /// Подписаться на события с фильтром
    /// </summary>
    public IDisposable Subscribe<TEvent>(Action<TEvent> handler,Func<TEvent, bool> filter,EventPriority priority = EventPriority.Normal) where TEvent : IAppEvent
    {
        ValidateHandler(handler);

        var eventType = typeof(TEvent);
        var subscription = new SubscriptionInfo<TEvent>(
            handler,
            filter,
            priority,
            GetCallerInfo());

        // Добавление подписки
        var subscriptions = _subscriptions.GetOrAdd(eventType, _ => new List<SubscriptionInfo>());

        lock (subscriptions)
        {
            // Вставка с учетом приоритета
            int insertIndex = 0;
            for (; insertIndex < subscriptions.Count; insertIndex++)
            {
                if (subscriptions[insertIndex].Priority < priority)
                    break;
            }

            subscriptions.Insert(insertIndex, subscription);
        }

        // Обновление статистики
        UpdateStatistics(eventType, subscription);

        // Логирование
        if (_isLoggingEnabled)
        {
            LogSubscriptionAdded(eventType, subscription);
        }

        // Возвращаем disposable для отписки
        return new DisposableSubscription(() => Unsubscribe(subscription));
    }

    /// <summary>
    /// Опубликовать событие
    /// </summary>
    public void Publish<TEvent>(TEvent @event)
        where TEvent : IAppEvent
    {
        _logger.Log($"Trying to publish Event {@event.EventId}");
        ValidateEvent(@event);

        _logger.Log($"Continue Trying to publish Event {@event.EventId}");
        var eventType = typeof(TEvent);
        var startTime = DateTime.UtcNow;

        // Логирование публикации
        if (_isLoggingEnabled)
        {
            LogEventPublished(@event, eventType);
        }

        // Сохранение в историю
        if (_isHistoryEnabled)
        {
            SaveToHistory(eventType, @event);
        }

        // Получение подписчиков
        if (!_subscriptions.TryGetValue(eventType, out var subscriptions) || subscriptions.Count == 0)
        {
            if (_isLoggingEnabled)
            {
                _logger.Log($"[EventBus] No subscribers for event: {eventType.Name}");
            }
            return;
        }

        // Обработка события
        try
        {
            // Копируем подписки для безопасной итерации
            var subscriptionsCopy = subscriptions.ToArray();
            int handlerCount = 0;

            foreach (var subscription in subscriptionsCopy)
            {
                if (_isDisposing) break;

                try
                {
                    // Проверка фильтра
                    if (!subscription.ShouldHandle(@event))
                        continue;

                    // Вызов обработчика
                    subscription.Handle(@event);
                    handlerCount++;

                    // Обновление статистики вызовов
                    UpdateHandlerStatistics(subscription);
                }
                catch (Exception ex)
                {
                    HandleHandlerException(ex, subscription, @event);
                }
            }

            // Обновление статистики события
            UpdateEventStatistics(eventType, startTime, handlerCount);
        }
        catch (Exception ex)
        {
            HandlePublishException(ex, @event);
        }
    }

    /// <summary>
    /// Опубликовать событие асинхронно
    /// </summary>
    public async Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : IAppEvent
    {
        ValidateEvent(@event);

        if (!_isAsyncProcessingEnabled)
        {
            Publish(@event);
            return;
        }

        var eventType = typeof(TEvent);
        var taskCompletionSource = new TaskCompletionSource<bool>();

        // Добавляем в очередь асинхронной обработки
        _asyncEventQueue.Enqueue(new AsyncEventTask
        {
            Event = @event,
            EventType = eventType,
            TaskCompletionSource = taskCompletionSource,
            CancellationToken = cancellationToken
        });

        // Ждем завершения обработки с таймаутом
        var timeoutTask = Task.Delay(_maxAsyncProcessingTimeMs, cancellationToken);
        var completedTask = await Task.WhenAny(taskCompletionSource.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            _logger.LogWarning($"[EventBus] Async event processing timeout: {eventType.Name}");
            throw new TimeoutException($"Event processing timeout: {eventType.Name}");
        }

        await taskCompletionSource.Task;
    }

    /// <summary>
    /// Количество подписчиков на тип события
    /// </summary>
    public int GetSubscriberCount<TEvent>()
        where TEvent : IAppEvent
    {
        var eventType = typeof(TEvent);
        if (_subscriptions.TryGetValue(eventType, out var subscriptions))
        {
            return subscriptions.Count;
        }
        return 0;
    }

    /// <summary>
    /// Освобождение ресурсов
    /// </summary>
    public void Dispose()
    {
        if (_isDisposing) return;

        _isDisposing = true;
        _logger.Log("[EventBus] Disposing...");

        // Остановка обработки асинхронных событий
        StopAsyncProcessing();

        // Очистка всех подписок
        ClearAllSubscriptions();

        // Очистка истории и статистики
        ClearHistoryAndStatistics();

        // Освобождение disposable ресурсов
        foreach (var disposable in _disposables)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EventBus] Error disposing resource: {ex.Message}");
            }
        }

        _disposables.Clear();

        _isInitialized = false;
        _logger.Log("[EventBus] Disposed");
    }

    #region Приватные методы

    /// <summary>
    /// Инициализация подписок по умолчанию
    /// </summary>
    private void InitializeDefaultSubscriptions()
    {
        // Подписка на системные события
        Subscribe<PerformanceMetricsEvent>(OnPerformanceMetricsReceived, EventPriority.SystemHigh);
        Subscribe<CriticalErrorEvent>(OnCriticalError, EventPriority.SystemCritical);
        Subscribe<AppStateChangedEvent>(OnAppStateChanged, EventPriority.SystemNormal);

        _logger.Log("[EventBus] Default subscriptions initialized");
    }

    /// <summary>
    /// Запуск обработки асинхронных событий
    /// </summary>
    private void StartAsyncProcessing()
    {
        _processingCts = new CancellationTokenSource();
        _processingTask = Task.Run(async () => await ProcessAsyncEventsAsync(_processingCts.Token));

        _logger.Log("[EventBus] Async event processing started");
    }

    /// <summary>
    /// Остановка обработки асинхронных событий
    /// </summary>
    private void StopAsyncProcessing()
    {
        try
        {
            _processingCts?.Cancel();

            if (_processingTask != null)
            {
                Task.WaitAny(_processingTask, Task.Delay(2000));

                if (!_processingTask.IsCompleted)
                {
                    _logger.LogWarning("[EventBus] Async processing task didn't complete in time");
                }

                _processingTask.Dispose();
            }

            _processingCts?.Dispose();

            _logger.Log("[EventBus] Async event processing stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[EventBus] Error stopping async processing: {ex.Message}");
        }
    }

    /// <summary>
    /// Подписка на события производительности
    /// </summary>
    private void SubscribeToPerformanceEvents()
    {
        // Регулярная проверка производительности
        var timer = new System.Threading.Timer(_ =>
        {
            if (_isInitialized && !_isDisposing)
            {
                var metrics = new PerformanceMetricsEvent
                {
                    //FPS = _performanceMonitor.CurrentFPS,
                    //MemoryMB = _performanceMonitor.MemoryUsageMB,
                    FPS = 0,
                    MemoryMB = 0,
                    EventQueueSize = _asyncEventQueue.Count,
                    SubscriberCount = _subscriptions.Sum(kv => kv.Value.Count),
                    Timestamp = DateTime.UtcNow
                };

                Publish(metrics);
            }
        }, null, 5000, 5000);

        _disposables.Add(timer);
    }

    /// <summary>
    /// Валидация обработчика
    /// </summary>
    private void ValidateHandler<TEvent>(Action<TEvent> handler)
        where TEvent : IAppEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler), "Event handler cannot be null");

        if (_isDisposing)
            throw new ObjectDisposedException(nameof(EventBus), "Cannot subscribe while disposing");
    }

    /// <summary>
    /// Валидация события
    /// </summary>
    private void ValidateEvent<TEvent>(TEvent @event)
        where TEvent : IAppEvent
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event), "Event cannot be null");

        if (!_isInitialized)
            throw new InvalidOperationException("EventBus is not initialized");

        if (_isDisposing)
            throw new ObjectDisposedException(nameof(EventBus), "Cannot publish while disposing");
    }

    /// <summary>
    /// Получить информацию о вызывающем коде
    /// </summary>
    private string GetCallerInfo()
    {
#if UNITY_EDITOR
        try
        {
            var stackTrace = new System.Diagnostics.StackTrace(2, true);
            var frame = stackTrace.GetFrame(0);
            var method = frame?.GetMethod();
            var type = method?.DeclaringType;
            return $"{type?.FullName}.{method?.Name}";
        }
        catch
        {
            return "Unknown";
        }
#else
        return "Unknown (Release)";
#endif
    }

    /// <summary>
    /// Отписаться от события
    /// </summary>
    private void Unsubscribe(SubscriptionInfo subscription)
    {
        if (subscription == null) return;

        var eventType = subscription.EventType;

        if (_subscriptions.TryGetValue(eventType, out var subscriptions))
        {
            lock (subscriptions)
            {
                subscriptions.Remove(subscription);

                // Удаляем пустой список
                if (subscriptions.Count == 0)
                {
                    _subscriptions.TryRemove(eventType, out _);
                }
            }

            if (_isLoggingEnabled)
            {
                LogSubscriptionRemoved(eventType, subscription);
            }
        }
    }

    /// <summary>
    /// Логирование добавления подписки
    /// </summary>
    private void LogSubscriptionAdded(Type eventType, SubscriptionInfo subscription)
    {
        var logEntry = new EventLogEntry
        {
            Type = EventLogType.SubscriptionAdded,
            EventType = eventType.Name,
            HandlerInfo = subscription.CallerInfo,
            Priority = subscription.Priority,
            Timestamp = DateTime.UtcNow
        };

        AddToEventLog(logEntry);

        if (_logger.IsDebugEnabled)
        {
            _logger.Log($"[EventBus] Subscription added: {eventType.Name} -> {subscription.CallerInfo}");
        }
    }

    /// <summary>
    /// Логирование удаления подписки
    /// </summary>
    private void LogSubscriptionRemoved(Type eventType, SubscriptionInfo subscription)
    {
        var logEntry = new EventLogEntry
        {
            Type = EventLogType.SubscriptionRemoved,
            EventType = eventType.Name,
            HandlerInfo = subscription.CallerInfo,
            Timestamp = DateTime.UtcNow
        };

        AddToEventLog(logEntry);

        if (_logger.IsDebugEnabled)
        {
            _logger.Log($"[EventBus] Subscription removed: {eventType.Name} -> {subscription.CallerInfo}");
        }
    }

    /// <summary>
    /// Логирование публикации события
    /// </summary>
    private void LogEventPublished<TEvent>(TEvent @event, Type eventType)
        where TEvent : IAppEvent
    {
        var logEntry = new EventLogEntry
        {
            Type = EventLogType.EventPublished,
            EventType = eventType.Name,
            EventData = @event.ToString(),
            Timestamp = DateTime.UtcNow
        };

        AddToEventLog(logEntry);

        if (_logger.IsDebugEnabled)
        {
            _logger.Log($"[EventBus] Event published: {eventType.Name}");
        }
    }

    /// <summary>
    /// Сохранение события в историю
    /// </summary>
    private void SaveToHistory<TEvent>(Type eventType, TEvent @event)
        where TEvent : IAppEvent
    {
        var historyList = _eventHistory.GetOrAdd(eventType, _ => new ConcurrentQueue<TEvent>()) as ConcurrentQueue<TEvent>;

        if (historyList != null)
        {
            historyList.Enqueue(@event);

            // Ограничение размера истории
            while (historyList.Count > MaxHistoryPerEvent)
            {
                historyList.TryDequeue(out _);
            }
        }
    }

    /// <summary>
    /// Обновление статистики подписки
    /// </summary>
    private void UpdateStatistics(Type eventType, SubscriptionInfo subscription)
    {
        var stats = _statistics.GetOrAdd(eventType, _ => new EventStatistics());
        stats.TotalSubscriptions++;
        stats.ActiveSubscriptions++;
    }

    /// <summary>
    /// Обновление статистики обработчика
    /// </summary>
    private void UpdateHandlerStatistics(SubscriptionInfo subscription)
    {
        var stats = _statistics.GetOrAdd(subscription.EventType, _ => new EventStatistics());
        stats.TotalHandledEvents++;

        subscription.IncrementHandledCount();
    }

    /// <summary>
    /// Обновление статистики события
    /// </summary>
    private void UpdateEventStatistics(Type eventType, DateTime startTime, int handlerCount)
    {
        var stats = _statistics.GetOrAdd(eventType, _ => new EventStatistics());
        stats.TotalPublishedEvents++;
        stats.LastPublishTime = DateTime.UtcNow;
        stats.LastPublishDuration = DateTime.UtcNow - startTime;
        stats.LastHandlerCount = handlerCount;
    }

    /// <summary>
    /// Обработка исключения в обработчике
    /// </summary>
    private void HandleHandlerException(Exception ex, SubscriptionInfo subscription, IAppEvent @event)
    {
        var errorEvent = new HandlerErrorEvent
        {
            Exception = ex,
            Event = @event,
            Timestamp = DateTime.UtcNow
        };

        // Публикация события ошибки
        Publish(errorEvent);

        // Логирование
        _logger.LogError($"[EventBus] Handler error in {subscription.CallerInfo}: {ex.Message}");

        // Обновление статистики ошибок
        var stats = _statistics.GetOrAdd(subscription.EventType, _ => new EventStatistics());
        stats.ErrorCount++;
    }

    /// <summary>
    /// Обработка исключения при публикации
    /// </summary>
    private void HandlePublishException<TEvent>(Exception ex, TEvent @event)
        where TEvent : IAppEvent
    {
        var errorEvent = new PublishErrorEvent
        {
            Exception = ex,
            EventType = typeof(TEvent),
            EventData = @event,
            Timestamp = DateTime.UtcNow
        };

        // Публикация события ошибки публикации
        try
        {
            // Используем базовый publish чтобы избежать рекурсии
            InternalPublish(errorEvent);
        }
        catch
        {
            // Если не удалось опубликовать, хотя бы залогируем
            _logger.LogError($"[EventBus] Publish error for {typeof(TEvent).Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Внутренняя публикация (без валидации и логирования)
    /// </summary>
    private void InternalPublish<TEvent>(TEvent @event)
        where TEvent : IAppEvent
    {
        var eventType = typeof(TEvent);

        if (_subscriptions.TryGetValue(eventType, out var subscriptions))
        {
            foreach (var subscription in subscriptions.ToArray())
            {
                if (subscription.ShouldHandle(@event))
                {
                    subscription.Handle(@event);
                }
            }
        }
    }

    /// <summary>
    /// Добавление записи в лог событий
    /// </summary>
    private void AddToEventLog(EventLogEntry entry)
    {
        _eventLog.Enqueue(entry);

        // Ограничение размера лога
        while (_eventLog.Count > MaxEventLogSize)
        {
            _eventLog.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Очистка всех подписок
    /// </summary>
    private void ClearAllSubscriptions()
    {
        foreach (var kvp in _subscriptions)
        {
            lock (kvp.Value)
            {
                kvp.Value.Clear();
            }
        }

        _subscriptions.Clear();
        _logger.Log("[EventBus] All subscriptions cleared");
    }

    /// <summary>
    /// Очистка истории и статистики
    /// </summary>
    private void ClearHistoryAndStatistics()
    {
        _eventHistory.Clear();
        _statistics.Clear();
        _eventLog.Clear();
    }

    /// <summary>
    /// Процесс асинхронной обработки событий
    /// </summary>
    private async Task ProcessAsyncEventsAsync(CancellationToken cancellationToken)
    {
        _logger.Log("[EventBus] Starting async event processing loop");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_asyncEventQueue.TryDequeue(out var asyncTask))
                {
                    await ProcessSingleAsyncEvent(asyncTask, cancellationToken);
                }
                else
                {
                    // Если очередь пуста, ждем немного
                    await Task.Delay(10, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EventBus] Async processing error: {ex.Message}");
                await Task.Delay(100, cancellationToken);
            }
        }

        _logger.Log("[EventBus] Async event processing loop stopped");
    }

    /// <summary>
    /// Обработка одного асинхронного события
    /// </summary>
    private async Task ProcessSingleAsyncEvent(AsyncEventTask asyncTask, CancellationToken cancellationToken)
    {
        try
        {
            // Проверка отмены
            if (cancellationToken.IsCancellationRequested ||
                asyncTask.CancellationToken.IsCancellationRequested)
            {
                asyncTask.TaskCompletionSource?.SetCanceled();
                return;
            }

            // Публикация события
            var publishTask = Task.Run(() => Publish(asyncTask.Event), cancellationToken);

            // Ожидание завершения с таймаутом
            var completedTask = await Task.WhenAny(
                publishTask,
                Task.Delay(_maxAsyncProcessingTimeMs, cancellationToken)
            );

            if (completedTask == publishTask)
            {
                asyncTask.TaskCompletionSource?.SetResult(true);
            }
            else
            {
                asyncTask.TaskCompletionSource?.SetException(
                    new TimeoutException($"Async event processing timeout: {asyncTask.EventType.Name}"));
            }
        }
        catch (Exception ex)
        {
            asyncTask.TaskCompletionSource?.SetException(ex);
        }
    }

    #endregion

    #region Обработчики системных событий

    private void OnPerformanceMetricsReceived(PerformanceMetricsEvent e)
    {
        // Адаптация настроек на основе метрик
        if (e.FPS < 45)
        {
            // При низком FPS уменьшаем логирование
            _isLoggingEnabled = e.FPS > 30;
        }

        if (e.EventQueueSize > 50)
        {
            // При большой очереди увеличиваем приоритет обработки
            _maxAsyncProcessingTimeMs = Math.Max(500, _maxAsyncProcessingTimeMs - 100);
        }
    }

    private void OnCriticalError(CriticalErrorEvent e)
    {
        // При критической ошибке приостанавливаем асинхронную обработку
        _isAsyncProcessingEnabled = false;

        // Очищаем очередь
        while (_asyncEventQueue.TryDequeue(out _)) { }

        _logger.LogWarning("[EventBus] Async processing disabled due to critical error");
    }

    private void OnAppStateChanged(AppStateChangedEvent e)
    {
        // Адаптация настроек шины в зависимости от состояния приложения
        switch (e.NewState)
        {
            case AppState.Flight:
                // В режиме полета приоритет - производительность
                _isLoggingEnabled = false;
                _isHistoryEnabled = false;
                break;

            case AppState.Assembly:
                // В режиме сборки включаем детальное логирование
                _isLoggingEnabled = true;
                _isHistoryEnabled = true;
                break;

            case AppState.MainMenu:
                // В меню баланс между функциональностью и производительностью
                _isLoggingEnabled = true;
                _isHistoryEnabled = false;
                break;
        }
    }

   

    #endregion

    #region Вспомогательные классы

    /// <summary>
    /// Базовый класс информации о подписке
    /// </summary>
    private abstract class SubscriptionInfo
    {
        public Type EventType { get; protected set; }
        public EventPriority Priority { get; }
        public string CallerInfo { get; }
        public int HandledCount { get; private set; }

        protected SubscriptionInfo(Type eventType, EventPriority priority, string callerInfo)
        {
            EventType = eventType;
            Priority = priority;
            CallerInfo = callerInfo;
        }

        public abstract bool ShouldHandle(IAppEvent @event);
        public abstract void Handle(IAppEvent @event);

        public void IncrementHandledCount() => HandledCount++;
    }

    /// <summary>
    /// Информация о подписке на конкретный тип события
    /// </summary>
    private class SubscriptionInfo<TEvent> : SubscriptionInfo
        where TEvent : IAppEvent
    {
        private readonly Action<TEvent> _handler;
        private readonly Func<TEvent, bool> _filter;

        public SubscriptionInfo(
            Action<TEvent> handler,
            Func<TEvent, bool> filter,
            EventPriority priority,
            string callerInfo)
            : base(typeof(TEvent), priority, callerInfo)
        {
            _handler = handler;
            _filter = filter;
        }

        public override bool ShouldHandle(IAppEvent @event)
        {
            if (@event is TEvent typedEvent)
            {
                return _filter == null || _filter(typedEvent);
            }
            return false;
        }

        public override void Handle(IAppEvent @event)
        {
            if (@event is TEvent typedEvent)
            {
                _handler(typedEvent);
            }
        }
    }

    /// <summary>
    /// Disposable для отписки
    /// </summary>
    private class DisposableSubscription : IDisposable
    {
        private readonly Action _unsubscribeAction;
        private bool _isDisposed;

        public DisposableSubscription(Action unsubscribeAction)
        {
            _unsubscribeAction = unsubscribeAction;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _unsubscribeAction?.Invoke();
                _isDisposed = true;
            }
        }
    }

    /// <summary>
    /// Задача асинхронной обработки события
    /// </summary>
    private class AsyncEventTask
    {
        public IAppEvent Event { get; set; }
        public Type EventType { get; set; }
        public TaskCompletionSource<bool> TaskCompletionSource { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }

    /// <summary>
    /// Статистика по событиям
    /// </summary>
    private class EventStatistics
    {
        public int TotalPublishedEvents { get; set; }
        public int TotalHandledEvents { get; set; }
        public int TotalSubscriptions { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int ErrorCount { get; set; }
        public DateTime LastPublishTime { get; set; }
        public TimeSpan LastPublishDuration { get; set; }
        public int LastHandlerCount { get; set; }
    }

    /// <summary>
    /// Запись в логе событий
    /// </summary>
    private class EventLogEntry
    {
        public EventLogType Type { get; set; }
        public string EventType { get; set; }
        public string HandlerInfo { get; set; }
        public string EventData { get; set; }
        public EventPriority Priority { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Тип записи в логе
    /// </summary>
    private enum EventLogType
    {
        SubscriptionAdded,
        SubscriptionRemoved,
        EventPublished,
        HandlerError,
        PublishError
    }

    #endregion

    #region События шины

    /// <summary>
    /// Событие метрик производительности
    /// </summary>
    public class PerformanceMetricsEvent : IAppEvent
    {
        public float FPS { get; set; }
        public float MemoryMB { get; set; }
        public int EventQueueSize { get; set; }
        public int SubscriberCount { get; set; }
        public DateTime Timestamp { get; set; }

        public string EventId { get; set; } = "PerformanceMetricsEvent";
    }

    /// <summary>
    /// Событие ошибки в обработчике
    /// </summary>
    public class HandlerErrorEvent : IAppEvent
    {
        public Exception Exception { get; set; }
        //public SubscriptionInfo Subscription { get; set; }
        public IAppEvent Event { get; set; }
        public DateTime Timestamp { get; set; }
        public string EventId { get; set; } = "HandlerErrorEvent";
        private SubscriptionInfo Subscription { get; set; }

    }

    /// <summary>
    /// Событие ошибки публикации
    /// </summary>
    public class PublishErrorEvent : IAppEvent
    {
        public Exception Exception { get; set; }
        public Type EventType { get; set; }
        public IAppEvent EventData { get; set; }
        public DateTime Timestamp { get; set; }
        public string EventId { get; set; } = "PublishErrorEvent";
    }

    #endregion
}
