using System;
using UnityEngine;

public class Clean_CreatePartRequestEvent : IAppEvent
{


    public string PartId;

    public string EventId { get; set; } = "Clean_CreatePartRequestEvent";

    public Vector3 WorldPosition;
    // Omitted by older callers: retain the prefab's authored scale.
    public Vector3? WorldScale;
    public float? MenuCellHeight;

    public DateTime Timestamp { get; set; }
}
