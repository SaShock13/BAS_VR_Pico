using UnityEngine;

// The surface mesh is extracted from the button's own rigidly skinned triangles.
// This component animates only while a hint is visible; it never sends input.
[DefaultExecutionOrder(10000)]
[RequireComponent(typeof(MeshRenderer))]
public sealed class ControllerButtonPressVisual : MonoBehaviour
{
    [SerializeField, Range(0.25f, 2f)] private float pressFrequency = 1.25f;
    [SerializeField, Range(0f, 0.003f)] private float pressDepth = 0.0008f;
    [SerializeField, Range(0f, 1f)] private float whiteStrength = 0.9f;
    [SerializeField] private bool rotatePress;
    [SerializeField] private Vector3 pressEulerAngles;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private MeshRenderer _surface;
    private MaterialPropertyBlock _properties;
    private Transform _bone;
    private Vector3 _beforePress;
    private Vector3 _lastWrittenPosition;
    private Quaternion _beforeRotation;
    private Quaternion _lastWrittenRotation;
    private bool _hasWrittenPose;
    private bool _physicalPressed;
    private float _startedAt;

    private void Prepare()
    {
        if (_surface == null)
            _surface = GetComponent<MeshRenderer>();
        if (_properties == null)
            _properties = new MaterialPropertyBlock();
    }

    public void Bind(Transform bone)
    {
        Unbind();
        if (bone == null)
            return;
        Prepare();
        _bone = bone;
        _startedAt = Time.unscaledTime;
        var parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        var scale = bone.lossyScale;
        transform.localScale = new Vector3(SafeRatio(scale.x, parentScale.x),
            SafeRatio(scale.y, parentScale.y), SafeRatio(scale.z, parentScale.z));
        enabled = true;
    }

    private static float SafeRatio(float value, float divisor) =>
        Mathf.Abs(divisor) > 0.00001f ? value / divisor : 1f;

    public void SetPhysicalPressed(bool pressed)
    {
        if (_physicalPressed == pressed)
            return;
        _physicalPressed = pressed;
        _startedAt = Time.unscaledTime;
        if (pressed)
        {
            RestorePose();
            if (_surface != null)
                _surface.enabled = false;
        }
    }

    private void LateUpdate() => ApplyAnimation(Time.unscaledTime);

    internal void ApplyAnimation(float time)
    {
        RestorePose();
        if (_bone == null || !_bone.gameObject.activeInHierarchy || _physicalPressed)
        {
            if (_surface != null)
                _surface.enabled = false;
            return;
        }

        float phase = Mathf.Repeat(Mathf.Max(0f, time - _startedAt) * pressFrequency, 1f);
        float press = phase < 0.16f ? Mathf.SmoothStep(0f, 1f, phase / 0.16f)
            : phase < 0.32f ? Mathf.SmoothStep(1f, 0f, (phase - 0.16f) / 0.16f) : 0f;
        _beforePress = _bone.localPosition;
        _beforeRotation = _bone.localRotation;
        if (rotatePress)
        {
            _lastWrittenRotation = Quaternion.Euler(_beforeRotation.eulerAngles + pressEulerAngles * press);
            _bone.localRotation = _lastWrittenRotation;
        }
        else
        {
            _lastWrittenPosition = _beforePress - _bone.localRotation * Vector3.up * (pressDepth * press);
            _bone.localPosition = _lastWrittenPosition;
        }
        _hasWrittenPose = true;
        transform.SetPositionAndRotation(_bone.position, _bone.rotation);

        _surface.enabled = press > 0.001f;
        if (_surface.enabled)
        {
            _properties.SetColor(BaseColor, new Color(1f, 1f, 1f, whiteStrength * press));
            _surface.SetPropertyBlock(_properties);
        }
    }

    private void RestorePose()
    {
        // If the SDK wrote a newer pose, preserve it instead of restoring stale data.
        if (_hasWrittenPose && _bone != null)
        {
            if (rotatePress)
            {
                if (SameRotation(_bone.localRotation, _lastWrittenRotation))
                    _bone.localRotation = _beforeRotation;
            }
            else if ((_bone.localPosition - _lastWrittenPosition).sqrMagnitude < 0.000000000001f)
                _bone.localPosition = _beforePress;
        }
        _hasWrittenPose = false;
    }

    private static bool SameRotation(Quaternion a, Quaternion b)
    {
        // Avoid acos/dot rounding for a pose we just wrote. q and -q represent
        // the same rotation; Transform can return either normalized form.
        var delta = new Vector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
        var inverse = new Vector4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
        return Mathf.Min(delta.sqrMagnitude, inverse.sqrMagnitude) < 0.0000000001f;
    }

    public void Unbind()
    {
        RestorePose();
        _bone = null;
        Prepare();
        _surface.enabled = false;
        enabled = false;
    }

    private void OnDisable()
    {
        RestorePose();
        if (_surface != null)
            _surface.enabled = false;
    }
}
