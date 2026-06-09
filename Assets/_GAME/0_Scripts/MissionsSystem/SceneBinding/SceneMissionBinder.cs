using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.GPUSort;

public class SceneMissionBinder : MonoBehaviour
{
    //[Header("Mission Points")]
    //public Transform PickupPoint;

    //public Transform DeliveryPoint;

    //[Header("Mission Objects")]
    //public Cargo Cargo;


    [SerializeField]
    private List<MissionPoint> missionPoints;

    public Cargo Cargo;

    private Dictionary<string, MissionPoint> _points;

    private void Awake()
    {
        _points = new Dictionary<string, MissionPoint>();

        foreach (var point in missionPoints)
        {
            _points[point.Id] = point;
        }
    }

    public MissionPoint GetPoint(string id)
    {
        if (_points.TryGetValue(id, out var point))
            return point;

        Debug.LogError($"Mission Point '{id}' not found");

        return null;
    }
}