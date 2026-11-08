
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartSpawnAnimator : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float _duration = 0.4f;
    [SerializeField, Min(0f)] private float _heightMultiplier = 1f;
    [Tooltip("World-space height for requests without a menu cell.")]
    [SerializeField, Min(0f)] private float _fallbackFlyHeight = 0.1f;

    [SerializeField, Range(0f, 1f)]
    private float _initialScaleMultiplier = 0.05f;

    [Header("Ease Out Back")]
    [SerializeField] private float _overshoot = 1.70158f;

    private Coroutine _animationCoroutine;
    private Transform _animatedPart;
    private Vector3 _targetScale;
    private Vector3 _targetPosition;
    private static readonly Dictionary<Transform, Vector3> TargetScales = new();

    public static Vector3 GetStableLocalScale(Transform part) =>
        TargetScales.TryGetValue(part, out var scale) ? scale : part.localScale;

    public static Vector3 GetStableWorldScale(Transform part)
    {
        if (!TargetScales.TryGetValue(part, out var scale)) return part.lossyScale;
        return part.parent != null ? Vector3.Scale(scale, part.parent.lossyScale) : scale;
    }

    public void Play(
    Transform part,
    Vector3 spawnPosition,
    Action onCompleted = null,
    float? menuCellHeight = null)
    {
        if (part == null)
        {
            Debug.LogWarning("PartSpawnAnimator: Transform детали не задан.");
            return;
        }

        FinishCurrentAnimation();
        _animatedPart = part;
        _targetScale = part.localScale;
        _targetPosition = spawnPosition + Vector3.up * CalculateFlyHeight(menuCellHeight);
        TargetScales[part] = _targetScale;

        _animationCoroutine = StartCoroutine(
            AnimateSpawn(part, spawnPosition, _targetPosition, onCompleted));
    }

    private float CalculateFlyHeight(float? menuCellHeight)
    {
        return menuCellHeight.HasValue ? Mathf.Max(0f, menuCellHeight.Value) * _heightMultiplier : _fallbackFlyHeight;
    }

    private IEnumerator AnimateSpawn(
        Transform part,
        Vector3 spawnPosition,
        Vector3 targetPosition,
        Action onCompleted)
    {
        Vector3 targetScale = part.localScale;

        Vector3 startPosition = spawnPosition;

        part.position = startPosition;
        part.localScale = targetScale * _initialScaleMultiplier;

        float elapsed = 0f;

        while (elapsed < _duration)
        {
            if (part == null)
            {
                TargetScales.Remove(part);
                _animatedPart = null;
                _animationCoroutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / _duration);
            float easedT = EaseOutBack(t, _overshoot);

            part.position = Vector3.LerpUnclamped(
                startPosition, targetPosition, Mathf.Clamp01(easedT));

            part.localScale = Vector3.LerpUnclamped(
                targetScale * _initialScaleMultiplier,
                targetScale,
                easedT);

            yield return null;
        }

        if (part != null)
        {
            part.position = targetPosition;
            part.localScale = targetScale;
        }

        _animationCoroutine = null;
        TargetScales.Remove(part);
        _animatedPart = null;
        onCompleted?.Invoke();
    }

    private static float EaseOutBack(float t, float overshoot)
    {
        float x = t - 1f;

        return 1f + (overshoot + 1f) * x * x * x
                   + overshoot * x * x;
    }

    private void OnDisable()
    {
        FinishCurrentAnimation();
    }

    private void FinishCurrentAnimation()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
        if (!ReferenceEquals(_animatedPart, null))
        {
            if (_animatedPart != null)
            {
                _animatedPart.localScale = _targetScale;
                _animatedPart.position = _targetPosition;
            }
            TargetScales.Remove(_animatedPart);
            _animatedPart = null;
        }
    }
}
