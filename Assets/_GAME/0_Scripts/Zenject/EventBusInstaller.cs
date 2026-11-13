using UnityEngine;
using Zenject;

public class EventBusInstaller : MonoInstaller
{
    [SerializeField] private bool installMenuNavigation;
    [SerializeField] private AssemblyEnvironmentCatalog assemblyEnvironmentCatalog;
    [SerializeField] private bool installFlightMenu;
    [SerializeField] private SFXLibrary sfxLibrary;

    public override void InstallBindings()
    {
        Container.Bind<IEventBus>()
            .To<EventBus>()
            .AsSingle()
            .NonLazy();

        Container.BindInitializableExecutionOrder<IEventBus>(-100); 
        Container.BindInterfacesTo<HapticFeedbackService>().AsSingle();
        Container.BindInterfacesTo<MenuHapticFeedback>().AsSingle().WithArguments(gameObject.scene);
        if (sfxLibrary != null)
        {
            Container.BindInstance(sfxLibrary);
            Container.BindInterfacesTo<SFXPlayer>().AsSingle();
            Container.BindInterfacesTo<SFXService>().AsSingle();
        }
        if (installFlightMenu)
            Container.BindInterfacesTo<FlightMenuService>().AsSingle();
        if (installMenuNavigation)
        {
            if (assemblyEnvironmentCatalog != null) Container.BindInstance(assemblyEnvironmentCatalog);
            Container.BindInterfacesTo<SceneLoader>().AsSingle();
            Container.BindInterfacesTo<MenuNavigationService>().AsSingle();
            Container.BindInterfacesTo<AssemblyProjectMenuService>().AsSingle();
        }
        Debug.Log("[EventBusInstaller] EventBus installed");
    }
}
