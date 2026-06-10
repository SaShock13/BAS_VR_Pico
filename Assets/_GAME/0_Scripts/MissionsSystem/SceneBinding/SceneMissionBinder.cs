using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static UnityEngine.Rendering.GPUSort;

public class SceneMissionBinder : MonoBehaviour
{
    //[Header("Mission Points")]
    //public Transform PickupPoint;

    //public Transform DeliveryPoint;

    //[Header("Mission Objects")]
    //public Cargo Cargo;

    [Inject] public IEventBus EventBus;
    [Inject] public Clean_AssemblySystem _AssemblySystem;

    [SerializeField]
    private List<MissionPoint> missionPoints;

    public Cargo Cargo;

    private Dictionary<MissionPointId, MissionPoint> _points;

    private void Awake()
    {
        _points = new Dictionary<MissionPointId, MissionPoint>();

        foreach (var point in missionPoints)
        {
            _points[point.Id] = point;
        }
    }

    public MissionPoint GetPoint(MissionPointId id)
    {
        if (_points.TryGetValue(id, out var point))
            return point;

        Debug.LogError($"Mission Point '{id}' not found");

        return null;
    }
}