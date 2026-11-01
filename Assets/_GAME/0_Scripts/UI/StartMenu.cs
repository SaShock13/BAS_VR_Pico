using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class StartMenu : MonoBehaviour
{
    [SerializeField] private TMP_Text startBtnText;
    [SerializeField] private Button startBtn;
    private IEventBus _eventBus;
    private IDisposable _subscription;

    [Inject]
    public void Construct(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    private void Start()
    {
        _subscription = _eventBus.Subscribe<MenuNavigationStatusEvent>(OnStatusChanged);
        SetButton(false);
    }

    public void StartConstructor()
    {
        if (_eventBus == null || !startBtn.interactable) return;
        SetButton(true);
        _eventBus.Publish(new RequestStartConstructorEvent());
    }

    private void OnStatusChanged(MenuNavigationStatusEvent status)
    {
        SetButton(status.IsLoading);
        if (!string.IsNullOrEmpty(status.Error))
            startBtnText.text = "ОШИБКА ЗАГРУЗКИ. ПОВТОРИТЬ";
    }

    private void SetButton(bool loading)
    {
        startBtn.interactable = !loading;
        startBtnText.text = loading ? "ЗАГРУЗКА КОНСТРУКТОРА…" : "НАЧАТЬ КОНСТРУИРОВАНИЕ";
    }

    private void OnDestroy()
    {
        _subscription?.Dispose();
    }
}
