using UnityEngine;
using Zenject;

public class PartTransformAdjustmentService /// Подумать, возможно сделать округление до шага
{
    [Inject] private readonly PartViewRegistry _viewRegistry;
    [Inject] private readonly IEventBus _eventBus;


    public void Move(
        string instanceId,
        Vector3 localDelta)
    {
        if (!_viewRegistry.TryGet(instanceId, out var view))
            return;

        view.transform.localPosition += localDelta;
        _eventBus.Publish(new AssemblyGeometryChangedEvent());
    }

    public void Rotate(
        string instanceId,
        Vector3 eulerDelta)
    {
        if (!_viewRegistry.TryGet(instanceId, out var view))
            return;

        view.transform.localRotation *=
            Quaternion.Euler(eulerDelta);
        _eventBus.Publish(new AssemblyGeometryChangedEvent());
    }

    public void ResetRotation(string instanceId)
    {
        if (!_viewRegistry.TryGet(instanceId, out var view))
            return;

        view.transform.localRotation = Quaternion.identity;
        _eventBus.Publish(new AssemblyGeometryChangedEvent());
    }

    public void ResetPosition(string instanceId)
    {
        if (!_viewRegistry.TryGet(instanceId, out var view))
            return;

        view.transform.localPosition = Vector3.zero;
        _eventBus.Publish(new AssemblyGeometryChangedEvent());
    }
}
