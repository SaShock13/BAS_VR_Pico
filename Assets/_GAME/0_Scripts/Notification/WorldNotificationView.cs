using TMPro;
using UnityEngine;
using System.Collections;

public sealed class WorldNotificationView
    : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _text;

    [SerializeField]
    private CanvasGroup _canvasGroup;

    [SerializeField]
    private Vector3 _offset =
        new(0f, 0.25f, 0f);

    private Transform _anchor;

    public void Show(
        WorldNotification notification)
    {
        _anchor = notification.Anchor;

        _text.text = notification.Text;

        StartCoroutine(
            LifeRoutine(notification.Duration));
    }

    private void LateUpdate()
    {
        if (_anchor == null)
            return;

        transform.position =
            _anchor.position + _offset;

        Camera cam = Camera.main;

        if (cam != null)
        {
            transform.forward =
                cam.transform.forward;
        }
    }

    private IEnumerator LifeRoutine(
        float duration)
    {
        _canvasGroup.alpha = 1;

        yield return new WaitForSeconds(duration);

        Destroy(gameObject);
    }
}