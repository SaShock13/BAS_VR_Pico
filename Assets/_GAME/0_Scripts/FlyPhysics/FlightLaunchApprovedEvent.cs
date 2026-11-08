using System;

// Published only after selection, validation and the flight snapshot have succeeded.
public sealed class FlightLaunchApprovedEvent : IAppEvent
{
    public string EventId => nameof(FlightLaunchApprovedEvent);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsDeferred { get; private set; }
    private bool _finished;
    private readonly Action _continue;
    private readonly Action _cancel;

    public FlightLaunchApprovedEvent(Action continueLaunch, Action cancelLaunch)
    {
        _continue = continueLaunch;
        _cancel = cancelLaunch;
    }

    public bool TryDefer()
    {
        if (IsDeferred || _finished)
            return false;
        IsDeferred = true;
        return true;
    }

    public void Continue()
    {
        if (!IsDeferred || _finished)
            return;
        _finished = true;
        _continue?.Invoke();
    }

    public void Cancel()
    {
        if (!IsDeferred || _finished)
            return;
        _finished = true;
        _cancel?.Invoke();
    }
}
