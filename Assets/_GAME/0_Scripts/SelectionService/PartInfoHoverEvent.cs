using System;
using UnityEngine;

public sealed class PartInfoHoverEvent : IAppEvent
{
    public string EventId => nameof(PartInfoHoverEvent);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public object Source;
    public bool IsHovered;
    public string ConfigId;
    public Transform PreviewAnchor;
}
