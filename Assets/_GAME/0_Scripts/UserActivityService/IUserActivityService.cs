public interface IUserActivityService
{
    float IdleTime { get; }

    void NotifyActivity();
}