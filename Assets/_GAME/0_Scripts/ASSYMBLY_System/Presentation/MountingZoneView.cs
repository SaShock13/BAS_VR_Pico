using UnityEngine;

public sealed class MountingZoneView : MountPoint
{
    [SerializeField] private string _mountId = "zone/top";
    [SerializeField] private PartType[] _allowedTypes;
    [SerializeField] private Vector2 _size = new Vector2(0.5f, 0.5f);
    [SerializeField, Min(0.001f)] private float _hoverHeight = 0.25f;
    [SerializeField, Min(0)] private float _hoverPadding = 0.05f;

    public override string MountId => _mountId;
    public override PartType[] AcceptedTypes => _allowedTypes;
    private DronePartView _owner;
    public override DronePartView Owner => _owner;

    internal void Initialize(DronePartView owner) => _owner = owner;

    internal bool TryProject(DronePartView part, out Vector3 localPosition,
        out Bounds volume, out bool fits, out float distance)
    {
        var target = transform.InverseTransformPoint(part.transform.position);
        var halfSize = _size * 0.5f;
        localPosition = default;
        volume = default;
        fits = false;
        distance = default;

        if (!part.HasMountingBounds) return false;

        var scale = transform.lossyScale;
        if (scale.x <= 0 || scale.y <= 0 || scale.z <= 0) return false;
        var partScale = part.transform.lossyScale;
        var relativeScale = new Vector3(partScale.x / scale.x, partScale.y / scale.y, partScale.z / scale.z);
        var geometry = MountingBounds.Transform(part.LocalMountingBounds, Matrix4x4.Scale(relativeScale));
        // Test proximity at the underside so an off-center model pivot does not affect pickup.
        var heldBounds = MountingBounds.Transform(part.LocalMountingBounds,
            transform.worldToLocalMatrix * part.transform.localToWorldMatrix);
        float height = heldBounds.min.y;
        if (Mathf.Abs(heldBounds.center.x) > halfSize.x + _hoverPadding ||
            Mathf.Abs(heldBounds.center.z) > halfSize.y + _hoverPadding ||
            heldBounds.center.y < 0 || Mathf.Abs(height) > _hoverHeight) return false;

        float minX = -halfSize.x - geometry.min.x;
        float maxX = halfSize.x - geometry.max.x;
        float minZ = -halfSize.y - geometry.min.z;
        float maxZ = halfSize.y - geometry.max.z;
        fits = minX <= maxX && minZ <= maxZ;
        localPosition = new Vector3(
            minX <= maxX ? Mathf.Clamp(target.x, minX, maxX) : -geometry.center.x,
            -geometry.min.y,
            minZ <= maxZ ? Mathf.Clamp(target.z, minZ, maxZ) : -geometry.center.z);
        volume = new Bounds(localPosition + geometry.center, geometry.size);
        distance = (part.transform.position - transform.TransformPoint(localPosition)).sqrMagnitude;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        var previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(_size.x, 0, _size.y));
        Gizmos.matrix = previous;
    }
}
