using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Zenject;

public class PhysicsBuilderTester : MonoBehaviour
{
    [Header("Drone Root (drag here)")]
    [SerializeField] private Transform _droneRoot;
    [SerializeField] private Rigidbody _droneRb;

    private DronePhysicsBuilder _physicsBuilder;
    [Inject] private Clean_AssemblySystem _domainRegistry;
    [Inject] private PartViewRegistry _viewRegistry;
    [Inject] private IPartConfigRegistry _configRegistry;
    DronePhysicsApplier _applier;


    private void Awake()
    {
        _physicsBuilder = new DronePhysicsBuilder(_configRegistry, _viewRegistry);
        //_droneRb = _droneRoot.GetComponent<Rigidbody>();

    }
    private DronePhysicsData _lastData;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            RunTest();
        }
        //if (Input.GetKeyDown(KeyCode.T))
        //{
        //    _droneRb.AddTorque(
        //        Vector3.forward * 20f,
        //        ForceMode.Impulse);
        //}
        //if (Input.GetKeyDown(KeyCode.F))
        //{
        //    _droneRb.AddForceAtPosition(
        //        Vector3.up * 20f,
        //        _droneRoot.position,
        //        ForceMode.Impulse);
        //}
    }

    public void CreateDrone(
        DronePhysicsData physicsData
        //,GameObject dronePrefab
        )
    {
        //GameObject drone =
        //    Object.Instantiate(dronePrefab);

        var allViews = _droneRoot.GetComponentsInChildren<DronePartView>();

        List <DronePartView> motorViews = new List<DronePartView> ();

        foreach ( DronePartView view in allViews )   /// todo обьединить с другим проходом по всем вью.
        {
            var domain = _domainRegistry.GetDomainState(view.InstanceId);

            Debug.Log($"тип детали {view.transform.name} дрона {domain.Type}");
            if (domain != null && domain.Type == PartType.Motor) motorViews.Add(view);
        }
            

        DronePhysicsSimulation simulation =
            _droneRoot.GetComponent<DronePhysicsSimulation>();

        simulation.Initialize(
            physicsData
            ,motorViews
            );
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

        _applier = new DronePhysicsApplier();

        Print(_lastData);


        //_droneRb = _droneRoot.GetComponent<Rigidbody>();
        Debug.Log($"_applier {_applier != null}  _droneRb {_droneRb != null}");
        DeactivateChildRBs();

        //_applier.Apply(_droneRb, _lastData);

        Debug.Log($"!!!!!!!!Моторов количество {_lastData.Motors.Count}");
        CreateDrone(_lastData);
    }

    private void DeactivateChildRBs()
    {
        var xrInteractables = _droneRoot.GetComponentsInChildren<XRGrabInteractable>();
        var rigidBodies = _droneRoot.GetComponentsInChildren<Rigidbody>();
        var mainRb = _droneRoot.GetComponent<Rigidbody>();

        foreach (var interactable in xrInteractables)
        {
            Destroy(interactable);
        }

        foreach (var rb in rigidBodies)
        {
            if (mainRb == rb) continue;
            Destroy(rb);
        }
    }

    private void Print(DronePhysicsData data)
    {
        Debug.Log("===== DRONE PHYSICS SNAPSHOT =====");
        Debug.Log($"Mass: {data.TotalMass}");
        Debug.Log($"COM: {data.LocalCenterOfMass}");
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
            _droneRoot.TransformPoint(_lastData.LocalCenterOfMass);

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
            _droneRoot.TransformPoint(_lastData.LocalCenterOfMass);

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