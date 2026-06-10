using UnityEngine;

public class MissionController : MonoBehaviour  /// todo UI сделать или использовать сервис уведомлений INotificationService?
{
    [SerializeField] private Transform player;
    [SerializeField] private MissionWorldMarker worldMarker;

    private MissionRuntime _runtime;
    private MissionContext _context;

    public MissionState State =>
        _runtime?.State ?? MissionState.Inactive;

    private void Update()
    {
        if (_runtime == null)
            return;

        _context.ElapsedMissionTime += Time.deltaTime;

        _runtime.Tick();
    }

    public void StartMission(MissionRuntime runtime)
    {
        worldMarker.Initialize(player);

        _runtime = runtime;

        _context = new MissionContext
        {
            Player = player,
            Controller = this,
            ElapsedMissionTime = 0
        };

        runtime.Start(_context);
    }

    public void StopMission()
    {
        _runtime = null;
    }
}