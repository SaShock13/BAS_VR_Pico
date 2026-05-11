using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class PhysicsBuilderTester : MonoBehaviour
{
    [Header("Drone Root (drag here)")]
    [SerializeField] private Transform _droneRoot;

    private DronePhysicsBuilder _physicsBuilder;
    [Inject] private Clean_AssemblySystem _domainRegistry;
    [Inject] private PartViewRegistry _viewRegistry;
    [Inject] private IPartConfigRegistry _configRegistry;


    private void Awake()
    {
        _physicsBuilder = new DronePhysicsBuilder(_configRegistry, _viewRegistry);
    }
    private DronePhysicsData _lastData;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            RunTest();
        }
    }

    private void RunTest()
    {
        if (_droneRoot == null)
        {
            Debug.LogError("Drone root not assigned");
            return;
        }

        DronePartView[] views =
            _droneRoot.GetComponentsInChildren<DronePartView>(true);

        if (views == null || views.Length == 0)
        {
            Debug.LogWarning("No DronePartView found");
            return;
        }

        List<PartDomainState> domainStates = new();

        foreach (var view in views)
        {
            if (view == null)
                continue;

            var domain = _domainRegistry.GetDomainState(view.InstanceId);

            if (domain == null)
                continue;

            domainStates.Add(domain);
        }

        _lastData =
            _physicsBuilder.Build(domainStates, _droneRoot);

        Print(_lastData);
    }

    private void Print(DronePhysicsData data)
    {
        Debug.Log("===== DRONE PHYSICS SNAPSHOT =====");
        Debug.Log($"Mass: {data.TotalMass}");
        Debug.Log($"COM: {data.CenterOfMass}");
        Debug.Log($"Motors: {data.Motors?.Count ?? 0}");
        Debug.Log($"HoverThrottle: {data.HoverThrottle}");
        Debug.Log("==================================");
    }

    // ---------------------------------------------------
    // VISUAL DEBUG (GIZMOS)
    // ---------------------------------------------------

    private void OnDrawGizmos()
    {
        if (_lastData == null)
            return;

        if (_droneRoot == null)
            return;

        // --- CENTER OF MASS ---
        Gizmos.color = Color.red;
        Vector3 comWorld =
            _droneRoot.TransformPoint(_lastData.CenterOfMass);

        Gizmos.DrawSphere(comWorld, 0.05f);

        Gizmos.DrawLine(_droneRoot.position, comWorld);

        // --- MOTORS ---
        if (_lastData.Motors != null)
        {
            foreach (var motor in _lastData.Motors)
            {
                Vector3 motorWorldPos =
                    _droneRoot.TransformPoint(motor.LocalPosition);

                Vector3 motorWorldDir =
                    _droneRoot.TransformDirection(motor.LocalDirection);

                // motor position
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(motorWorldPos, 0.03f);

                // thrust direction
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(
                    motorWorldPos,
                    motorWorldPos + motorWorldDir * 0.2f);

                // thrust force indicator (scaled)
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(
                    motorWorldPos,
                    motorWorldPos + motorWorldDir * (motor.MaxThrust * 0.01f));
            }
        }
    }

    // ---------------------------------------------------
    // OPTIONAL RUNTIME DEBUG LINES
    // ---------------------------------------------------

    private void LateUpdate()
    {
        if (_lastData == null)
            return;

        if (_droneRoot == null)
            return;

        // COM LINE
        Vector3 comWorld =
            _droneRoot.TransformPoint(_lastData.CenterOfMass);

        Debug.DrawLine(_droneRoot.position, comWorld, Color.red);

        // MOTORS
        if (_lastData.Motors == null)
            return;

        foreach (var motor in _lastData.Motors)
        {
            Vector3 motorWorldPos =
                _droneRoot.TransformPoint(motor.LocalPosition);

            Vector3 motorWorldDir =
                _droneRoot.TransformDirection(motor.LocalDirection);

            Debug.DrawRay(
                motorWorldPos,
                motorWorldDir * 0.2f,
                Color.cyan);

            Debug.DrawRay(
                motorWorldPos,
                motorWorldDir * (motor.MaxThrust * 0.01f),
                Color.yellow);
        }
    }
}