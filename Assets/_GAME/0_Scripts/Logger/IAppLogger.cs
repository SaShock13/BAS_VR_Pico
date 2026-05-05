public interface IAppLogger
{
    bool IsDebugEnabled { get; }

    void Initialize();
    void Log(string message);
    void LogError(string message);  
    void LogWarning(string message);

}