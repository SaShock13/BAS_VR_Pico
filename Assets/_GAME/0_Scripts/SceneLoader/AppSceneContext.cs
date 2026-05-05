using System;
using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// Контекст загруженной сцены
/// </summary>
public class AppSceneContext
{
    public string SceneName { get; set; }
    public Scene Scene { get; set; }
    public GameObject RootObject { get; set; }
    public LoadSceneMode LoadMode { get; set; }
    public bool IsActive { get; set; }
    public bool IsPreloaded { get; set; }
    public DateTime LoadTime { get; set; }

    public T GetComponentInScene<T>() where T : Component
    {
        if (RootObject != null)
        {
            return RootObject.GetComponentInChildren<T>();
        }
        return null;
    }

    public T[] GetComponentsInScene<T>() where T : Component
    {
        if (RootObject != null)
        {
            return RootObject.GetComponentsInChildren<T>();
        }
        return Array.Empty<T>();
    }
}