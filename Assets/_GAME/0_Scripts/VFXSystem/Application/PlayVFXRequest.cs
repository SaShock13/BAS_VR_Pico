using System;
using UnityEngine;

public sealed class PlayVFXRequest : IAppEvent
{
    public string EventId => nameof(PlayVFXRequest);
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public VFXId VFXId { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Scale { get; set; } = Vector3.one;
}
