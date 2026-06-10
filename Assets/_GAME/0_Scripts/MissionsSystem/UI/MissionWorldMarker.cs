using TMPro;
using UnityEngine;

public class MissionWorldMarker : MonoBehaviour
{
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private GameObject markerRoot;

    [SerializeField] private float offsetY = 1f;
    [SerializeField] private float floatAmplitude = 0.5f;
    [SerializeField] private float floatSpeed = 2f;

    private Transform _target;
    private Transform _player;

    public void Initialize(Transform player)
    {
        _player = player;
    }

    private void OnEnable()
    {
        MissionEvents.TargetChanged += OnTargetChanged;
    }

    private void OnDisable()
    {
        MissionEvents.TargetChanged -= OnTargetChanged;
    }

    private void Update()
    {
        if (_target == null)
            return;

        float animatedOffset =
        offsetY +
        Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;

        markerRoot.transform.position =
            _target.position + Vector3.up * animatedOffset;

        float distance =
            Vector3.Distance(
                _player.position,
                _target.position);

        distanceText.text =
            $"{distance:F0}m";
    }

    private void OnTargetChanged(
        Transform target)
    {
        _target = target;

        markerRoot.SetActive(
            target != null);
    }
}