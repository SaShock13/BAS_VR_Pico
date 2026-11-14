using System.Collections.Generic;
using UnityEngine;

public class MissionRuntime
{
    private readonly List<MissionStep> _steps;
    private readonly List<MissionCondition> _failConditions;

    private MissionContext _context;

    private readonly MissionDefinition _definition;
    private readonly IEventBus _eventBus;

    private int _currentStepIndex;

    public MissionState State { get; private set; }

    public MissionStep CurrentStep =>
        _currentStepIndex < _steps.Count
            ? _steps[_currentStepIndex]
            : null;

    public MissionRuntime(
        List<MissionStep> steps,
        List<MissionCondition> failConditions,
        MissionDefinition definition,
        IEventBus eventBus = null)
    {
        _steps = steps;
        _definition = definition;
        _failConditions = failConditions;
        _eventBus = eventBus;
    }

    public void Start(MissionContext context)
    {
        _context = context;

        foreach (var step in _steps)
            step.Initialize(context);

        foreach (var condition in _failConditions)
            condition.Initialize(context);

        State = MissionState.Running;

        MissionEvents.MissionStarted?.Invoke(_definition.MissionName);
        _eventBus?.Publish(new MissionBriefingEvent(_definition.Briefing,
            _definition.BriefingNotificationDuration));


        if (_steps.Count > 0)
        {

            var info = new MissionObjectiveInfo();
            MissionEvents.ObjectiveChanged?.Invoke(
                GetObjectiveInfo());

            MissionEvents.TargetChanged?.Invoke(
                _steps[0].Target);

            EnterCurrentStep();
        }
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
        HideControllerButtonHint();

        var stepData = _currentStepIndex < _definition.Steps.Count
            ? _definition.Steps[_currentStepIndex] : null;
        _eventBus?.Publish(new MissionCompletionFeedbackEvent(
            stepData != null ? stepData.CompletionMessage : "Отлично, вы сделали это!",
            stepData == null || stepData.PlayCompletionSound
                ? (SFXId?)(stepData != null ? stepData.CompletionSound : SFXId.MissionStepComplete) : null,
            false,
            stepData != null ? stepData.CompletionNotificationDuration : 0f));

        _currentStepIndex++;

        if (_currentStepIndex >= _steps.Count)
        {
            Complete();
            return;
        }

        MissionEvents.ObjectiveChanged?.Invoke(GetObjectiveInfo());

        MissionEvents.TargetChanged?.Invoke(
             CurrentStep.Target);

        EnterCurrentStep();
    }

    private void EnterCurrentStep()
    {
        CurrentStep.Enter();
        UpdateControllerButtonHint();
    }

    private void UpdateControllerButtonHint()
    {
        var data = _currentStepIndex < _definition.Steps.Count
            ? _definition.Steps[_currentStepIndex] : null;
        var action = data != null && CurrentStep != null && !CurrentStep.IsCompleted
            && data.ControllerButtonAction != null ? data.ControllerButtonAction.action : null;
        _eventBus?.Publish(new ControllerButtonHintRequest(action));
    }

    private void HideControllerButtonHint() =>
        _eventBus?.Publish(new ControllerButtonHintRequest(null));

    public void Pause()
    {
        if (State != MissionState.Running)
            return;

        State = MissionState.Paused;
        HideControllerButtonHint();
    }

    public void Stop()
    {
        if (State == MissionState.Running || State == MissionState.Paused)
            CurrentStep?.Exit();
        CancelPendingTransition();
        HideControllerButtonHint();
        State = MissionState.Inactive;
    }

    public void Resume()
    {
        if (State != MissionState.Paused)
            return;

        State = MissionState.Running;
        UpdateControllerButtonHint();
    }

    private void Complete()
    {
        State = MissionState.Completed;
        MissionEvents.TargetChanged?.Invoke(null);
        MissionEvents.MissionCompleted?.Invoke(_definition.MissionName);
        _eventBus?.Publish(new MissionCompletionFeedbackEvent(
            _definition.CompletionMessage,
            _definition.PlayCompletionSound ? (SFXId?)_definition.CompletionSound : null,
            true,
            _definition.CompletionNotificationDuration,
            _context.CompletionNotificationDismissed,
            _context.CompletionNotificationCancelled));
    }

    private void Fail(string reason)
    {
        CurrentStep?.Exit();
        CancelPendingTransition();
        HideControllerButtonHint();
        State = MissionState.Failed;
        MissionEvents.TargetChanged?.Invoke(null);
        MissionEvents.MissionFailed?.Invoke(_definition.MissionName,reason);
    }

    private void CancelPendingTransition()
    {
        _context?.CompletionNotificationCancelled?.Invoke();
        if (_context != null)
        {
            _context.CompletionNotificationDismissed = null;
            _context.CompletionNotificationCancelled = null;
        }
    }

    public MissionObjectiveInfo GetObjectiveInfo()
    {
        var stepData = _currentStepIndex < _definition.Steps.Count
            ? _definition.Steps[_currentStepIndex] : null;
        return new MissionObjectiveInfo
        (
            _definition.MissionName,
            _definition.Briefing,
            CurrentStep.Description,
            _currentStepIndex,
            _steps.Count,
            stepData != null ? stepData.ObjectiveNotificationDuration : 0f
        );
    }
}
