using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(menuName = "Drone/Visual/Material Definition")]
public class MaterialDefinition : ScriptableObject
{
    public string Id;

    public string DisplayName;

    public AssetReference MaterialReference; // Попробовать , если удобно


    public string MaterialAddress;
}