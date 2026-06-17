using System.Collections;
using UnityEngine;

public class BackgroundFadeEffect :
    MonoBehaviour,
    IPreflightEffect
{
    [SerializeField]
    private MeshRenderer _renderer;
    private Collider _collider;

    [SerializeField]
    private float _targetAlpha = 0.75f;

    [SerializeField]
    private float _duration = 0.5f;

    private Material _material;
    private Coroutine _coroutine;

    private void Awake()
    {
        _material = _renderer.material;
        _collider = _renderer.GetComponent<Collider>();
    }

    public void Enter()
    {
        _collider.enabled = true;
        Debug.Log($"Enter {this}");
        StartFade(_targetAlpha);
    }

    public void Exit()
    {
        _collider.enabled = false;
        StartFade(0);
    }

    private void StartFade(float target)
    {
        if (_coroutine != null)
            StopCoroutine(_coroutine);

        _coroutine = StartCoroutine(Fade(target));
    }

    private IEnumerator Fade(float target)
    {
        float start = _material.color.a;

        float time = 0;

        while (time < _duration)
        {
            time += Time.deltaTime;

            float t = time / _duration;

            Color color = _material.color;

            color.a = Mathf.Lerp(
                start,
                target,
                t
            );

            _material.color = color;

            yield return null;
        }

        Color final = _material.color;

        final.a = target;

        _material.color = final;

        _coroutine = null;
    }
}