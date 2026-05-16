using UnityEngine;
using Zenject;

public class AddressablesLoaderInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<AddressablesAssetService>()
            .AsSingle();

        Container.Bind<AddressablesPrefabService>()
            .AsSingle();
    }
}