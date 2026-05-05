
public class PartDomainState 
{
    public string InstanceId { get; }
    public string PartId { get; }

    public PartLifecycleState LifecycleState { get; private set; }
    public string AttachedSocketId { get; private set; }

    public PartVisualProperties? VisualProperties { get; private set; }

    public PartDomainState(string instanceId, string partId)
    {
        InstanceId = instanceId;
        PartId = partId;
        LifecycleState = PartLifecycleState.Free;
    }

    public void SetVisual(PartVisualProperties? visual)
    {
        VisualProperties = visual;
    }

    public void AttachToSocket(string socketId)
    {
        LifecycleState = PartLifecycleState.Installed;
        AttachedSocketId = socketId;
    }

    public void Detach()
    {
        LifecycleState = PartLifecycleState.Free;
        AttachedSocketId = null;
    }
}
