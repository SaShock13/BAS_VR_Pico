using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public sealed class OpenFlightMissionSelection : IAppEvent
{
    public string EventId => nameof(OpenFlightMissionSelection);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string PanelId;
}

public sealed class ConfirmFlightMissionRequest : IAppEvent
{
    public string EventId => nameof(ConfirmFlightMissionRequest);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string MissionId;
}

public sealed class FlightEnvironmentSelectionView : MonoBehaviour
{
    [SerializeField] private string panelId;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private Button previous, next, confirm, cancel;
    private IEventBus _bus;
    private FlightEnvironmentCatalog _catalog;
    private IDisposable _subscription;
    private int _index;

    [Inject] public void Construct(IEventBus bus, FlightEnvironmentCatalog catalog)
    { _bus = bus; _catalog = catalog; }

    private void Start()
    {
        panel.SetActive(false);
        previous.onClick.AddListener(Previous);
        next.onClick.AddListener(Next);
        confirm.onClick.AddListener(Confirm);
        cancel.onClick.AddListener(Close);
        _subscription = _bus.Subscribe<OpenFlightMissionSelection>(e =>
        {
            if (string.Equals(panelId ?? string.Empty, e.PanelId ?? string.Empty))
                Open();
        });
    }

    private void Open()
    {
        var selected = _catalog.FindMission(FlightSession.Mission == null ? null : FlightSession.Mission.MissionId);
        _index = 0;
        if (selected != null)
            for (int i = 0; i < _catalog.Missions.Count; i++)
                if (_catalog.Missions[i] == selected) _index = i;
        Render();
        panel.SetActive(true);
    }

    private void Previous() { _index--; Render(); }
    private void Next() { _index++; Render(); }
    private void Render()
    {
        int count = _catalog.Missions.Count;
        _index = count == 0 ? 0 : (_index + count) % count;
        var entry = count == 0 ? null : _catalog.Missions[_index];
        var mission = entry?.mission;
        var environment = entry == null ? null : _catalog.Find(entry.environmentId);
        title.text = mission == null ? "Нет доступной миссии" : mission.MissionName + "  " + (_index + 1) + "/" + count;
        description.text = mission == null ? string.Empty :
            $"Локация: {environment?.title ?? "не назначена"}\n{mission.Briefing}";
        var requirements = mission?.Requirements?.Describe();
        if (!string.IsNullOrEmpty(requirements))
            description.text += $"\nТребования к дрону: {requirements}";
        else if (mission != null)
            description.text += "\nТребования к дрону: базовая комплектность";
        confirm.interactable = mission != null && environment != null;
        previous.interactable = next.interactable = count > 1;
    }

    private void Confirm()
    {
        if (!confirm.interactable) return;
        _bus.Publish(new ConfirmFlightMissionRequest { MissionId = _catalog.Missions[_index].mission.MissionId });
        Close();
    }
    public void Close() => panel.SetActive(false);
    private void OnDestroy()
    {
        _subscription?.Dispose();
        previous.onClick.RemoveListener(Previous);
        next.onClick.RemoveListener(Next);
        confirm.onClick.RemoveListener(Confirm);
        cancel.onClick.RemoveListener(Close);
    }
}
