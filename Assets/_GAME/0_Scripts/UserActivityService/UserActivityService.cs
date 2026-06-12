using UnityEngine;
using Zenject;

public class UserActivityService : IUserActivityService, ITickable
{
    public float IdleTime { get; private set; }

    [Inject] IEventBus _eventBus;

    public UserActivityService()
    {
        //_eventBus.Subscribe<PartSocketAttachedEvent>(_ => NotifyActivity());
        //_eventBus.Subscribe<PartSocketDetachedEvent>(_ => NotifyActivity());
        //_eventBus.Subscribe<PartVisualChangedEvent>(_ => NotifyActivity());
        //_eventBus.Subscribe<PartSelectedEvent>(_ => NotifyActivity());
        //_eventBus.Subscribe<Clean_PartCreatedEvent>(_ => NotifyActivity());
        //_eventBus.Subscribe<Clean_PartDeletedEvent>(_ => NotifyActivity());
        //_eventBus.Subscribe<Clean_PartCreatedEvent>(_ => NotifyActivity());
    }

    public void NotifyActivity()
    {

        Debug.Log($"Зарегистрирована активность. Время простоя сброшено в 0 {this}");
        IdleTime = 0;
    }

    public void Tick()
    {
        IdleTime += Time.deltaTime;

        Debug.Log($"000000IdleTime {IdleTime}");
    }
}