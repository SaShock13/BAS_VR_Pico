using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public sealed class ToastNotificationPresenter : MonoBehaviour
{
    [SerializeField] private ToastNotificationView _view;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.25f;
    private readonly Queue<ToastNotification> _queue = new();
    private INotificationService _service;
    private bool _subscribed;
    private bool _isShowing;

    [Inject]
    public void Construct(INotificationService service)
    {
        Unsubscribe();
        _service = service;
        _view.SetAlpha(0f);
        _view.gameObject.SetActive(false);
        if (isActiveAndEnabled) Subscribe();
    }

    private void OnEnable() => Subscribe();

    private void Subscribe()
    {
        if (_service == null || _subscribed) return;
        _service.ToastRequested += OnNotificationRequested;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;
        _service.ToastRequested -= OnNotificationRequested;
        _subscribed = false;
    }

    private void OnNotificationRequested(ToastNotification message)
    {
        _queue.Enqueue(message);
        if (!_isShowing) StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        _isShowing = true;
        while (_queue.Count > 0)
            yield return ShowMessage(_queue.Dequeue());
        _isShowing = false;
    }

    private IEnumerator ShowMessage(ToastNotification message)
    {
        if (!_view.AttachToViewer())
        {
            Debug.LogWarning("Toast notification has no player camera assigned.", this);
            yield break;
        }
        _view.SetAlpha(0f);
        _view.SetMessage(message);
        _view.gameObject.SetActive(true);
        yield return Fade(0f, 1f);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, message.Duration));
        yield return Fade(1f, 0f);
        _view.gameObject.SetActive(false);
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _view.SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / _fadeDuration)));
            yield return null;
        }
        _view.SetAlpha(to);
    }

    private void OnDisable()
    {
        Unsubscribe();
        StopAllCoroutines();
        _queue.Clear();
        _isShowing = false;
        if (_view != null)
        {
            _view.SetAlpha(0f);
            _view.gameObject.SetActive(false);
        }
    }

    private void OnDestroy() => Unsubscribe();
}
