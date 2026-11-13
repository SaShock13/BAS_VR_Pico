using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public sealed class FlightModeView : MonoBehaviour
{
    [SerializeField] private Toggle arcadeToggle;
    [SerializeField] private Button launchButton;
    [SerializeField] private TMP_Text modeLabel;
    [SerializeField] private string panelId;
    private IEventBus _bus;
    private IDisposable _modeSubscription;

    [Inject] public void Construct(IEventBus bus) => _bus = bus;

    private void Start()
    {
        arcadeToggle.SetIsOnWithoutNotify(FlightSession.Mode == FlightMode.Arcade);
        Render(arcadeToggle.isOn);
        arcadeToggle.onValueChanged.AddListener(SelectMode);
        launchButton.onClick.AddListener(Launch);
        _modeSubscription = _bus.Subscribe<FlightModeRequest>(e =>
        {
            bool arcade = e.Mode == FlightMode.Arcade;
            arcadeToggle.SetIsOnWithoutNotify(arcade);
            Render(arcade);
        });
    }

    private void SelectMode(bool arcade)
    {
        _bus.Publish(new FlightModeRequest { Mode = arcade ? FlightMode.Arcade : FlightMode.Simulation });
    }

    private void Render(bool arcade) => modeLabel.text = arcade ? "Режим: Аркада" : "Режим: Симуляция";
    public void SetLaunchAvailable(bool available) => launchButton.interactable = available;
    private void Launch() => _bus.Publish(new LaunchFlightRequest { PanelId = panelId });

    private void OnDestroy()
    {
        _modeSubscription?.Dispose();
        arcadeToggle.onValueChanged.RemoveListener(SelectMode);
        launchButton.onClick.RemoveListener(Launch);
    }
}
