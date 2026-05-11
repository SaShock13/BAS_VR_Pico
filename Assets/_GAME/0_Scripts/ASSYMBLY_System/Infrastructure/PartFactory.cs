using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Zenject;

public class PartFactory : IPartFactory
{
    private readonly DiContainer _container;

    [Inject]
    public PartFactory(DiContainer container)
    {
        _container = container;
    } 

    public GameObject Create(PartConfig config, Vector3 position , Quaternion rotation)
    {
        //GameObject instance = Object.Instantiate(
        //    config.Prefab,
        //    position,
        //    rotation
        //);

        GameObject instance = _container.InstantiatePrefab(
            config.Prefab,
            position,
            rotation,
            null);
        _container.InjectGameObject(instance);

        // Физика пока не нужна
        //Rigidbody rb = instance.AddComponent<Rigidbody>();
        //rb.mass = config.Mass;

        // XR настройки, сожет прямо на префабе будет уже настроено?
        //XRGrabInteractable grab = instance.AddComponent<XRGrabInteractable>();
        //grab.throwOnDetach = false;

        return instance;
    }
}
