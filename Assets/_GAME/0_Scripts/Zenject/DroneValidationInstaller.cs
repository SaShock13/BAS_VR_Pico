using UnityEngine;
using Zenject;

public class DroneValidationInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<DroneFocusEffect>().FromComponentInHierarchy().AsSingle();
        Container.Bind<BackgroundFadeEffect>().FromComponentInHierarchy().AsSingle();
        Container.Bind<ValidateEffectsSystem>().AsSingle();
    }

    public static void InstallValidationBindings(DiContainer container)
    {
        container.BindInterfacesAndSelfTo<DroneValidationStateService>().AsSingle();
        container.Bind<IDroneAnalyzer>()
            .To<DroneAnalyzer>()
            .AsSingle();

        container.Bind<IDroneValidator>()
            .To<ThrustValidator>()
            .AsSingle();

        container.Bind<IDroneValidator>()
            .To<CenterOfMassValidator>()
            .AsSingle();

        container.Bind<IDroneValidator>()
            .To<FlightTimeValidator>()
            .AsSingle();

        container.Bind<IDroneValidator>()
            .To<CollisionValidator>()
            .AsSingle();
        container.Bind<IDroneValidator>()
            .To<CompletenessValidator>()
            .AsSingle();




        container.Bind<DroneValidationService>()
            .AsSingle();

        container.Bind<StructuralValidator>()
            .AsSingle();

        container.Bind<DroneReadinessService>()
            .AsSingle();
    }
}
