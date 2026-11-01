using UnityEngine;

[CreateAssetMenu(fileName = "PartConfig", menuName = "Scriptable Objects/PartConfig")]
public class PartConfig : ScriptableObject
{
    [Header("Identity")]
    public string PartId;
    [Tooltip("Previous IDs accepted when loading older assemblies and garage saves.")]
    public string[] LegacyPartIds = System.Array.Empty<string>();
    public PartType PartType;

    [Header("Visual")]
    public GameObject Prefab;

    public string PrefabAddress;

    [Header("Physics")]
    public float Mass;

    public Vector3 LocalCenterOfMass;

}
