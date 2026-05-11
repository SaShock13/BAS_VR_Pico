using UnityEngine;

[CreateAssetMenu(fileName = "PartConfig", menuName = "Scriptable Objects/PartConfig")]
public class PartConfig : ScriptableObject
{
    [Header("Identity")]
    public string PartId;
    public PartType PartType;

    [Header("Visual")]
    public GameObject Prefab;

    [Header("Physics")]
    public float Mass;

    public Vector3 LocalCenterOfMass;

}
