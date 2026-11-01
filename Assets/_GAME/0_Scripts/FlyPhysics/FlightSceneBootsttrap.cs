using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Zenject;

public class FlightSceneBootsttrap : MonoBehaviour
{
    private Clean_AssemblySystem _assemblySystem;
    private DronePhysicsStatsBuilder _physicsBuilder;

    private Transform _droneTransform;
    private Rigidbody _droneRb;
    private DroneDebugInput _input;
    private DronePhysicsSimulation _physicsSimulation;
    private DronePhysicsData _lastData;


    [SerializeField] Transform spawnPointTransform;
    [SerializeField] private FlightController _flightController;

    [Inject]
    public void Construct(
        Clean_AssemblySystem assemblySystem,
        DronePhysicsStatsBuilder physicsBuilder
        )
    {
        _assemblySystem = assemblySystem;
        _physicsBuilder = physicsBuilder;
    }



    async void Start()
    {


        if (FlightSession.Drone != null)
        {
            _droneTransform = await _assemblySystem.SpawnAssembly(
                FlightSession.Drone,
                spawnPointTransform.position,
                true);
        }

    }


    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            SceneManager.LoadScene(0);
        }



        if (Input.GetKeyDown(KeyCode.F))
        {
            if(_droneTransform == null) return;

            FindDrone();
            SetupFlightController();
            RunTest();
        }



    }

    private void SetupFlightController()
    {
        _input = FindAnyObjectByType<DroneDebugInput>();

        if (_input != null && _physicsSimulation != null)
        {
            _flightController.Initialize(_input,_physicsSimulation, _droneRb);
        }
    }

    private void FindDrone()
    {
        _physicsSimulation = _droneTransform.GetComponent<DronePhysicsSimulation>();      

        _droneRb = _droneTransform.GetComponent<Rigidbody>();

        if(_flightController == null) FindAnyObjectByType(typeof(FlightController));

    }

    public void CreateDrone(
        DronePhysicsData physicsData
        //,GameObject dronePrefab
        )
    {
        //GameObject drone =
        //    Object.Instantiate(dronePrefab);

        var allViews = _droneTransform.GetComponentsInChildren<DronePartView>();

        List<DronePartView> motorViews = new List<DronePartView>();

        foreach (DronePartView view in allViews)   /// todo обьединить с другим проходом по всем вью.
        {
            var domain = _assemblySystem.GetPartDomainState(view.InstanceId);

            //Debug.Log($"тип детали {view.transform.name} дрона {domain.Type}");
            if (domain != null && domain.Type == PartType.Motor) motorViews.Add(view);
        }


        //DronePhysicsSimulation simulation =
        //    _droneRoot.GetComponent<DronePhysicsSimulation>();


        _physicsSimulation.Initialize(
            physicsData
            , motorViews
            );

        _physicsSimulation.enabled = true;
    }

    private void RunTest()
    {
        if (_droneTransform == null)
        {
            Debug.LogError("Drone root not assigned");
            return;
        }

        DronePartView[] views =
            _droneTransform.GetComponentsInChildren<DronePartView>(true);

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

            var domain = _assemblySystem.GetPartDomainState(view.InstanceId);

            if (domain == null)
                continue;

            domainStates.Add(domain);
        }

        _lastData =
            _physicsBuilder.Build(domainStates, _droneTransform);

        //_applier = new DronePhysicsApplier();

        Print(_lastData);


        //_droneRb = _droneRoot.GetComponent<Rigidbody>();
        //Debug.Log($"_applier {_applier != null}  _droneRb {_droneRb != null}");
        DeactivateChildRBs();

        //_applier.Apply(_droneRb, _lastData);

        CreateDrone(_lastData);
    }

    private void DeactivateChildRBs()
    {
        var xrInteractables = _droneTransform.GetComponentsInChildren<XRGrabInteractable>();
        var rigidBodies = _droneTransform.GetComponentsInChildren<Rigidbody>();
        var mainRb = _droneTransform.GetComponent<Rigidbody>();

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
        Debug.Log($"YAw BIAS : {data.YawBias}");
        Debug.Log($"bbbbbbbBattery : {data.Battery != null}");
        foreach (var motor in data.Motors)
        {

            Debug.Log($"motor.RotationDirection {motor.RotationDirection}");
            Debug.Log($"motor YawFactor {motor.MixData.YawFactor}");
        }

        Debug.Log("==================================");
    }
}

