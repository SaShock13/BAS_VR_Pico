using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

/// <summary>
/// Сервис загрузки сцен с поддержкой предзагрузки
/// </summary>
public interface ISceneLoader : IInitializable, IDisposable
{
    // Основные методы
    //Task<AppSceneContext> LoadSceneAdditiveAsync(string sceneName, bool activateImmediately = true);
    //Task UnloadSceneAsync(string sceneName);
    //Task<AppSceneContext> PreloadSceneAsync(string sceneName);  // С использованием контекста сцены, пока лишнее
    Task PreloadSceneAsync(string sceneName, Action callback, LoadSceneMode mode = LoadSceneMode.Single);
    //Task ActivatePreloadedScene(string sceneName); // С прицелом на несколько предзагруженных сцен
    Task ActivatePreloadedScene();

    // Проверки
    //bool IsSceneLoaded(string sceneName);
    //bool IsScenePreloaded(string sceneName);
    //AppSceneContext GetSceneContext(string sceneName);

    // Управление
    //void UnloadAllScenesExcept(string[] scenesToKeep);
}