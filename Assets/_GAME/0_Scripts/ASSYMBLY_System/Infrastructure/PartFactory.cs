using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PartFactory : IPartFactory
{
    public GameObject Create(PartConfig config, Vector3 position , Quaternion rotation)
    {
        GameObject instance = Object.Instantiate(
            config.Prefab,
            position,
            rotation
        );

        // Физика пока не нужна
        //Rigidbody rb = instance.AddComponent<Rigidbody>();
        //rb.mass = config.Mass;

        // XR настройки, сожет прямо на префабе будет уже настроено?
        //XRGrabInteractable grab = instance.AddComponent<XRGrabInteractable>();
        //grab.throwOnDetach = false;

        return instance;
    }
}
