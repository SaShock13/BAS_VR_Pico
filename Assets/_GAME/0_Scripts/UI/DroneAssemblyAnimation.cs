using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Keeps the serialized layout of the Drones 1.0 menu animation.
public class DroneAssemblyAnimation : MonoBehaviour
{
    [Serializable]
    public class PartInfo
    {
        public Transform part;
        public Vector3 originalPosition;
        public Quaternion originalRotation;
    }

    public Transform rootObject;
    public List<PartInfo> parts = new List<PartInfo>();
    public float moveSpeed = 2f;
    public float rotationSpeed = 180f;
    public float delayBetweenParts = 0.3f;
    public float delayBetweenAssembles = 1f;
    [SerializeField] private bool isAutoStart = true;
    private Transform[] _targets;
    private Vector3[] _moveStarts;
    private Quaternion[] _rotationStarts;
    private Coroutine _animation;
    private bool _initialized;

    private void Start()
    {
        Initialize();
        if (isAutoStart) StartEndlessAnimation();
    }

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        int count = rootObject == null ? 0 : Mathf.Min(parts.Count, rootObject.childCount);
        _targets = new Transform[count];
        _moveStarts = new Vector3[count];
        _rotationStarts = new Quaternion[count];
        for (int i = 0; i < count; i++)
        {
            _targets[i] = rootObject.GetChild(i);
            if (parts[i].part == null) continue;
            parts[i].originalPosition = parts[i].part.position;
            parts[i].originalRotation = parts[i].part.rotation;
        }
    }

    public void StartEndlessAnimation()
    {
        Initialize();
        if (_animation == null && _targets.Length > 0)
            _animation = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        var betweenCycles = new WaitForSeconds(Mathf.Max(0f, delayBetweenAssembles));
        while (true)
        {
            yield return MoveParts(true);
            yield return betweenCycles;
            yield return MoveParts(false);
            yield return betweenCycles;
        }
    }

    private IEnumerator MoveParts(bool assembling)
    {
        for (int i = 0; i < _targets.Length; i++)
        {
            if (parts[i].part == null) continue;
            _moveStarts[i] = parts[i].part.position;
            _rotationStarts[i] = parts[i].part.rotation;
        }
        float elapsed = 0f;
        bool complete = false;
        while (!complete)
        {
            complete = true;
            for (int i = 0; i < _targets.Length; i++)
            {
                var part = parts[i].part;
                if (part == null || _targets[i] == null) continue;
                int order = assembling ? i : _targets.Length - 1 - i;
                float time = elapsed - order * Mathf.Max(0f, delayBetweenParts);
                if (time < 0f) { complete = false; continue; }
                var position = assembling ? _targets[i].position : parts[i].originalPosition;
                var rotation = assembling ? _targets[i].rotation : parts[i].originalRotation;
                var nextPosition = Vector3.MoveTowards(_moveStarts[i], position, Mathf.Max(0.01f, moveSpeed) * time);
                var nextRotation = Quaternion.RotateTowards(_rotationStarts[i], rotation, Mathf.Max(1f, rotationSpeed) * time);
                part.SetPositionAndRotation(nextPosition, nextRotation);
                if ((nextPosition - position).sqrMagnitude > 0.000001f || Quaternion.Angle(nextRotation, rotation) > 0.1f)
                    complete = false;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public void StopAnimation()
    {
        if (_animation != null) StopCoroutine(_animation);
        _animation = null;
    }

    private void OnDisable() => StopAnimation();

    private void OnEnable()
    {
        if (_initialized && isAutoStart) StartEndlessAnimation();
    }
}
