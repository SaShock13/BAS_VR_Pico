using System;
using System.Collections.Generic;

public class AssemblyProjectRequest : IAppEvent
{
    public string EventId => nameof(AssemblyProjectRequest);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Operation;
    public string Id;
    public string Name;
}

public class AssemblyProjectListEvent : IAppEvent
{
    public string EventId => nameof(AssemblyProjectListEvent);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public List<AssemblyProjectData> Projects;
    public string Error;
}
