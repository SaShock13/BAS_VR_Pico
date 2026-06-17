using System.Collections;
using UnityEngine;

public class DroneFocusEffect : MonoBehaviour, IPreflightEffect
{
    [SerializeField]
    private Transform _preflightAnchor;

    [SerializeField]
    private float _scaleFactor = 0.2f;

    [SerializeField]
    private float _moveDuration = 0.5f;

    [SerializeField]
    private float _rotationSpeed = 15f;

    private Transform _drone;

    private Vector3 _startPos;
    private Vector3 _startScale;
    private Quaternion _startRot;

    private Coroutine _moveCoroutine;
    private Coroutine _rotateCoroutine;

    public void Initialize(Transform drone)
    {
        _drone = drone;
    }

    public void Enter()
    {

        Debug.Log($"Enter {this}");
        _startPos = _drone.position;
        _startRot = _drone.rotation;
        _startScale = _drone.localScale;

        StopRotation();

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        _moveCoroutine = StartCoroutine(
            Move(
                _preflightAnchor.position,
                _preflightAnchor.rotation,
                Vector3.one * _scaleFactor,
                startRotateAfterMove: true
            )
        );
    }

    public void Exit()
    {
        StopRotation();

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        _moveCoroutine = StartCoroutine(
            Move(
                _startPos,
                _startRot,
                _startScale,
                startRotateAfterMove: false
            )
        );
    }

    private IEnumerator Move(
        Vector3 targetPos,
        Quaternion targetRot,
        Vector3 targetScale,
        bool startRotateAfterMove)
    {
        float time = 0;

        Vector3 startPos = _drone.position;
        Quaternion startRot = _drone.rotation;
        Vector3 startScale = _drone.localScale;

        while (time < _moveDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.SmoothStep(
                0,
                1,
                time / _moveDuration
            );

            _drone.position =
                Vector3.Lerp(startPos, targetPos, t);

            _drone.rotation =
                Quaternion.Slerp(startRot, targetRot, t);

            _drone.localScale =
                Vector3.Lerp(startScale, targetScale, t);

            yield return null;
        }

        _drone.position = targetPos;
        _drone.rotation = targetRot;
        _drone.localScale = targetScale;

        _moveCoroutine = null;

        if (startRotateAfterMove)
        {
            _rotateCoroutine =
                StartCoroutine(Rotate());
        }
    }

    private IEnumerator Rotate()
    {
        while (true)
        {
            _drone.Rotate(
                Vector3.up,
                _rotationSpeed * Time.deltaTime,
                Space.World
            );

            yield return null;
        }
    }

    private void StopRotation()
    {
        if (_rotateCoroutine == null)
            return;

        StopCoroutine(_rotateCoroutine);

        _rotateCoroutine = null;
    }
}