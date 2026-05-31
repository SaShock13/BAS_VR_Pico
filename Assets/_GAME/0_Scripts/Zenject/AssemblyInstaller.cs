using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class AssemblyInstaller : MonoInstaller
{
    [SerializeField] private List<PartConfig> partConfigs;

    public override void InstallBindings()
    {
        Container.Bind<IPartConfigRegistry>().To<PartConfigRepository>().AsSingle().WithArguments(partConfigs);
        Container.Bind<IPartFactory>().To<PartFactory>().AsSingle();
        Container.Bind<ISocketResolver>().To<SocketRegistry>().AsSingle();

        Container.Bind<ISaveService>().To<JsonSaveService>().AsSingle();
        Container.Bind<IGarageService>().To<GarageService>().AsSingle();
        Container.Bind<IGarageSaveService>().To<JsonGarageSaveService>().AsSingle();

        Container.BindInterfacesAndSelfTo<PartViewRegistry>().AsSingle();
        Container.BindInterfacesAndSelfTo<Clean_AssemblySystem>().AsSingle();
        Container.BindInterfacesAndSelfTo<SelectionService>().AsSingle();
        Container.BindInterfacesAndSelfTo<PartHighlightService>().AsSingle();



        // Selection Install
        Container.Bind<ISelectionService>().To<NewSelectionService>().AsSingle();
        Container.BindInterfacesAndSelfTo<InspectorService>().AsSingle();


        Container.Bind<INotificationService>().To<NotificationService>().AsSingle();



    }
}