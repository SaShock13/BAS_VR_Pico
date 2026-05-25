
using UnityEngine;

public class PartDomainState 
{

    public bool isLoaded = false;
    public string InstanceId { get; }

    public string DroneId { get; set; }

    public string PartId { get; }



    public PartLifecycleState LifecycleState { get; private set; }
    public string AttachedSocketId { get; private set; }

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
        LifecycleState = PartLifecycleState.Installed;

        AttachedPartInstanceId = attachedPartInstanceId;
        AttachedSocketId = socketId;


        Debug.Log($"77777Деталь {InstanceId}  присоединенна к детали {AttachedPartInstanceId} к сокету {AttachedSocketId}");
    }

    public void Detach()
    {
        LifecycleState = PartLifecycleState.Free;

        AttachedPartInstanceId = null;
        AttachedSocketId = null;
    }
}
