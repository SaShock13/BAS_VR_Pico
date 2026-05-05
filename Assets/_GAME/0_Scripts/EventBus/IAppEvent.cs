
using System;

/// <summary>
/// Базовый интерфейс для всех событий приложения
/// </summary>
public interface IAppEvent
{
    /// <summary>Уникальный идентификатор события</summary>
    string EventId { get; }

    /// <summary>Время регистрации события</summary>
    DateTime Timestamp { get; set; }

}