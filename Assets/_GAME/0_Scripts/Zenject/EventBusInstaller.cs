using UnityEngine;
using Zenject;

public class EventBusInstaller : MonoInstaller
{
    [SerializeField] private bool installMenuNavigation;

    public override void InstallBindings()
    {
        Container.Bind<IEventBus>()
            .To<EventBus>()
            .AsSingle()
            .NonLazy();

        Container.BindInitializableExecutionOrder<IEventBus>(-100); 
        if (installMenuNavigation)
        {
            Container.BindInterfacesTo<SceneLoader>().AsSingle();
            Container.BindInterfacesTo<MenuNavigationService>().AsSingle();
            Container.BindInterfacesTo<AssemblyProjectMenuService>().AsSingle();
        }
        Debug.Log("[EventBusInstaller] EventBus installed");
    }
}
