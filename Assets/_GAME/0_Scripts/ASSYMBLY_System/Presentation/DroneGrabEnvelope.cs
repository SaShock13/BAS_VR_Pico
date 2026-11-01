using UnityEngine;

// Uses the frame's existing broad collider; no extra physics shape or runtime tick.
public sealed class DroneGrabEnvelope : MonoBehaviour
{
    [SerializeField] private BoxCollider mainCollider;
    private Vector3 _originalCenter;
    private Vector3 _originalSize;
    private bool _initialized;
    private bool _flightMode;

    public BoxCollider MainCollider => mainCollider;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (_initialized || mainCollider == null) return;
        _initialized = true;
        _originalCenter = mainCollider.center;
        _originalSize = mainCollider.size;
    }

    public void SetDetailMode(bool detailMode)
    {
        Initialize();
        if (mainCollider != null && !_flightMode) mainCollider.enabled = !detailMode;
    }

    public void SetBounds(Bounds rootLocalBounds)
    {
        Initialize();
        if (mainCollider == null || _flightMode) return;
        var scale = transform.lossyScale;
        if (Mathf.Abs(scale.x * scale.y * scale.z) < 0.000001f) return;
        var converted = new Bounds();
        for (int i = 0; i < 8; i++)
        {
            var corner = rootLocalBounds.center + Vector3.Scale(rootLocalBounds.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            var point = mainCollider.transform.InverseTransformPoint(transform.TransformPoint(corner));
            if (i == 0) converted = new Bounds(point, Vector3.zero);
            else converted.Encapsulate(point);
        }
        mainCollider.center = converted.center;
        mainCollider.size = Vector3.Max(converted.size, Vector3.one * 0.01f);
    }

    public void PrepareForFlight()
    {
        Initialize();
        _flightMode = true;
        if (mainCollider == null) return;
        mainCollider.center = _originalCenter;
        mainCollider.size = _originalSize;
        mainCollider.enabled = true;
    }
}
