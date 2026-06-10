using UnityEngine;
using Zenject;

public sealed class TabletMissionPresenter : MonoBehaviour
{
    [SerializeField]
    private TabletMissionView _view;

    private ITabletService _tabletService;
    [SerializeField]
    private GameObject _content;

    [Inject]
    public void Construct(
        ITabletService tabletService)
    {
        _tabletService = tabletService;
    }

    private void Awake()
    {
        _tabletService.MissionChanged += OnMissionChanged;

        OnMissionChanged(
            _tabletService.CurrentMissionInfo);
    }

    private void OnDestroy()
    {
        _tabletService.MissionChanged -= OnMissionChanged;
    }

    private void OnMissionChanged(
        TabletMissionState state)
    {
        _view.UpdateMission(state);
    }

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.T))
        {
            _content.SetActive(!_content.activeInHierarchy);
        }

    }
}