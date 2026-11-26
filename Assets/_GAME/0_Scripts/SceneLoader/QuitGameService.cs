using System;
using UnityEngine;
using Zenject;

public sealed class QuitGameRequest : IAppEvent
{
    public string EventId => nameof(QuitGameRequest);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed class QuitGameService : IInitializable, IDisposable
{
    private readonly IEventBus _bus;
    private IDisposable _subscription;

    public QuitGameService(IEventBus bus) => _bus = bus;
    public void Initialize() => _subscription = _bus.Subscribe<QuitGameRequest>(_ => Application.Quit());
    public void Dispose() => _subscription?.Dispose();
}
