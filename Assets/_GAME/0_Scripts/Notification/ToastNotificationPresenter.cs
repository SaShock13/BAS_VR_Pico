using System.Collections;
using System.Collections.Generic;
using Pico.Platform;
using UnityEngine;
using Zenject;

public sealed class ToastNotificationPresenter : MonoBehaviour
{
    [SerializeField]
    private ToastNotificationView _view;

    [SerializeField]
    private float _fadeDuration = 0.25f;

    private readonly Queue<ToastNotification> _queue = new();

    private ToastNotification? _lastMessage;

    [Inject] private INotificationService _service;

    private bool _isShowing;

    private bool repeatAllowed = true; // можно ли повторять одно и тоже сообщение подряд

    public void Start()
    {
        _service.ToastRequested += OnNotificationRequested; // todo ВКЛЮЧИТЬ И РАЗОбРАТЬСЯ
    }

    private void OnNotificationRequested(
        ToastNotification message)
    {
        if (!repeatAllowed &&_lastMessage.HasValue &&
        _lastMessage.Value.Text == message.Text)
        {
            return;
        }

        _lastMessage = message;

        _queue.Enqueue(message);

        if (!_isShowing)
            StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        _isShowing = true;

        while (_queue.Count > 0)
        {
            ToastNotification message =
                _queue.Dequeue();

            yield return ShowMessage(message);
        }

        _isShowing = false;
    }

    private IEnumerator ShowMessage(
        ToastNotification message)
    {
        _view.gameObject.SetActive(true);
        _view.SetMessage(message);

        // fade in

        float t = 0;

        while (t < _fadeDuration)
        {
            t += Time.deltaTime;

            _view.SetAlpha(
                Mathf.Clamp01(t / _fadeDuration));

            yield return null;
        }

        _view.SetAlpha(1);

        yield return new WaitForSeconds(
            message.Duration);

        // fade out

        t = 0;

        while (t < _fadeDuration)
        {
            t += Time.deltaTime;

            _view.SetAlpha(
                1f - Mathf.Clamp01(
                    t / _fadeDuration));

            yield return null;
        }

        _view.SetAlpha(0);
        _view.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_service != null)
            _service.ToastRequested -=
                OnNotificationRequested;
    }
}