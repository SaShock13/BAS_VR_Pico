using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

// Lives in ConstructorScene. Environments contain scenery only, using the Core's coordinates.
public sealed class AssemblyEnvironmentBootstrap : MonoBehaviour
{
    [SerializeField] private AssemblyEnvironmentCatalog catalog;

    private async void Start()
    {
        try
        {
            if (catalog == null) throw new InvalidOperationException("Assembly environment catalog is missing.");
            var entry = catalog.Find(AssemblyEnvironmentSession.EnvironmentId);
            if (entry == null && catalog.Environments.Count > 0) entry = catalog.Environments[0];
            if (entry == null || string.IsNullOrWhiteSpace(entry.id)
                || string.IsNullOrWhiteSpace(entry.scenePath) || entry.scenePath == gameObject.scene.path
                || !Application.CanStreamedLevelBeLoaded(entry.scenePath))
                throw new InvalidOperationException("Assembly environment is missing or not included in Build Profiles.");

            var scene = SceneManager.GetSceneByPath(entry.scenePath);
            if (!scene.isLoaded)
            {
                var operation = SceneManager.LoadSceneAsync(entry.scenePath, LoadSceneMode.Additive);
                if (operation == null) throw new InvalidOperationException("Cannot load " + entry.scenePath);
                while (!operation.isDone) await Task.Yield();
                scene = SceneManager.GetSceneByPath(entry.scenePath);
                if (this == null)
                {
                    if (scene.IsValid() && scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
                    return;
                }
            }
            if (!scene.IsValid() || !scene.isLoaded || !SceneManager.SetActiveScene(scene))
                throw new InvalidOperationException("Cannot activate assembly environment: " + entry.scenePath);
            AssemblyEnvironmentSession.EnvironmentId = entry.id;
            if (LightmapSettings.lightProbes != null && LightmapSettings.lightProbes.count > 0)
                LightProbes.Tetrahedralize();
        }
        catch (Exception exception)
        {
            if (this == null) return;
            Debug.LogException(exception, this);
            // Do not strand the user in a Core scene without a floor or environment.
            if (Application.CanStreamedLevelBeLoaded("BootMenu"))
                SceneManager.LoadSceneAsync("BootMenu", LoadSceneMode.Single);
        }
    }
}
