using Unity.XR.CoreUtils;
using UnityEngine;
using Zenject;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public sealed class GaragePortalTrigger : MonoBehaviour
{
    [SerializeField] private bool returnToAssembly;
    private IEventBus _events;

    [Inject]
    public void Construct(IEventBus events) => _events = events;

    private void OnTriggerEnter(Collider other)
    {
        // Hands and grabbed drone parts must not cause scene transitions.
        if (!isActiveAndEnabled || _events == null || !(other is CharacterController)) return;
        if (other.GetComponent<XROrigin>() == null) return;
        _events.Publish(new GaragePortalRequest { ReturnToAssembly = returnToAssembly });
    }
}
