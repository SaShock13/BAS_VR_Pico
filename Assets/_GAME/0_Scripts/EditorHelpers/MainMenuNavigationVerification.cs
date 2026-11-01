#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Zenject;

public static class MainMenuNavigationVerification
{
    private class TestLogger : IAppLogger
    {
        public bool IsDebugEnabled => false;
        public void Initialize() { }
        public void Log(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
    }

    private class TestLoader : ISceneLoader
    {
        public int Loads;
        public int Activations;
        public string Scene;
        public Task Pending = Task.CompletedTask;
        public void Initialize() { }
        public void Dispose() { }
        public Task PreloadSceneAsync(string scene, Action callback, LoadSceneMode mode = LoadSceneMode.Single)
        {
            Loads++;
            Scene = scene;
            return Pending;
        }
        public Task ActivatePreloadedScene() { Activations++; return Task.CompletedTask; }
    }

    [UnityEditor.MenuItem("Tools/BAS VR/Verify Main Menu Navigation")]
    public static void Run()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Edit Mode check only.");
        var logger = new TestLogger();
        using (var bus = new EventBus(logger, new DiContainer()))
        {
            bus.Initialize();
            var loader = new TestLoader();
            int errors = 0;
            using(var status = bus.Subscribe<MenuNavigationStatusEvent>(e => { if(e.Error != null) errors++; }))
            {
                using(var navigation = new MenuNavigationService(bus, loader, logger))
                {
                    navigation.Initialize();
                    bus.Publish(new RequestStartConstructorEvent());
                    bus.Publish(new RequestStartConstructorEvent());
                    if(loader.Scene != "ConstructorScene" || loader.Loads != 1 || loader.Activations != 1)
                        throw new Exception("Successful transition or repeated-click guard failed");
                }
                int previous = loader.Loads;
                bus.Publish(new RequestStartConstructorEvent());
                if(loader.Loads != previous)throw new Exception("Subscription survives disposal");
                loader = new TestLoader { Pending = Task.FromException(new Exception("Expected test failure")) };
                using(var navigation = new MenuNavigationService(bus, loader, logger))
                {
                    navigation.Initialize();
                    bus.Publish(new RequestStartConstructorEvent());
                    if(errors != 1 || loader.Activations != 0)throw new Exception("Error recovery failed");
                    loader.Pending = Task.CompletedTask;
                    bus.Publish(new RequestStartConstructorEvent());
                    if(loader.Loads != 2 || loader.Activations != 1)throw new Exception("Retry failed");
                }
                loader = new TestLoader { Pending = new TaskCompletionSource<bool>().Task };
                using(var navigation = new MenuNavigationService(bus, loader, logger))
                {
                    navigation.Initialize();
                    bus.Publish(new RequestStartConstructorEvent());
                    bus.Publish(new RequestStartConstructorEvent());
                    if(loader.Loads != 1 || loader.Activations != 0)throw new Exception("In-flight repeated-click guard failed");
                }
            }
        }
        UnityEngine.Debug.Log("PASS: ConstructorScene transition; repeated clicks while pending and after activation; error status and retry; unsubscribe on disposal. No runtime scenes loaded.");
    }
}

#endif
