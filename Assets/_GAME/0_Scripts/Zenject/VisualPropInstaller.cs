using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class VisualPropInstaller : MonoInstaller
{
    [Header( "Список доступных материалов")]
    [SerializeField] private List<MaterialDefinition> materials;  


    public override void InstallBindings()
    {
        Container.Bind<IMaterialRegistry>().To<MaterialRegistry>().AsSingle().WithArguments(materials);
        Container.Bind<IVisualPresetRepository>().To<VisualPresetRepository>().AsSingle();
    }
}