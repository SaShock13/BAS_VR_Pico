using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class StartMenu : MonoBehaviour
{
    [SerializeField] private TMP_Text startBtnText;
    [SerializeField] private Button startBtn;
    [SerializeField] private TMP_InputField projectName;
    [SerializeField] private Button showSaves;
    [SerializeField] private Button previous;
    [SerializeField] private Button next;
    [SerializeField] private Button[] savedRows;
    [SerializeField] private TMP_Text statusText;
    private IEventBus _eventBus;
    private IDisposable _subscription, _listSubscription;
    private List<AssemblyProjectData> _projects = new();
    private int _page;
    private bool _loading;
    [Inject] public void Construct(IEventBus eventBus) { _eventBus = eventBus; }
    private void Start()
    {
        _subscription = _eventBus.Subscribe<MenuNavigationStatusEvent>(OnStatusChanged);
        _listSubscription = _eventBus.Subscribe<AssemblyProjectListEvent>(OnList);
        showSaves.onClick.AddListener(Refresh);
        previous.onClick.AddListener(() => { _page = Mathf.Max(0, _page - 1); Render(); });
        next.onClick.AddListener(() => { _page++; Render(); });
        for (int i = 0; i < savedRows.Length; i++)
        {
            int row = i;
            savedRows[i].onClick.AddListener(() => LoadRow(row));
        }
        SetButton(false);
        Refresh();
    }
    public void StartConstructor()
    {
        if (_eventBus == null || _loading) return;
        _eventBus.Publish(new AssemblyProjectRequest { Operation = "new", Name = projectName.text });
    }
    private void Refresh() { if (!_loading) _eventBus.Publish(new AssemblyProjectRequest { Operation = "list" }); }
    private void OnList(AssemblyProjectListEvent data)
    {
        if (!string.IsNullOrEmpty(data.Error)) { statusText.text = data.Error; SetButton(false); return; }
        _projects = data.Projects;
        _page = 0;
        statusText.text = _projects.Count == 0 ? "Сохранённых сборок пока нет" : "Выберите сборку для загрузки";
        Render();
    }
    private void LoadRow(int row)
    {
        int index = _page * savedRows.Length + row;
        if (_loading || index >= _projects.Count) return;
        _eventBus.Publish(new AssemblyProjectRequest { Operation = "load", Id = _projects[index].Id });
    }
    private void Render()
    {
        for (int i = 0; i < savedRows.Length; i++)
        {
            int index = _page * savedRows.Length + i;
            savedRows[i].gameObject.SetActive(index < _projects.Count);
            savedRows[i].interactable = !_loading;
            if (index < _projects.Count)
            {
                var p = _projects[index];
                savedRows[i].GetComponentInChildren<TMP_Text>().text = p.Name + "  |  " + new DateTime(p.UpdatedAtTicks, DateTimeKind.Utc).ToLocalTime().ToString("dd.MM.yyyy HH:mm");
            }
        }
        previous.interactable = !_loading && _page > 0;
        next.interactable = !_loading && (_page + 1) * savedRows.Length < _projects.Count;
    }
    private void OnStatusChanged(MenuNavigationStatusEvent status)
    {
        SetButton(status.IsLoading);
        if (!string.IsNullOrEmpty(status.Error)) statusText.text = status.Error;
    }
    private void SetButton(bool loading)
    {
        _loading = loading;
        startBtn.interactable = !loading;
        showSaves.interactable = !loading;
        projectName.interactable = !loading;
        startBtnText.text = loading ? "ЗАГРУЗКА…" : "НОВАЯ СБОРКА";
        Render();
    }
    private void OnDestroy() { _subscription?.Dispose(); _listSubscription?.Dispose(); }
}
