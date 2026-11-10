using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public sealed class OpenFlightEnvironmentSelection : IAppEvent
{
    public string EventId => nameof(OpenFlightEnvironmentSelection);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed class ConfirmFlightEnvironmentRequest : IAppEvent
{
    public string EventId => nameof(ConfirmFlightEnvironmentRequest);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EnvironmentId;
}

public sealed class FlightEnvironmentSelectionView : MonoBehaviour
{
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
        _subscription = _bus.Subscribe<OpenFlightEnvironmentSelection>(_ => Open());
    }

    private void Open()
    {
        var selected = _catalog.Find(FlightSession.EnvironmentId);
        _index = 0;
        if (selected != null)
            for (int i = 0; i < _catalog.Environments.Count; i++)
                if (_catalog.Environments[i] == selected) _index = i;
        Render();
        panel.SetActive(true);
    }

    private void Previous() { _index--; Render(); }
    private void Next() { _index++; Render(); }
    private void Render()
    {
        int count = _catalog.Environments.Count;
        _index = count == 0 ? 0 : (_index + count) % count;
        var entry = count == 0 ? null : _catalog.Environments[_index];
        title.text = entry == null ? "Нет доступных локаций" : entry.title + "  " + (_index + 1) + "/" + count;
        description.text = entry == null ? "Добавьте окружения в FlightEnvironmentCatalog" : entry.description;
        confirm.interactable = entry != null;
        previous.interactable = next.interactable = count > 1;
    }

    private void Confirm()
    {
        if (_catalog.Environments.Count == 0) return;
        _bus.Publish(new ConfirmFlightEnvironmentRequest { EnvironmentId = _catalog.Environments[_index].id });
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
