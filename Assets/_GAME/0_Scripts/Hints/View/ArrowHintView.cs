using UnityEngine;

public sealed class ArrowHintView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField]
    private Transform _arrowVisual;

    [Header("Settings")]
    [SerializeField]
    private float _heightOffset = 0.25f;

    [SerializeField]
    private float _bobAmplitude = 0.05f;

    [SerializeField]
    private float _bobSpeed = 2f;

    [SerializeField]
    private bool _faceDown = true;

    private Transform _target;

    private Vector3 _baseOffset;

    public void Show(HintInfo hint)
    {
        gameObject.SetActive(true);

        _target = hint.SoketTransform;


        Debug.Log($"ArrowHintView _target {_target.transform.position}");

        _baseOffset = Vector3.up * _heightOffset;
    }

    private void LateUpdate()
    {
        if (_target == null)
            return;

        // позиция над объектом + "дыхание"
        float bob = Mathf.Sin(Time.time * _bobSpeed) * _bobAmplitude;

        Vector3 worldOffset = _baseOffset + Vector3.up * bob;

        transform.position = _target.position + worldOffset;

        // ориентация
        if (_faceDown)
        {
            // стрелка должна смотреть вниз
            transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        }
    }

    public void Hide()
    {
        _target = null;
        gameObject.SetActive(false);
    }
}