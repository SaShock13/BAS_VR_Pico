using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public sealed class OpenAssemblyEnvironmentSelection : IAppEvent
{
    public string EventId => nameof(OpenAssemblyEnvironmentSelection);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed class ConfirmAssemblyEnvironmentRequest : IAppEvent
{
    public string EventId => nameof(ConfirmAssemblyEnvironmentRequest);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EnvironmentId;
}

public sealed class AssemblyEnvironmentSelectionView : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text title, description;
    [SerializeField] private Button previous, next, confirm, cancel;
    [SerializeField] private CanvasGroup underlyingMenu;
    private IEventBus _bus;
    private AssemblyEnvironmentCatalog _catalog;
    private IDisposable _openSubscription, _statusSubscription;
    private int _index;
    private bool _loading;

    [Inject] public void Construct(IEventBus bus, AssemblyEnvironmentCatalog catalog)
    { _bus = bus; _catalog = catalog; }

    private void Start()
    {
        panel.SetActive(false);
        previous.onClick.AddListener(Previous);
        next.onClick.AddListener(Next);
        confirm.onClick.AddListener(Confirm);
        cancel.onClick.AddListener(Close);
        _openSubscription = _bus.Subscribe<OpenAssemblyEnvironmentSelection>(_ => Open());
        _statusSubscription = _bus.Subscribe<MenuNavigationStatusEvent>(OnStatus);
    }

    private void Open()
    {
        _index = 0;
        for (int i = 0; i < _catalog.Environments.Count; i++)
            if (_catalog.Environments[i]?.id == AssemblyEnvironmentSession.EnvironmentId) _index = i;
        Render();
        if (underlyingMenu != null)
        {
            underlyingMenu.interactable = false;
            underlyingMenu.blocksRaycasts = false;
        }
        panel.SetActive(true);
    }
    private void Previous() { if (!_loading) { _index--; Render(); } }
    private void Next() { if (!_loading) { _index++; Render(); } }
    private void Render()
    {
        int count = _catalog.Environments.Count;
        _index = count == 0 ? 0 : (_index + count) % count;
        var entry = count == 0 ? null : _catalog.Environments[_index];
        title.text = entry == null ? "Нет локаций сборки" : entry.title;
        description.text = entry == null ? "Настройте каталог локаций сборки" : entry.description;
        confirm.interactable = !_loading && entry != null;
        previous.interactable = next.interactable = !_loading && count > 1;
        cancel.interactable = !_loading;
    }
    private void Confirm()
    {
        if (_loading || _catalog.Environments.Count == 0) return;
        var entry = _catalog.Environments[_index];
        if (entry != null) _bus.Publish(new ConfirmAssemblyEnvironmentRequest { EnvironmentId = entry.id });
    }
    private void OnStatus(MenuNavigationStatusEvent status)
    {
        _loading = status.IsLoading;
        Render();
        if (!string.IsNullOrEmpty(status.Error)) description.text = status.Error;
    }
    private void Close()
    {
        if (_loading) return;
        panel.SetActive(false);
        if (underlyingMenu != null)
        {
            underlyingMenu.interactable = true;
            underlyingMenu.blocksRaycasts = true;
        }
        // Release AssemblyProjectMenuService's pending-start guard after cancellation.
        _bus.Publish(new MenuNavigationStatusEvent { IsLoading = false });
    }
    private void OnDestroy()
    {
        _openSubscription?.Dispose();
        _statusSubscription?.Dispose();
        previous.onClick.RemoveListener(Previous);
        next.onClick.RemoveListener(Next);
        confirm.onClick.RemoveListener(Confirm);
        cancel.onClick.RemoveListener(Close);
    }
}
