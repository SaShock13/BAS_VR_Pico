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

        Container.Bind<ISaveService>().To<JsonSaveService>().AsSingle();
        Container.Bind<IGarageService>().To<GarageService>().AsSingle();
        Container.Bind<IGarageSaveService>().To<JsonGarageSaveService>().AsSingle();

        Container.BindInterfacesAndSelfTo<PartViewRegistry>().AsSingle();
        Container.Bind<ISelectionService>().To<SelectionService>().AsSingle();

        Container.BindInterfacesAndSelfTo<Clean_AssemblySystem>().AsSingle();
        Container.BindInterfacesAndSelfTo<PartHighlightService>().AsSingle();

        Container.BindInterfacesAndSelfTo<PartTransformAdjustmentService>().AsSingle();

        Container.BindInterfacesAndSelfTo<UserActivityService>().AsSingle();

        // Selection Install
        Container.BindInterfacesAndSelfTo<InspectorService>().AsSingle();
        var configsRepository = Container.Resolve<IPartConfigRepository>();
        var viewsRepository = Container.Resolve<PartViewRegistry>();
        Container.BindInterfacesAndSelfTo<DronePhysicsStatsBuilder>().AsSingle().WithArguments( configsRepository, viewsRepository);


        



    }
}