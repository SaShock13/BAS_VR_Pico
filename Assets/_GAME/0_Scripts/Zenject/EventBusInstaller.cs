using UnityEngine;
using Zenject;

public class EventBusInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<IEventBus>()
            .To<EventBus>()
            .AsSingle()
            .NonLazy();

        Container.BindInitializableExecutionOrder<IEventBus>(-100); 
        Debug.Log("[EventBusInstaller] EventBus installed");
    }
}