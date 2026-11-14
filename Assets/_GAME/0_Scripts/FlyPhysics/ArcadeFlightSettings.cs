using UnityEngine;

[CreateAssetMenu(fileName = "ArcadeFlightSettings", menuName = "BAS VR/Arcade Flight Settings")]
public sealed class ArcadeFlightSettings : ScriptableObject
{
    [SerializeField, Min(0f), Tooltip("Maximum thrust acceleration above gravity, in m/s².")]
    private float verticalAcceleration = 6f;
    [SerializeField, Min(0f), Tooltip("Yaw speed in degrees per second.")]
    private float yawSpeed = 90f;
    [SerializeField, Min(0f), Tooltip("Pitch and roll change in degrees per second.")]
    private float tiltSpeed = 60f;
    [SerializeField, Range(0.1f, 89f), Tooltip("Maximum pitch and roll in degrees. Attitude is held when the stick is released.")]
    private float maxTilt = 35f;
    [SerializeField, Min(0f), Tooltip("Rigidbody linear damping. Higher values brake movement more strongly.")]
    private float drag = 0.8f;

    public float VerticalAcceleration => Mathf.Max(0f, verticalAcceleration);
    public float YawSpeed => Mathf.Max(0f, yawSpeed);
    public float TiltSpeed => Mathf.Max(0f, tiltSpeed);
    public float MaxTilt => Mathf.Clamp(maxTilt, 0.1f, 89f);
    public float Drag => Mathf.Max(0f, drag);
}
