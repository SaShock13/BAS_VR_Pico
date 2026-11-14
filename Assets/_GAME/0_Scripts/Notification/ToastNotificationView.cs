using TMPro;
using UnityEngine;

public sealed class ToastNotificationView : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Camera _viewer;
    [SerializeField] private Vector3 _headOffset = new(0f, -0.25f, 1.5f);
    private Canvas _canvas;

    public void SetViewer(Camera viewer)
    {
        _viewer = viewer;
        AttachToViewer();
    }

    public bool AttachToViewer()
    {
        if (_canvas == null) _canvas = GetComponent<Canvas>();
        if (_viewer == null) _viewer = _canvas.worldCamera;
        if (_viewer == null) _viewer = Camera.main;
        if (_viewer == null) return false;

        // Parenting follows XR head tracking without a per-frame camera lookup or follower.
        if (transform.parent != _viewer.transform) transform.SetParent(_viewer.transform, false);
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = _viewer;
        transform.localPosition = _headOffset;
        transform.localRotation = Quaternion.identity;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        return true;
    }

    public void SetMessage(ToastNotification message)
    {
        _text.text = message.Text;
    }

    public void SetAlpha(float value)
    {
        _canvasGroup.alpha = value;
    }
}
