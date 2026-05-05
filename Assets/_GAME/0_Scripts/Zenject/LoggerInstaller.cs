using UnityEngine;
using Zenject;

public class LoggerInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<IAppLogger>()
            .To<ConsoleLogger>()
            .AsSingle()
            .NonLazy();

        Debug.Log("[LoggerInstaller] ConsoleLogger installed");
    }
}