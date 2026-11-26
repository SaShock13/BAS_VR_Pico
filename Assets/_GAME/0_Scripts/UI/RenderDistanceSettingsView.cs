using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public sealed class RenderDistanceSettingsView : MonoBehaviour
{
    [SerializeField] private Button decreaseButton, increaseButton, resetButton;
    [SerializeField] private TMP_Text distanceLabel;
    [SerializeField] private Toggle fogToggle;
    private IEventBus _bus;
    private IRenderDistanceSettings _settings;

    [Inject]
    public void Construct(IEventBus bus, IRenderDistanceSettings settings)
    {
        _bus = bus;
        _settings = settings;
        _settings.Changed += Refresh;
        Refresh(settings.Distance);
    }

    private void Awake()
    {
        decreaseButton.onClick.AddListener(Decrease);
        increaseButton.onClick.AddListener(Increase);
        resetButton.onClick.AddListener(ResetDistance);
        fogToggle.onValueChanged.AddListener(SetFog);
    }

    private void OnEnable() { if (_settings != null) Refresh(_settings.Distance); }
    private void Decrease() => SetDistance(_settings.Distance - RenderDistancePolicy.Step);
    private void Increase() => SetDistance(_settings.Distance + RenderDistancePolicy.Step);
    private void ResetDistance() => SetDistance(RenderDistancePolicy.Default);
    private void SetDistance(float distance) => _bus.Publish(new SetRenderDistanceRequest { Distance = distance });
    private void SetFog(bool enabled) => _bus.Publish(new SetFogEnabledRequest { Enabled = enabled });

    private void Refresh(float distance)
    {
        distanceLabel.text = $"Дальность прорисовки: {distance:0} м";
        decreaseButton.interactable = distance > RenderDistancePolicy.Minimum;
        increaseButton.interactable = distance < RenderDistancePolicy.Maximum;
        fogToggle.SetIsOnWithoutNotify(_settings.FogEnabled);
    }

    private void OnDestroy()
    {
        if (_settings != null) _settings.Changed -= Refresh;
        decreaseButton.onClick.RemoveListener(Decrease);
        increaseButton.onClick.RemoveListener(Increase);
        resetButton.onClick.RemoveListener(ResetDistance);
        fogToggle.onValueChanged.RemoveListener(SetFog);
    }
}
