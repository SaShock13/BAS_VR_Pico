using System;
using UnityEngine;

public sealed class PlaySFXRequest : IAppEvent
{
    public string EventId => nameof(PlaySFXRequest);
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public SFXId SFXId { get; set; }
    public Vector3 Position { get; set; }
    public float Volume { get; set; } = 1f;
    public Transform FollowTarget { get; set; }
}

