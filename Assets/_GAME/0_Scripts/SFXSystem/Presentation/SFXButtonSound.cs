using UnityEngine;
using UnityEngine.UI;
using Zenject;

// Uses Button.onClick, including XR UI activation and keyboard/controller Submit.
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class SFXButtonSound : MonoBehaviour
{
    [Inject] private IEventBus _eventBus;
    // First persistent onClick listener, before actions that can unload the scene.
    public void Play()
    {
        _eventBus.Publish(new PlaySFXRequest { SFXId = SFXId.UIClick });
    }
}
