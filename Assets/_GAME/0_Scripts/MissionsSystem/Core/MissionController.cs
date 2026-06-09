using UnityEngine;

public class MissionController : MonoBehaviour
{
    [SerializeField] private Transform player;

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