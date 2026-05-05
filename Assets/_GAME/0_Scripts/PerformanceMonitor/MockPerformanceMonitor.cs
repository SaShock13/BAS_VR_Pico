using UnityEngine;

public class MockPerformanceMonitor : IPerformanceMonitor
{
    public float CurrentFPS { get; private set; } = 777;

    public float MemoryUsageMB { get; private set; } = 0.001f;

    public void Initialize()
    {

        Debug.Log($"Initialize MockPerformanceMonitor ");
    }

    public void StartMonitoring()
    {

        Debug.Log($"Start Performance Monitoring  {this}");
    }

    public void StopMonitoring()
    {

        Debug.Log($"Stop Performance Monitoring   {this}");
    }
}
