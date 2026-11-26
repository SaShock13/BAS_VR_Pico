using UnityEngine;
using Zenject;

public sealed class QuitGameView : MonoBehaviour
{
    [SerializeField] private ConfirmationView confirmation;
    [SerializeField, TextArea] private string message = "Выйти из игры?";
    private IEventBus _bus;

    [Inject] public void Construct(IEventBus bus) => _bus = bus;

    public void Request()
    {
        if (isActiveAndEnabled)
            confirmation.Show(message, Confirm);
    }

    private void Confirm() => _bus.Publish(new QuitGameRequest());
}
