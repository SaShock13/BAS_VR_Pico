using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Zenject;
using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class FlightSceneBootsttrap : MonoBehaviour
{
    [SerializeField] private Transform spawnPointTransform;
    [SerializeField] private FlightController _flightController;
    [SerializeField] private DroneDebugInput _input;
    [SerializeField] private ArcadeFlightSettings arcadeFlightSettings;
    [SerializeField] private FlightEnvironmentCatalog environmentCatalog;
    [SerializeField] private Transform playerRoot;
    private Clean_AssemblySystem _assemblySystem;
    private DronePhysicsStatsBuilder _physicsBuilder;

    [Inject] public void Construct(Clean_AssemblySystem assemblySystem, DronePhysicsStatsBuilder physicsBuilder)
    { _assemblySystem = assemblySystem; _physicsBuilder = physicsBuilder; }

    private async void Start()
    {
        _flightController.enabled = false;
        try
        {
            var environment = await LoadEnvironment();
            if (this == null) return;
            playerRoot.SetPositionAndRotation(environment.playerSpawn.position, environment.playerSpawn.rotation);
            spawnPointTransform = environment.droneSpawn;
            if (FlightSession.Drone != null) await SpawnDrone();
        }
        catch (Exception exception)
        {
            if (this == null) return;
            Debug.LogException(exception);
            // A failed load must not strand the player in an empty scene.
            SceneManager.LoadSceneAsync(MenuNavigationService.ConstructorSceneName, LoadSceneMode.Single);
        }
    }

    private async Task<FlightEnvironmentContext> LoadEnvironment()
    {
        var entry = environmentCatalog.Find(FlightSession.EnvironmentId);
        if (entry == null && environmentCatalog.Environments.Count > 0) entry = environmentCatalog.Environments[0];
        if (entry == null) throw new InvalidOperationException("No flight environments configured.");
        var operation = SceneManager.LoadSceneAsync(entry.scenePath, LoadSceneMode.Additive);
        if (operation == null) throw new InvalidOperationException("Cannot load " + entry.scenePath);
        while (!operation.isDone) await Task.Yield();
        var scene = SceneManager.GetSceneByPath(entry.scenePath);
        if (this == null)
        {
            if (scene.IsValid() && scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
            throw new OperationCanceledException();
        }
        FlightEnvironmentContext context = null;
        foreach (var root in scene.GetRootGameObjects())
        foreach (var candidate in root.GetComponentsInChildren<FlightEnvironmentContext>(true))
        {
            if (context != null) throw new InvalidOperationException("Multiple environment contexts in " + entry.scenePath);
            context = candidate;
        }
        if (context == null || context.playerSpawn == null || context.droneSpawn == null)
            throw new InvalidOperationException("Environment needs player and drone spawn points: " + entry.scenePath);
        SceneManager.SetActiveScene(scene);
        if (LightmapSettings.lightProbes != null && LightmapSettings.lightProbes.count > 0)
            LightProbes.Tetrahedralize();
        return context;
    }

    private async Task SpawnDrone()
    {
        var root = await _assemblySystem.SpawnAssembly(FlightSession.Drone, spawnPointTransform.position, true);
        if (this == null || root == null) return;
        root.transform.rotation = spawnPointTransform.rotation;
        SceneManager.MoveGameObjectToScene(root.gameObject, gameObject.scene);
        var body = root.GetComponent<Rigidbody>();
        var simulation = root.GetComponent<DronePhysicsSimulation>();
        if (simulation != null) simulation.enabled = false;
        root.GetComponent<DroneGrabEnvelope>()?.PrepareForFlight();
        foreach (var grab in root.GetComponentsInChildren<XRGrabInteractable>(true))
        { grab.enabled = false; Destroy(grab); }
        foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
            if (rb != body) Destroy(rb);
        // Let removed child bodies leave the physics world before starting the root body.
        await System.Threading.Tasks.Task.Yield();
        if (this == null || root == null) return;
        if (FlightSession.Mode == FlightMode.Arcade)
        {
            root.gameObject.AddComponent<ArcadeFlightController>().Initialize(body, _input, arcadeFlightSettings);
            return;
        }
        var states = new List<PartDomainState>();
        var motors = new List<DronePartView>();
        foreach (var view in root.GetComponentsInChildren<DronePartView>(true))
        {
            var domain = _assemblySystem.GetPartDomainState(view.InstanceId);
            if (domain == null) continue;
            states.Add(domain);
            if (domain.Type == PartType.Motor) motors.Add(view);
        }
        var data = _physicsBuilder.Build(states, root);
        FlightSession.Physics = data;
        simulation.Initialize(data, motors);
        simulation.enabled = true;
        _flightController.Initialize(_input, simulation, body);
        _flightController.enabled = true;
    }
}
