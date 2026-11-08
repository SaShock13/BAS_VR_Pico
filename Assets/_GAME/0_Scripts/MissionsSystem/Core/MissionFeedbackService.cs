using System;
using Zenject;

public sealed class MissionFeedbackService : IInitializable, IDisposable
{
    private readonly IEventBus _events;
    private readonly INotificationService _notifications;
    private IDisposable _subscription;
    private IDisposable _briefingSubscription;

    public MissionFeedbackService(IEventBus events, INotificationService notifications)
    {
        _events = events;
        _notifications = notifications;
    }

    public void Initialize()
    {
        if (_subscription == null)
            _subscription = _events.Subscribe<MissionCompletionFeedbackEvent>(OnCompleted);
        if (_briefingSubscription == null)
            _briefingSubscription = _events.Subscribe<MissionBriefingEvent>(OnBriefing);
    }

    private void OnBriefing(MissionBriefingEvent briefing)
    {
        if (!string.IsNullOrWhiteSpace(briefing.Briefing))
            _notifications.Info(briefing.Briefing, briefing.Duration > 0f ? briefing.Duration : 3f);
    }

    private void OnCompleted(MissionCompletionFeedbackEvent feedback)
    {
        var message = feedback.Message;
        if (string.IsNullOrWhiteSpace(message) && feedback.OnDismissed != null)
            message = "Поздравляю, вы завершили миссию! Сейчас вы перейдёте в полёт.";
        if (!string.IsNullOrWhiteSpace(message))
            _notifications.Success(message, feedback.Sound,
                feedback.Duration > 0f ? feedback.Duration : (feedback.IsMissionComplete ? 4f : 3f),
                feedback.OnDismissed, feedback.OnCancelled);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
        _subscription = null;
        _briefingSubscription?.Dispose();
        _briefingSubscription = null;
    }
}
