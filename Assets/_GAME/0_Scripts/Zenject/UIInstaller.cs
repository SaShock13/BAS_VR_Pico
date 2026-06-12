using UnityEngine;
using Zenject;

public class UIInstaller : MonoInstaller
{
    [SerializeField]
    private HintScenarioDefinition _hintScenarioDefinition;


    public override void InstallBindings()
    {
        Container.Bind<INotificationService>().To<NotificationService>().AsSingle();
        Container.Bind<ITabletService>().To<TabletService>().AsSingle();
        Container.Bind<IHintService>().To<HintService>().AsSingle();

        Container.BindInterfacesTo<HintScenarioController>()
        .AsSingle();

        Container.BindInstance(_hintScenarioDefinition)
            .AsSingle();
    }
}