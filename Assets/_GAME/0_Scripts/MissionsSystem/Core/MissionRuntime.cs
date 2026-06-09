using System.Collections.Generic;
using UnityEngine;

public class MissionRuntime
{
    private readonly List<MissionStep> _steps;
    private readonly List<MissionCondition> _failConditions;

    private MissionContext _context;

    private int _currentStepIndex;

    public MissionState State { get; private set; }

    public MissionStep CurrentStep =>
        _currentStepIndex < _steps.Count
            ? _steps[_currentStepIndex]
            : null;

    public MissionRuntime(
        List<MissionStep> steps,
        List<MissionCondition> failConditions)
    {
        _steps = steps;
        _failConditions = failConditions;
    }

    public void Start(MissionContext context)
    {
        _context = context;

        foreach (var step in _steps)
            step.Initialize(context);

        foreach (var condition in _failConditions)
            condition.Initialize(context);

        State = MissionState.Running;

        MissionEvents.MissionStarted?.Invoke();

        if (_steps.Count > 0)
            _steps[0].Enter();
    }

    public void Tick()
    {
        if (State != MissionState.Running)
            return;

        foreach (var condition in _failConditions)
        {
            if (condition.IsTriggered())
            {
                Fail(condition.FailReason);
                return;
            }
        }

        var currentStep = CurrentStep;

        if (currentStep == null)
        {
            Complete();
            return;
        }

        currentStep.Tick();

        if (!currentStep.IsCompleted)
            return;

        currentStep.Exit();

        _currentStepIndex++;

        if (_currentStepIndex >= _steps.Count)
        {
            Complete();
            return;
        }

        CurrentStep.Enter();
    }

    public void Pause()
    {
        if (State != MissionState.Running)
            return;

        State = MissionState.Paused;
    }

    public void Resume()
    {
        if (State != MissionState.Paused)
            return;

        State = MissionState.Running;
    }

    private void Complete()
    {
        State = MissionState.Completed;
        Debug.Log("MISSION COMPLETED");
        MissionEvents.MissionCompleted?.Invoke();
    }

    private void Fail(string reason)
    {
        State = MissionState.Failed;
        Debug.LogError($"MISSION FAILED: {reason}");
        MissionEvents.MissionFailed?.Invoke(reason);
    }
}