using UnityEngine;

public abstract class MountPoint : MonoBehaviour
{
    public abstract string MountId { get; }
    public abstract PartType[] AcceptedTypes { get; }
    public abstract DronePartView Owner { get; }
}
