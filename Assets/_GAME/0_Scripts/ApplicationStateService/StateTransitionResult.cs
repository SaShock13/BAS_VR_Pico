using System;
using UnityEngine;

public enum Result
{
    Undefined,
    Failed,
    Succeed
}

public class StateTransitionResult
{
    public static Result _result = Result.Undefined;
    public static string failedMessage = string.Empty;


    public static StateTransitionResult Failed(string message)
    {
        failedMessage = message;
        _result = Result.Failed;
        return new StateTransitionResult(_result = Result.Failed);
    } 
    public static StateTransitionResult Succeed ()
    {
        failedMessage = string.Empty;
        _result = Result.Succeed;
        return new StateTransitionResult (_result = Result.Succeed);
    }

    public bool Success()
    {
        return _result == Result.Succeed;
    }

    public string ErrorMessage()
    {
        return failedMessage;
    }

    public StateTransitionResult(Result result)
    {
        _result = result;
    }
}
