using System;
using UnityEngine;

public class Clean_CreatePartRequestEvent : IAppEvent
{


    public string PartId;

    public string EventId { get; set; } = "Clean_CreatePartRequestEvent";

    public DateTime Timestamp { get; set; }
}
