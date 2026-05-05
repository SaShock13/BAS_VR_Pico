using UnityEngine;

[CreateAssetMenu(fileName = "PartConfig", menuName = "Scriptable Objects/PartConfig")]
public class PartConfig : ScriptableObject
{
    public string PartId;
    public GameObject Prefab;
    public float Mass;
}
