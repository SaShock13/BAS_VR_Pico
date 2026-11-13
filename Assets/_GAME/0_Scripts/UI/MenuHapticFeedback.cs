using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Zenject;

public sealed class MenuHapticFeedback : IInitializable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly Scene _scene;
    private readonly List<XRUIInputModule> _modules = new();

    public MenuHapticFeedback(IEventBus eventBus, Scene scene)
    {
        _eventBus = eventBus;
        _scene = scene;
    }

    public void Initialize()
    {
        foreach (var root in _scene.GetRootGameObjects())
        {
            foreach (var module in root.GetComponentsInChildren<XRUIInputModule>(true))
            {
                module.pointerClick += OnPointerClick;
                _modules.Add(module);
            }
        }
    }

    public void Dispose()
    {
        foreach (var module in _modules)
            module.pointerClick -= OnPointerClick;

        _modules.Clear();
    }

    private void OnPointerClick(GameObject target, PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left ||
            data is not TrackedDeviceEventData { interactor: XRBaseInteractor interactor })
            return;

        if (!target || !target.TryGetComponent<Selectable>(out var control) ||
            control is not IPointerClickHandler || !control.isActiveAndEnabled || !control.IsInteractable())
            return;

        ControllerHand hand;
        switch (interactor.handedness)
        {
            case InteractorHandedness.Left:
                hand = ControllerHand.Left;
                break;
            case InteractorHandedness.Right:
                hand = ControllerHand.Right;
                break;
            default:
                return;
        }

        _eventBus.Publish(new HapticFeedbackRequestedEvent
        {
            Hand = hand,
            Amplitude = 0.2f,
            Duration = 0.05f,
            Timestamp = DateTime.UtcNow
        });
    }
}
