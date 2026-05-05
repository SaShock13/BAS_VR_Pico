using System;
using UnityEngine;

public class ConsoleLogger : IAppLogger
{
    public bool IsDebugEnabled { get; } = true;

    public void Initialize()
    {
        Log("Initializing logger");
    }

    public void Log(string message)
    {

        Debug.Log(message);
    }

    public void LogError(string message)
    {
        Debug.LogError(message);
    }

    public void LogWarning(string message)
    {
        Debug.LogWarning(message);
    }
}
