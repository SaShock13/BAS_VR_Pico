using System.Threading.Tasks;
using UnityEngine;

public class FlightState : IAppState
{
    public AppState StateId { get;} = AppState.Flight;

    public Task EnterAsync(StateTransitionContext context)
    {

        Debug.Log($"EnterAsync FlightState{this}");
        return Task.CompletedTask;
    }

    public Task ExitAsync()
    {

        Debug.Log($"ExitAsync FlightState");
        return Task.CompletedTask;
        
    }

    public void StateInitialize()
    {

        Debug.Log($"StateInitialize {this}");
    }
}

public class ApplicationPauseState : IAppState
{
    public AppState StateId { get;} = AppState.Paused;

    public Task EnterAsync(StateTransitionContext context)
    {

        Debug.Log($"EnterAsync ApplicationPauseState{this}");
        return Task.CompletedTask;
    }

    public Task ExitAsync()
    {

        Debug.Log($"ExitAsync ApplicationPauseState");
        return Task.CompletedTask;
        
    }

    public void StateInitialize()
    {

        Debug.Log($"StateInitialize {this}");
    }
}
public class ErrorState : IAppState
{
    public AppState StateId { get;} = AppState.Error;

    public Task EnterAsync(StateTransitionContext context)
    {

        Debug.Log($"EnterAsync ErrorState{this}");
        return Task.CompletedTask;
    }

    public Task ExitAsync()
    {

        Debug.Log($"ExitAsync ErrorState");
        return Task.CompletedTask;
        
    }

    public void StateInitialize()
    {

        Debug.Log($"StateInitialize {this}");
    }
}

