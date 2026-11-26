using System;

[Serializable]
public class PartSaveData
{
    public string InstanceId;
    public string PartId;
    public PartType Type;
    public PartLifecycleState LifecycleState;
    public string AttachedSocketId;
    public string AttachedZoneId;
    public string AttachedPartId;
    public PartVisualProperties VisualProperties;
    public bool HasArmCoverOffset;
    public float ArmCoverOffset;

    public TransformSaveData Transform;
}
