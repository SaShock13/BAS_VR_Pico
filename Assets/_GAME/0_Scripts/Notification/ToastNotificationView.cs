using TMPro;
using UnityEngine;

public sealed class ToastNotificationView : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private CanvasGroup _canvasGroup;

    public void SetMessage(ToastNotification message)
    {
        _text.text = message.Text;
    }

    public void SetAlpha(float value)
    {
        _canvasGroup.alpha = value;
    }
}