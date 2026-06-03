using UnityEngine;
using Zenject;

public class DroneValidationInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<IDroneAnalyzer>()
            .To<DroneAnalyzer>()
            .AsSingle();

        Container.Bind<IDroneStatsCalculator>()
            .To<DroneStatsCalculator>()
            .AsSingle();

        Container.Bind<IDroneValidator>()
            .To<ThrustValidator>()
            .AsSingle();

        Container.Bind<IDroneValidator>()
            .To<CenterOfMassValidator>()
            .AsSingle();

        Container.Bind<IDroneValidator>()
            .To<FlightTimeValidator>()
            .AsSingle();

        Container.Bind<IDroneValidator>()
            .To<CollisionValidator>()
            .AsSingle();




        Container.Bind<DroneValidationService>()
            .AsSingle();

        Container.Bind<StructuralValidator>()
            .AsSingle();

        Container.Bind<DroneReadinessService>()
            .AsSingle();
    }
}