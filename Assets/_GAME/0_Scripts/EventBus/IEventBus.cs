using System;
using System.Threading;
using System.Threading.Tasks;
using Zenject;

/// <summary>
/// Шина событий для межсистемной коммуникации
/// </summary>
public interface IEventBus : IInitializable, IDisposable
{
    /// <summary>
    /// Подписаться на события типа TEvent
    /// </summary>
    /// <typeparam name="TEvent">Тип события</typeparam>
    /// <param name="handler">Обработчик события</param>
    /// <param name="priority">Приоритет обработки (выше = раньше)</param>
    IDisposable Subscribe<TEvent>(
        Action<TEvent> handler,
        EventPriority priority = EventPriority.Normal) where TEvent : IAppEvent;

    /// <summary>
    /// Подписаться на события с фильтром
    /// </summary>
    IDisposable Subscribe<TEvent>(
        Action<TEvent> handler,
        Func<TEvent, bool> filter,
        EventPriority priority = EventPriority.Normal) where TEvent : IAppEvent;

    /// <summary>
    /// Опубликовать событие
    /// </summary>
    void Publish<TEvent>(TEvent @event) where TEvent : IAppEvent;

    /// <summary>
    /// Опубликовать событие асинхронно
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IAppEvent;

    /// <summary>
    /// Количество подписчиков на тип события
    /// </summary>
    int GetSubscriberCount<TEvent>() where TEvent : IAppEvent;
}


