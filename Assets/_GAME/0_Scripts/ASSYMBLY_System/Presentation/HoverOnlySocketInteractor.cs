using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HoverOnlySocketInteractor : XRSocketInteractor
{
    [SerializeField, Tooltip("Optional mount region. The part pivot must enter it; contact by a distant end of the part is insufficient.")]
    private SphereCollider _mountProximityCollider;

    [SerializeField] private HoverOnlySocketInteractor[] _mountCompetitors = System.Array.Empty<HoverOnlySocketInteractor>();

    public override bool CanHover(IXRHoverInteractable interactable)
    {
        if (!base.CanHover(interactable)) return false;
        if (_mountProximityCollider == null) return true;
        if (interactable is IXRSelectInteractable selectable && !selectable.isSelected) return false;
        return IsMountCandidate(interactable.transform);
    }

    internal bool IsMountCandidate(Transform part)
    {
        if (_mountProximityCollider == null) return true;
        var region = _mountProximityCollider.transform;
        var scale = region.lossyScale;
        float radius = _mountProximityCollider.radius *
            Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        var offset = part.position - region.TransformPoint(_mountProximityCollider.center);
        float distance = offset.sqrMagnitude;
        if (distance > radius * radius) return false;

        // Wider acquisition regions may overlap. Only the closest compatible mount gets a preview.
        foreach (var other in _mountCompetitors)
        {
            if (other == null || !other.isActiveAndEnabled || other._mountProximityCollider == null) continue;
            var collider = other._mountProximityCollider;
            float otherDistance = (part.position - collider.transform.TransformPoint(collider.center)).sqrMagnitude;
            if (otherDistance < distance ||
                (otherDistance == distance && other.GetInstanceID() < GetInstanceID())) return false;
        }
        return true;
    }

    public override bool CanSelect(IXRSelectInteractable interactable)
    {
        return false;
    }
}
