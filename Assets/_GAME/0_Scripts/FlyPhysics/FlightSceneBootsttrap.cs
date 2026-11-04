using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Zenject;

public class FlightSceneBootsttrap : MonoBehaviour
{
    [SerializeField] private Transform spawnPointTransform;
    [SerializeField] private FlightController _flightController;
    [SerializeField] private DroneDebugInput _input;
    [SerializeField] private ArcadeFlightSettings arcadeFlightSettings;
    private Clean_AssemblySystem _assemblySystem;
    private DronePhysicsStatsBuilder _physicsBuilder;

    [Inject] public void Construct(Clean_AssemblySystem assemblySystem, DronePhysicsStatsBuilder physicsBuilder)
    { _assemblySystem = assemblySystem; _physicsBuilder = physicsBuilder; }

    private async void Start()
    {
        if (FlightSession.Drone == null) return;
        _flightController.enabled = false;
        var root = await _assemblySystem.SpawnAssembly(FlightSession.Drone, spawnPointTransform.position, true);
        if (this == null || root == null) return;
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
