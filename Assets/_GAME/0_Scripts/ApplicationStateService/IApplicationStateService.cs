using System.Threading.Tasks;
using System;
using Zenject;

/// <summary>
/// Сервис управления состояниями приложения
/// Координирует переходы между режимами работы
/// </summary>
public interface IApplicationStateService : IInitializable
{
    /// <summary>Текущее состояние приложения</summary>
    AppState CurrentState { get; }

    /// <summary>Предыдущее состояние приложения</summary>
    AppState PreviousState { get; }

    /// <summary>Приложение на паузе</summary>
    bool IsPaused { get; }

    /// <summary>
    /// Запросить изменение состояния
    /// </summary>
    /// <param name="targetState">Целевое состояние</param>
    /// <param name="context">Контекст перехода (опционально)</param>
    /// <returns>Успешность инициализации перехода</returns>
    Task<StateTransitionResult> RequestStateChangeAsync(
        AppState targetState,
        StateTransitionContext context = null);

    /// <summary>
    /// Перевести приложение в режим паузы
    /// </summary>
    /// <param name="pauseInitiator">Причина паузы</param>
    void PauseApplication(PauseInitiator pauseInitiator = PauseInitiator.User);

    /// <summary>
    /// Возобновить работу приложения
    /// </summary>
    void ResumeApplication();

    /// <summary>
    /// Событие изменения состояния приложения
    /// </summary>
    event Action<AppStateChangedEvent> OnAppStateChanged;

    /// <summary>
    /// Событие изменения состояния паузы
    /// </summary>
    event Action<PauseStateChangedEvent> OnPauseStateChanged;
}

/// <summary>
/// Контекст перехода между состояниями
/// </summary>
public class StateTransitionContext
{
    /// <summary>Источник запроса перехода</summary>
    public TransitionSource Source { get; set; } = TransitionSource.User;

    /// <summary>Дополнительные данные для перехода</summary>
    public object Data { get; set; }

    /// <summary>Пропустить валидацию (только для системных переходов)</summary>
    public bool SkipValidation { get; set; } = false;

    /// <summary>Коллбэк при успешном переходе</summary>
    public Action OnSuccess { get; set; }

    /// <summary>Коллбэк при ошибке перехода</summary>
    public Action<Exception> OnError { get; set; }
}