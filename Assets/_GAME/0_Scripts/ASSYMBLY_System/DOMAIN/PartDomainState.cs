
using UnityEngine;

public class PartDomainState 
{

    public bool isLoaded = false;
    public string InstanceId { get; }

    public string DroneId { get; set; }

    public string PartId { get; }



    public PartLifecycleState LifecycleState { get; private set; }
    public string AttachedMountId { get; private set; }
    public bool IsAttachedToZone { get; private set; }
    public string AttachedSocketId => IsAttachedToZone ? null : AttachedMountId;

    public string? AttachedPartInstanceId { get; private set; }

    public PartVisualProperties VisualProperties { get; private set; }
    public PartType Type { get; private set; }

    public PartDomainState(string instanceId, string partId, PartType type, PartVisualProperties visual)
    {
        InstanceId = instanceId;
        PartId = partId;
        Type = type;
        LifecycleState = PartLifecycleState.Free;
        ///SetVisual(new PartVisualProperties() {Color = Color.gray , Smoothness = 0 });
        SetVisual(visual);
    }

    public void SetVisual(PartVisualProperties visual)
    {
        VisualProperties = visual;
    }

    public void AttachToPartSocket(
    string attachedPartInstanceId,
    string socketId)
    {
        AttachToMount(attachedPartInstanceId, socketId, false);
    }

    internal void AttachToMount(string parentId, string mountId, bool isZone)
    {
        LifecycleState = PartLifecycleState.Installed;
        AttachedPartInstanceId = parentId;
        AttachedMountId = mountId;
        IsAttachedToZone = isZone;
    }

    public void Detach()
    {
        LifecycleState = PartLifecycleState.Free;

        AttachedPartInstanceId = null;
        AttachedMountId = null;
        IsAttachedToZone = false;
    }
}
