using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public sealed class FlightModeView : MonoBehaviour
{
    [SerializeField] private Toggle arcadeToggle;
    [SerializeField] private Button launchButton;
    [SerializeField] private TMP_Text modeLabel;
    private IEventBus _bus;

    [Inject] public void Construct(IEventBus bus) => _bus = bus;

    private void Start()
    {
        arcadeToggle.SetIsOnWithoutNotify(FlightSession.Mode == FlightMode.Arcade);
        Render(arcadeToggle.isOn);
        arcadeToggle.onValueChanged.AddListener(SelectMode);
        launchButton.onClick.AddListener(Launch);
    }

    private void SelectMode(bool arcade)
    {
        _bus.Publish(new FlightModeRequest { Mode = arcade ? FlightMode.Arcade : FlightMode.Simulation });
        Render(arcade);
    }

    private void Render(bool arcade) => modeLabel.text = arcade ? "Режим: Аркада" : "Режим: Симуляция";
    public void SetLaunchAvailable(bool available) => launchButton.interactable = available;
    private void Launch() => _bus.Publish(new LaunchFlightRequest());

    private void OnDestroy()
    {
        arcadeToggle.onValueChanged.RemoveListener(SelectMode);
        launchButton.onClick.RemoveListener(Launch);
    }
}
