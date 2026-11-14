using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Zenject;
using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class FlightSceneBootsttrap : MonoBehaviour
{
    private enum FlightView { Player, FirstPerson, ThirdPerson }

    [SerializeField] private Transform spawnPointTransform;
    [SerializeField] private FlightController _flightController;
    [SerializeField] private DroneDebugInput _input;
    [SerializeField] private ArcadeFlightSettings arcadeFlightSettings;
    [SerializeField] private FlightEnvironmentCatalog environmentCatalog;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private MissionController missionController;
    [SerializeField] private SceneMissionBinder missionBinder;
    [SerializeField, Min(0f)] private float thirdPersonDampingSeconds = 0.18f;
    private Clean_AssemblySystem _assemblySystem;
    private DronePhysicsStatsBuilder _physicsBuilder;
    private INotificationService _notifications;
    private FlightView _view;
    private Transform _viewDrone;
    private Camera _playerCamera;
    private Camera _droneCamera;
    private bool _playerCameraWasEnabled;
    private Vector3 _firstPersonOffset;
    private Vector3 _thirdPersonOffset;
    private Quaternion _thirdPersonHeading;
    private readonly List<Canvas> _flightCanvases = new();
    private ToastNotificationView _toastView;
    private FlightHud _hud;

    private void OnDisable()
    {
        _input.ViewChangeRequested -= ChangeView;
        RestorePlayerView();
        _hud?.SetVisible(false);
    }

    private void OnEnable()
    {
        _input.ViewChangeRequested += ChangeView;
        _hud?.SetVisible(true);
    }

    private void OnDestroy()
    {
        _hud?.Destroy();
        if (_droneCamera != null) Destroy(_droneCamera.gameObject);
    }

    private void SetupFlightViews(Transform drone)
    {
        var origin = playerRoot.GetComponentInChildren<Unity.XR.CoreUtils.XROrigin>(true);
        if (origin == null || origin.Camera == null)
        {
            Debug.LogWarning("Flight views require an XR Origin with a camera.", this);
            return;
        }

        _playerCamera = origin.Camera;
        _viewDrone = drone;

        // Project visual bounds onto the drone's forward axis so the camera clears different builds.
        var forward = drone.forward;
        float front = 0f;
        float rear = 0f;
        foreach (var renderer in drone.GetComponentsInChildren<MeshRenderer>())
        {
            if (!renderer.enabled) continue;
            var bounds = renderer.bounds;
            float center = Vector3.Dot(bounds.center - drone.position, forward);
            float extent = Vector3.Dot(bounds.extents,
                new Vector3(Mathf.Abs(forward.x), Mathf.Abs(forward.y), Mathf.Abs(forward.z)));
            front = Mathf.Max(front, center + extent);
            rear = Mathf.Max(rear, extent - center);
        }

        _firstPersonOffset = new Vector3(0f, 0.12f, front + 0.2f);
        _thirdPersonOffset = new Vector3(0f, 0.55f, -rear - 1.7f);
        var cameraObject = new GameObject("Drone Camera");
        cameraObject.transform.SetParent(transform, false);
        _droneCamera = cameraObject.AddComponent<Camera>();
        _droneCamera.CopyFrom(_playerCamera);
        _droneCamera.enabled = false;
        cameraObject.tag = "MainCamera";

        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == _playerCamera)
                    _flightCanvases.Add(canvas);

            if (_toastView == null) _toastView = root.GetComponentInChildren<ToastNotificationView>(true);
        }
    }

    private void ChangeView()
    {
        if (_viewDrone == null || _playerCamera == null || _droneCamera == null) return;
        switch (_view)
        {
            case FlightView.Player:
                _view = FlightView.FirstPerson;
                UpdateDroneCameraPose();
                _playerCameraWasEnabled = _playerCamera.enabled;
                _playerCamera.enabled = false;
                _droneCamera.enabled = true;
                SetInterfaceCamera(_droneCamera);
                _hud?.SetView(_droneCamera, FlightHud.View.FirstPerson);
                break;
            case FlightView.FirstPerson:
                _view = FlightView.ThirdPerson;
                _thirdPersonHeading = Quaternion.Euler(0f, _viewDrone.eulerAngles.y, 0f);
                UpdateDroneCameraPose(true);
                _hud?.SetView(_droneCamera, FlightHud.View.ThirdPerson);
                break;
            case FlightView.ThirdPerson:
                RestorePlayerView();
                break;
        }
    }

    private void LateUpdate()
    {
        if (_view != FlightView.Player)
        {
            if (_viewDrone == null) RestorePlayerView();
            else UpdateDroneCameraPose();
        }

        _hud?.Refresh();
    }

    private void UpdateDroneCameraPose(bool snap = false)
    {
        var cameraTransform = _droneCamera.transform;
        if (_view == FlightView.FirstPerson)
        {
            cameraTransform.SetPositionAndRotation(
                _viewDrone.position + _viewDrone.rotation * _firstPersonOffset, _viewDrone.rotation);
            return;
        }

        var forward = Vector3.ProjectOnPlane(_viewDrone.forward, Vector3.up);
        if (forward.sqrMagnitude > 0.0001f)
            _thirdPersonHeading = Quaternion.LookRotation(forward, Vector3.up);

        float blend = snap || thirdPersonDampingSeconds <= 0f
            ? 1f
            : 1f - Mathf.Exp(-Time.deltaTime / thirdPersonDampingSeconds);
        var targetPosition = _viewDrone.position + _thirdPersonHeading * _thirdPersonOffset;
        cameraTransform.SetPositionAndRotation(
            Vector3.Lerp(cameraTransform.position, targetPosition, blend),
            Quaternion.Slerp(cameraTransform.rotation, _thirdPersonHeading, blend));
    }

    private void SetInterfaceCamera(Camera camera)
    {
        foreach (var canvas in _flightCanvases)
            if (canvas != null) canvas.worldCamera = camera;

        if (_toastView != null) _toastView.SetViewer(camera);
    }

    private void RestorePlayerView()
    {
        if (_view == FlightView.Player) return;
        if (_droneCamera != null) _droneCamera.enabled = false;
        if (_playerCamera != null)
        {
            _playerCamera.enabled = _playerCameraWasEnabled;
            SetInterfaceCamera(_playerCamera);
            _hud?.SetView(_playerCamera, FlightHud.View.Player);
        }
        _view = FlightView.Player;
    }

    [Inject]
    public void Construct(Clean_AssemblySystem assemblySystem, DronePhysicsStatsBuilder physicsBuilder,
        INotificationService notifications)
    {
        _assemblySystem = assemblySystem;
        _physicsBuilder = physicsBuilder;
        _notifications = notifications;
    }

    private async void Start()
    {
        _flightController.enabled = false;
        try
        {
            var environment = await LoadEnvironment();
            if (this == null) return;
            playerRoot.SetPositionAndRotation(environment.playerSpawn.position, environment.playerSpawn.rotation);
            spawnPointTransform = environment.droneSpawn;
            if (FlightSession.Drone != null) await SpawnDrone(environment);
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

    private async Task SpawnDrone(FlightEnvironmentContext environment)
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
        SetupFlightViews(root.transform);
        if (FlightSession.Mission != null)
        {
            if (environment.missionStart == null)
                throw new InvalidOperationException("The flight environment has no mission start zone.");
            missionBinder.BindFlightEnvironment(environment);
            var mission = FlightSession.Mission;
            var runtime = MissionFactory.CreateRuntime(mission, missionBinder);
            missionController.ArmFlightMission(runtime, root.transform, environment.missionStart,
                environment.missionStartRadius);
            _notifications.Info("Подлетите к маркеру старта, чтобы начать миссию.", 5f);
        }
        if (FlightSession.Mode == FlightMode.Arcade)
        {
            root.gameObject.AddComponent<ArcadeFlightController>().Initialize(body, _input, arcadeFlightSettings);
            SetupHud(body, null);
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
        SetupHud(body, data.Battery);
    }

    private void SetupHud(Rigidbody body, BatteryPhysicsData battery)
    {
        if (_playerCamera == null || body == null) return;
        _hud = new FlightHud(body, battery, spawnPointTransform.position.y, _input);
        _hud.SetView(_playerCamera, FlightHud.View.Player);
        _hud.SetVisible(isActiveAndEnabled);
    }
}
