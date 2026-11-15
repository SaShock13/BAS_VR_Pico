using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public sealed class FlightMenuView : MonoBehaviour
{
    [SerializeField] private TMP_Text pauseLabel;
    [SerializeField] private string resumeText = "Продолжить";
    [SerializeField] private Button settingsButton, modesButton, restartButton;
    [SerializeField] private Button hudSettingsButton, settingsBackButton, hudBackButton, modesBackButton;
    [SerializeField] private Toggle fpvToggle;
    [SerializeField] private Toggle[] hudToggles;
    [SerializeField] private Button[] modeButtons;
    [SerializeField] private GameObject[] _mainButtons;
    [SerializeField] private RectTransform _hudPage;
    [SerializeField] private RectTransform _settingsPage;
    [SerializeField] private RectTransform _modePage;
    [SerializeField, Min(0f)] private float menuDistance = 1.45f;
    [SerializeField, Min(0.001f)] private float menuScale = 0.055f;

    private IEventBus _bus;
    private IDisposable _pauseSubscription;
    private Camera _playerCamera;
    private string _flightScenePath;

    [Inject]
    public void Construct(IEventBus bus)
    {
        _bus = bus;
        _pauseSubscription = bus.Subscribe<PauseStateChangedEvent>(e =>
        {
            if (e.PausedState != AppState.Flight) return;
            if (e.IsPaused)
            {
                pauseLabel.text = resumeText;
                PlaceInFrontOfCamera();
            }
            gameObject.SetActive(e.IsPaused);
        });
    }

    public void TogglePause() => _bus.Publish(new ToggleFlightPauseRequest());
    public void ReturnToAssembly() => _bus.Publish(new ReturnToAssemblyRequest());

    private void PlaceInFrontOfCamera()
    {
        var origin = GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
        if (origin != null) _playerCamera = origin.Camera;
        var camera = _playerCamera != null ? _playerCamera : Camera.main;
        if (camera == null) return;
        transform.SetParent(null, false);
        var forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();
        transform.SetPositionAndRotation(camera.transform.position + forward * menuDistance,
            Quaternion.LookRotation(forward, Vector3.up));
        transform.localScale = Vector3.one * menuScale;
        var group = GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        GetComponentInChildren<Canvas>(true).worldCamera = camera;
    }

    private void Start()
    {
        _flightScenePath = gameObject.scene.path;
        settingsButton.onClick.AddListener(() => ShowPage(_settingsPage));
        modesButton.onClick.AddListener(() => ShowPage(_modePage));
        restartButton.onClick.AddListener(() =>
            _bus.Publish(new RestartFlightRequest { ScenePath = _flightScenePath }));
        hudSettingsButton.onClick.AddListener(() => ShowPage(_hudPage));
        settingsBackButton.onClick.AddListener(() => ShowPage(null));
        hudBackButton.onClick.AddListener(() => ShowPage(_settingsPage));
        modesBackButton.onClick.AddListener(() => ShowPage(null));

        fpvToggle.SetIsOnWithoutNotify(FlightSession.DroneFirstPersonEnabled);
        fpvToggle.onValueChanged.AddListener(value => FlightSession.DroneFirstPersonEnabled = value);

        for (int i = 0; i < hudToggles.Length; i++)
        {
            int index = i;
            hudToggles[i].SetIsOnWithoutNotify(FlightSession.IsHudFieldVisible(index));
            hudToggles[i].onValueChanged.AddListener(value => FlightSession.SetHudFieldVisible(index, value));
        }

        for (int i = 0; i < modeButtons.Length; i++)
        {
            var mode = (FlightControlMode)i;
            modeButtons[i].onClick.AddListener(() =>
            {
                DroneDebugInput.SelectControlMode(mode);
                ShowPage(null);
            });
        }

        ShowPage(null);
    }

    private void OnDisable() => ShowPage(null);

    private void ShowPage(RectTransform page)
    {
        foreach (var button in _mainButtons)
            button.SetActive(page == null);

        _settingsPage.gameObject.SetActive(page == _settingsPage);
        _hudPage.gameObject.SetActive(page == _hudPage);
        _modePage.gameObject.SetActive(page == _modePage);
    }

    private void OnDestroy() => _pauseSubscription?.Dispose();
}
