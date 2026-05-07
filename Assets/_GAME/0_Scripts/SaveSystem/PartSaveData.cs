using System;

[Serializable]
public class PartSaveData
{
    public string InstanceId;
    public string PartId;
    public PartLifecycleState LifecycleState;
    public string AttachedSocketId;
    public string AttachedPartId;
    public PartVisualProperties VisualProperties;

    public TransformSaveData Transform;
}