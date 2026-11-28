using UnityEngine;

[CreateAssetMenu(fileName = "PartConfig", menuName = "Scriptable Objects/PartConfig")]
public class PartConfig : ScriptableObject
{
    [System.Serializable]
    public struct InfoCharacteristic
    {
        public string Label;
        public string Value;
        public string Unit;
    }

    [Header("Карточка детали")]
    [Tooltip("Название в карточке. Если пусто, используется имя префаба.")]
    public string DisplayName;
    [TextArea(2, 6)] public string Description;
    [TextArea(2, 5)] public string AdditionalInfo;
    public InfoCharacteristic[] InfoCharacteristics = System.Array.Empty<InfoCharacteristic>();

    [Header("Identity")]
    public string PartId;
    [Tooltip("Previous IDs accepted when loading older assemblies and garage saves.")]
    public string[] LegacyPartIds = System.Array.Empty<string>();
    public PartType PartType;

    [Header("Visual")]
    [Tooltip("Keep prefab materials and disable visual customization.")]
    public bool HasConstantMaterial;

    public GameObject Prefab;

    public string PrefabAddress;

    [Header("Physics")]
    public float Mass;

    public Vector3 LocalCenterOfMass;

}
