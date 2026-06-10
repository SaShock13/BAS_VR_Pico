using UnityEngine;
using Zenject;

public class UIInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<INotificationService>().To<NotificationService>().AsSingle();
        Container.Bind<ITabletService>().To<TabletService>().AsSingle();
    }
}