using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class AssemblyInstaller : MonoInstaller
{
    [SerializeField] private List<PartConfig> partConfigs;

    public override void InstallBindings()
    {
        Container.Bind<IPartConfigRepository>().To<PartConfigRepository>().AsSingle().WithArguments(partConfigs);
        Container.Bind<IPartFactory>().To<PartFactory>().AsSingle();
        Container.Bind<ISocketResolver>().To<SocketRegistry>().AsSingle();

        Container.BindInterfacesAndSelfTo<PartViewRegistry>().AsSingle();
        Container.BindInterfacesAndSelfTo<Clean_AssemblySystem>().AsSingle();
        Container.BindInterfacesAndSelfTo<SelectionService>().AsSingle();
        Container.BindInterfacesAndSelfTo<PartHighlightService>().AsSingle();

        Container.Bind<ISaveService>().To<JsonSaveService>().AsSingle();

    }
}