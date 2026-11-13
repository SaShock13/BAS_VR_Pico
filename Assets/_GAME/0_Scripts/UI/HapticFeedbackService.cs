using System;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.XR.PXR;
using VibrateType = Unity.XR.PXR.PXR_Input.VibrateType;
#else
using UnityEngine.XR;
#endif
using Zenject;

public sealed class HapticFeedbackService : IInitializable, IDisposable
{
    private readonly IEventBus _eventBus;
    private IDisposable _subscription;

    public HapticFeedbackService(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void Initialize()
    {
        _subscription = _eventBus.Subscribe<HapticFeedbackRequestedEvent>(OnHapticFeedbackRequested);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }

    private static void OnHapticFeedbackRequested(HapticFeedbackRequestedEvent request)
    {
        if (!float.IsFinite(request.Amplitude) || !float.IsFinite(request.Duration) ||
            request.Amplitude <= 0f || request.Duration <= 0f)
            return;

        var amplitude = Mathf.Clamp01(request.Amplitude);
#if UNITY_ANDROID && !UNITY_EDITOR
        var controller = request.Hand switch
        {
            ControllerHand.Left => VibrateType.LeftController,
            ControllerHand.Right => VibrateType.RightController,
            ControllerHand.Both => VibrateType.BothController,
            _ => VibrateType.None
        };
        var durationMs = Mathf.RoundToInt(Mathf.Clamp(request.Duration, 0.001f, 65.535f) * 1000f);
        PXR_Input.SendHapticImpulse(controller, amplitude, durationMs);
#else
        if (request.Hand is ControllerHand.Left or ControllerHand.Both)
            SendImpulse(XRNode.LeftHand, amplitude, request.Duration);

        if (request.Hand is ControllerHand.Right or ControllerHand.Both)
            SendImpulse(XRNode.RightHand, amplitude, request.Duration);
#endif
    }

#if !UNITY_ANDROID || UNITY_EDITOR
    private static void SendImpulse(XRNode node, float amplitude, float duration)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (device.isValid && device.TryGetHapticCapabilities(out var capabilities) &&
            capabilities.supportsImpulse && capabilities.numChannels > 0)
            device.SendHapticImpulse(0, amplitude, duration);
    }
#endif
}
