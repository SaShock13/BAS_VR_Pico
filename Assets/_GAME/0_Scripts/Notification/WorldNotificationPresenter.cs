using UnityEngine;
using Zenject;

public sealed class WorldNotificationPresenter
    : MonoBehaviour
{
    [SerializeField]
    private WorldNotificationView _prefab;

    [Inject] private INotificationService _service;

    public void Start()
    {
        _service.WorldRequested += OnWorldRequested;
    }

    private void OnWorldRequested(
        WorldNotification notification)
    {
        WorldNotificationView view =
            Instantiate(
                _prefab,
                notification.Anchor);

        view.Show(notification);
    }
}