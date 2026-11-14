using UnityEngine;

public class MissionController : MonoBehaviour  /// todo UI сделать или использовать сервис уведомлений INotificationService?
{
    [SerializeField] private Transform player;
    [SerializeField] private MissionWorldMarker worldMarker;

    private MissionRuntime _runtime;
    private MissionContext _context;
    private MissionRuntime _armedMission;
    private Transform _startZone;
    private float _startRadiusSquared;

    public void ArmFlightMission(MissionRuntime runtime, Transform drone, Transform startZone, float radius)
    {
        StopMission();
        player = drone;
        _armedMission = runtime;
        _startZone = startZone;
        _startRadiusSquared = radius * radius;
        if (worldMarker != null)
            worldMarker.Initialize(drone);
        MissionEvents.TargetChanged?.Invoke(startZone);
    }

    public MissionState State =>
        _runtime?.State ?? MissionState.Inactive;

    private void Update()
    {
        if (_armedMission != null && player != null && _startZone != null &&
            (player.position - _startZone.position).sqrMagnitude <= _startRadiusSquared)
            StartMission(_armedMission);

        if (_runtime == null || _runtime.State != MissionState.Running)
            return;

        _context.ElapsedMissionTime += Time.deltaTime;

        _runtime.Tick();
    }

    public void StartMission(MissionRuntime runtime)
    {
        StopMission();
        if (worldMarker != null)
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
        if (_runtime != null || _armedMission != null)
            MissionEvents.TargetChanged?.Invoke(null);
        _armedMission = null;
        _startZone = null;
        _runtime?.Stop();
        _runtime = null;
    }

    private void OnDisable() => StopMission();
}
