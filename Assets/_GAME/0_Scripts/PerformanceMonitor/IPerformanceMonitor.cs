using UnityEngine;

public interface IPerformanceMonitor
{
    float CurrentFPS { get; }
    float MemoryUsageMB { get; }

    void Initialize();
    void StartMonitoring();
    void StopMonitoring();
}
