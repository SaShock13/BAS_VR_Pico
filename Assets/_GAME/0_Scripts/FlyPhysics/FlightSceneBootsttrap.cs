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
    private LineRenderer[] _controllerRays;
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
    private IRenderDistanceSettings _renderDistanceSettings;
    private bool _playerCameraWasEnabled;
    private Vector3 _firstPersonOffset;
    private Vector3 _thirdPersonOffset;
    private Quaternion _thirdPersonHeading;
    private Quaternion _thirdPersonBaseRotation;
    private Quaternion _neutralHeadRotation;
    private readonly List<Canvas> _flightCanvases = new();
    private ToastNotificationView _toastView;
    private FlightHud _hud;
    private Transform _fpvController;
    private Transform _leftModelStick;
    private Transform _rightModelStick;
    private Quaternion _leftStickRest;
    private Quaternion _rightStickRest;
    private Transform _heldController, _leftHand, _rightHand;
    private Transform _heldLeftStick, _heldRightStick;
    private GameObject _leftHandVisual, _rightHandVisual;
    private RenderTexture _fpvFeed;
    private Camera _feedCamera;
    private RectTransform _heldScreen;
    [Header("Held FPV Controller")]
    [SerializeField] private Vector3 heldControllerRotation = new Vector3(0f, 180f, 0f);
    [SerializeField] private Vector3 heldScreenPosition = new Vector3(-0.105f, 0.2255f, 0.1f);
    [SerializeField] private Vector3 heldScreenRotation = new Vector3(90f, 0f, 180f);
    [SerializeField] private Vector2 heldScreenSize = new Vector2(320f, 180f);
    private const int HeldControllerLayer = 31;
    private const int FeedHudLayer = 30;

    [Header("FPV Vignette")]
    [SerializeField, Range(0.15f, 0.95f)] private float vignetteClearRadius = 0.56f;
    [SerializeField, Range(0f, 1f)] private float vignetteOpacity = 1f;
    [SerializeField, Range(0.7f, 1.6f), Tooltip("Higher values bring the dark border inward.")]
    private float vignetteScale = 1.15f;
    private const string VignetteRadiusKey = "Flight.FpvVignetteClearRadius";
    private float _previewedVignetteRadius = 0.56f;
    private Vector2 _previewedVignetteAppearance;

    [ContextMenu("Save FPV Vignette Size")]
    private void SaveVignetteSize()
    {
        PlayerPrefs.SetFloat(VignetteRadiusKey, vignetteClearRadius);
        PlayerPrefs.SetFloat("Flight.FpvVignetteOpacity", vignetteOpacity);
        PlayerPrefs.SetFloat("Flight.FpvVignetteScale", vignetteScale);
        PlayerPrefs.Save();
    }

    private void OnValidate()
    {
        if (_heldScreen == null) return;
        _heldScreen.localPosition = heldScreenPosition;
        _heldScreen.localRotation = Quaternion.Euler(heldScreenRotation);
        _heldScreen.sizeDelta = heldScreenSize;
    }
    private IDisposable _menuPauseSubscription;
    private FlightView _viewBeforePause;
    private bool _menuPaused;

    private void OnDisable()
    {
        _input.ViewChangeRequested -= ChangeView;
        RestorePlayerView();
        _hud?.SetVisible(false);
    }

    private void OnEnable()
    {
        _input.ViewChangeRequested += ChangeView;
        _hud?.SetVisible(_view != FlightView.Player || _feedCamera != null);
    }

    private void OnDestroy()
    {
        _menuPauseSubscription?.Dispose();
        _renderDistanceSettings?.UnregisterCamera(_droneCamera);
        _renderDistanceSettings?.UnregisterCamera(_feedCamera);
        if (_heldController != null) SetHeldControllerVisible(false);
        if (_fpvFeed != null) Destroy(_fpvFeed);
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
        _renderDistanceSettings?.RegisterCamera(_droneCamera);
        _droneCamera.enabled = false;
        cameraObject.tag = "MainCamera";
        SetupFpvController();
        SetupHeldController(origin.transform);

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
        if (_menuPaused) return;
        AdvanceView();
    }

    private void AdvanceView()
    {
        var next = _view switch
        {
            FlightView.Player when FlightSession.DroneFirstPersonEnabled => FlightView.FirstPerson,
            FlightView.Player or FlightView.FirstPerson => FlightView.ThirdPerson,
            _ => FlightView.Player
        };
        SelectView(next);
    }

    private void SelectView(FlightView next)
    {
        if (next == FlightView.Player)
        {
            RestorePlayerView();
            return;
        }
        if (_viewDrone == null || _playerCamera == null || _droneCamera == null) return;
        _neutralHeadRotation = _playerCamera.transform.localRotation;
        if (_view == FlightView.Player)
        {
            SetHeldControllerVisible(false);
            _playerCameraWasEnabled = _playerCamera.enabled;
            _playerCamera.enabled = false;
            _droneCamera.enabled = true;
            SetInterfaceCamera(_droneCamera);
        }
        _view = next;
        if (next == FlightView.ThirdPerson)
            _thirdPersonHeading = Quaternion.Euler(0f, _viewDrone.eulerAngles.y, 0f);
        UpdateDroneCameraPose(true);
        _hud?.SetView(_droneCamera, next == FlightView.FirstPerson
            ? FlightHud.View.FirstPerson : FlightHud.View.ThirdPerson);
        _hud?.SetVisible(true);
    }

    private void LateUpdate()
    {
        if (_hud != null && _previewedVignetteRadius != vignetteClearRadius)
        {
            _previewedVignetteRadius = vignetteClearRadius;
            _hud.SetVignetteRadius(vignetteClearRadius);
        }
        var appearance = new Vector2(vignetteOpacity, vignetteScale);
        if (_hud != null && appearance != _previewedVignetteAppearance)
        {
            _previewedVignetteAppearance = appearance;
            _hud.SetVignetteAppearance(appearance.x, appearance.y);
        }
        if (_view != FlightView.Player)
        {
            if (_viewDrone == null) RestorePlayerView();
            else UpdateDroneCameraPose();
        }

        _hud?.Refresh();
        if (_view == FlightView.Player && _heldController != null && _viewDrone != null && !_menuPaused)
            UpdateHeldController();
    }

    private void SetupHeldController(Transform rig)
    {
        if (_fpvController == null) return;
        foreach (var child in rig.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "[Building Block] Left Controller") _leftHandVisual = child.gameObject;
            if (child.name == "[Building Block] Right Controller") _rightHandVisual = child.gameObject;
        }
        if (_leftHandVisual == null || _rightHandVisual == null) return;
        _leftHand = _leftHandVisual.transform.parent;
        _rightHand = _rightHandVisual.transform.parent;
        _heldController = Instantiate(_fpvController.gameObject, transform, false).transform;
        _heldController.localScale = Vector3.one * 0.48f;
        _heldLeftStick = _heldController.Find("Sphere001");
        _heldRightStick = _heldController.Find("Sphere002");

        _fpvFeed = new RenderTexture(512, 288, 16) { name = "Drone FPV Feed" };
        var feed = new GameObject("Drone FPV Feed Camera");
        feed.transform.SetParent(transform, false);
        _feedCamera = feed.AddComponent<Camera>();
        _feedCamera.CopyFrom(_droneCamera);
        _renderDistanceSettings?.RegisterCamera(_feedCamera);
        _feedCamera.stereoTargetEye = StereoTargetEyeMask.None;
        _feedCamera.targetTexture = _fpvFeed;
        _feedCamera.aspect = (float)_fpvFeed.width / _fpvFeed.height;
        _feedCamera.allowHDR = false;
        _feedCamera.allowMSAA = false;
        _feedCamera.cullingMask &= ~(1 << HeldControllerLayer);
        _feedCamera.cullingMask |= 1 << FeedHudLayer;
        _playerCamera.cullingMask &= ~(1 << FeedHudLayer);
        _droneCamera.cullingMask &= ~(1 << FeedHudLayer);
        _fpvFeed.Create();

        var screen = new GameObject("FPV Screen", typeof(RectTransform), typeof(Canvas));
        var rect = (RectTransform)screen.transform;
        _heldScreen = rect;
        rect.SetParent(_heldController, false);
        rect.localScale = Vector3.one * 0.002f;
        OnValidate();
        screen.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var display = new GameObject("Video", typeof(RectTransform)).AddComponent<UnityEngine.UI.RawImage>();
        display.rectTransform.SetParent(rect, false);
        display.rectTransform.anchorMin = Vector2.zero;
        display.rectTransform.anchorMax = Vector2.one;
        display.rectTransform.offsetMin = Vector2.zero;
        display.rectTransform.offsetMax = Vector2.zero;
        display.texture = _fpvFeed;
        display.raycastTarget = false;
        foreach (var child in _heldController.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = HeldControllerLayer;
        _playerCamera.cullingMask |= 1 << HeldControllerLayer;
        SetHeldControllerVisible(true);
    }

    private void SetHeldControllerVisible(bool visible)
    {
        if (_heldController == null) return;
        _heldController.gameObject.SetActive(visible);
        _feedCamera.enabled = visible;
        _leftHandVisual.SetActive(!visible);
        _rightHandVisual.SetActive(!visible);
    }

    private void UpdateHeldController()
    {
        var aim = Quaternion.Slerp(_leftHand.rotation, _rightHand.rotation, 0.5f);
        var across = _rightHand.position - _leftHand.position;
        var forward = Vector3.Cross(across, aim * Vector3.up);
        var rotation = forward.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(forward, Vector3.Cross(forward, across))
            : aim;
        var midpoint = (_leftHand.position + _rightHand.position) * 0.5f;
        rotation *= Quaternion.Euler(heldControllerRotation);
        var gripCenter = Vector3.Scale(new Vector3(-0.105f, 0f, 0f), _heldController.localScale);
        _heldController.SetPositionAndRotation(midpoint - rotation * gripCenter, rotation);
        _feedCamera.transform.SetPositionAndRotation(
            _viewDrone.position + _viewDrone.rotation * _firstPersonOffset, _viewDrone.rotation);
        var input = _input.CurrentInput;
        MoveModelStick(_heldLeftStick, _rightStickRest, input.Yaw, -_input.Vertical);
        MoveModelStick(_heldRightStick, _leftStickRest, -input.Roll, -input.Pitch);
    }

    private void SetupFpvController()
    {
        var model = Resources.Load<GameObject>("FlightController/Контроллер_Whole_2");
        if (model == null) return;

        _fpvController = Instantiate(model, _droneCamera.transform, false).transform;
        _fpvController.localPosition = new Vector3(0.035f, -0.26f, 0.75f);
        _fpvController.localRotation = Quaternion.Euler(-58f, 0f, 0f);
        _fpvController.localScale = Vector3.one * 0.34f;
        foreach (var renderer in _fpvController.GetComponentsInChildren<MeshRenderer>())
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _leftModelStick = _fpvController.Find("Sphere002");
        _rightModelStick = _fpvController.Find("Sphere001");
        if (_leftModelStick != null) _leftStickRest = _leftModelStick.localRotation;
        if (_rightModelStick != null) _rightStickRest = _rightModelStick.localRotation;
        _fpvController.gameObject.SetActive(false);
    }

    private static void MoveModelStick(Transform stick, Quaternion rest, float horizontal, float vertical)
    {
        if (stick == null) return;
        var rotation = Quaternion.Euler(vertical * 20f, 0f, -horizontal * 20f) * rest;
        if (stick.localRotation != rotation) stick.localRotation = rotation;
    }

    private void UpdateDroneCameraPose(bool snap = false)
    {
        var cameraTransform = _droneCamera.transform;
        var headLook = Quaternion.Inverse(_neutralHeadRotation) * _playerCamera.transform.localRotation;
        if (_view == FlightView.FirstPerson)
        {
            cameraTransform.SetPositionAndRotation(
                _viewDrone.position + _viewDrone.rotation * _firstPersonOffset, _viewDrone.rotation * headLook);
            return;
        }

        var forward = Vector3.ProjectOnPlane(_viewDrone.forward, Vector3.up);
        if (forward.sqrMagnitude > 0.0001f)
            _thirdPersonHeading = Quaternion.LookRotation(forward, Vector3.up);

        float blend = snap || thirdPersonDampingSeconds <= 0f
            ? 1f
            : 1f - Mathf.Exp(-Time.deltaTime / thirdPersonDampingSeconds);
        var targetPosition = _viewDrone.position + _thirdPersonHeading * _thirdPersonOffset;
        _thirdPersonBaseRotation = snap ? _thirdPersonHeading :
            Quaternion.Slerp(_thirdPersonBaseRotation, _thirdPersonHeading, blend);
        cameraTransform.SetPositionAndRotation(
            Vector3.Lerp(cameraTransform.position, targetPosition, blend),
            _thirdPersonBaseRotation * headLook);
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
        if (_fpvController != null) _fpvController.gameObject.SetActive(false);
        if (_playerCamera != null)
        {
            _playerCamera.enabled = _playerCameraWasEnabled;
            SetInterfaceCamera(_playerCamera);
            SetPlayerHud();
        }
        _view = FlightView.Player;
        SetHeldControllerVisible(!_menuPaused);
    }

    [Inject]
    public void ConstructRenderDistance(IRenderDistanceSettings settings) => _renderDistanceSettings = settings;

    [Inject]
    public void Construct(Clean_AssemblySystem assemblySystem, DronePhysicsStatsBuilder physicsBuilder,
        INotificationService notifications, IEventBus events)
    {
        _assemblySystem = assemblySystem;
        _physicsBuilder = physicsBuilder;
        _notifications = notifications;
        _menuPauseSubscription = events.Subscribe<PauseStateChangedEvent>(OnFlightPauseChanged);
    }

    private void OnFlightPauseChanged(PauseStateChangedEvent state)
    {
        if (state.PausedState != AppState.Flight || state.IsPaused == _menuPaused) return;
        _menuPaused = state.IsPaused;
        SetControllerRaysVisible(_menuPaused);
        SetHeldControllerVisible(!state.IsPaused && _view == FlightView.Player);
        if (_menuPaused)
        {
            _viewBeforePause = _view;
            RestorePlayerView();
        }
        else if (_viewBeforePause != FlightView.Player)
        {
            SelectView(_viewBeforePause == FlightView.FirstPerson && !FlightSession.DroneFirstPersonEnabled
                ? FlightView.ThirdPerson : _viewBeforePause);
        }
    }

    private void SetControllerRaysVisible(bool visible)
    {
        if (_controllerRays == null) return;
        foreach (var ray in _controllerRays)
            if (ray != null) ray.forceRenderingOff = !visible;
    }

    private async void Start()
    {
        _controllerRays = playerRoot.GetComponentsInChildren<LineRenderer>(true);
        SetControllerRaysVisible(_menuPaused);
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
        if (body != null) body.interpolation = RigidbodyInterpolation.Interpolate;
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
        vignetteClearRadius = Mathf.Clamp(PlayerPrefs.GetFloat(VignetteRadiusKey, vignetteClearRadius), 0.15f, 0.95f);
        _previewedVignetteRadius = 0.56f;
        vignetteOpacity = Mathf.Clamp01(PlayerPrefs.GetFloat("Flight.FpvVignetteOpacity", vignetteOpacity));
        vignetteScale = Mathf.Clamp(PlayerPrefs.GetFloat("Flight.FpvVignetteScale", vignetteScale), 0.7f, 1.6f);
        _previewedVignetteAppearance = new Vector2(vignetteOpacity, vignetteScale);
        _hud.SetVignetteAppearance(vignetteOpacity, vignetteScale);
        SetPlayerHud();
    }

    private void SetPlayerHud()
    {
        if (_hud == null) return;
        if (_feedCamera != null) _hud.SetView(_feedCamera, FlightHud.View.FirstPerson, FeedHudLayer);
        _hud.SetVisible(_feedCamera != null && isActiveAndEnabled);
    }
}
