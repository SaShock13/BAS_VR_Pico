using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class SceneMissionBinder : MonoBehaviour
{
    //[Header("Mission Points")]
    //public Transform PickupPoint;

    //public Transform DeliveryPoint;

    //[Header("Mission Objects")]
    //public Cargo Cargo;

    [Inject] public IEventBus EventBus;
    [Inject] public Clean_AssemblySystem _AssemblySystem;
    [Inject] public IHintScenarioController _hintScenario;

    [SerializeField]
    private List<MissionPoint> missionPoints = new();

    public Cargo Cargo;
    [SerializeField] private Transform flightLaunchTarget;
    public Transform FlightLaunchTarget => flightLaunchTarget;

    private Dictionary<MissionPointId, MissionPoint> _points;

    private void Awake()
    {
        _points = new Dictionary<MissionPointId, MissionPoint>();

        foreach (var point in missionPoints)
        {
            if (point == null)
                continue;
            _points[point.Id] = point;
        }
    }

    public void BindFlightEnvironment(FlightEnvironmentContext environment)
    {
        _points.Clear();
        foreach (var point in environment.missionPoints)
            if (point != null) _points.Add(point.Id, point);
        Cargo = environment.cargo;
    }

    public MissionPoint GetPoint(MissionPointId id)
    {
        if (_points.TryGetValue(id, out var point))
            return point;

        Debug.LogError($"Mission Point '{id}' not found");

        return null;
    }
}
