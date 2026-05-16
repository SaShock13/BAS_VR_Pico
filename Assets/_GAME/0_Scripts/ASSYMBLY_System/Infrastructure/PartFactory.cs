using System.Threading.Tasks;
using UnityEngine;
using Zenject;

public class PartFactory : IPartFactory
{
    private readonly DiContainer _container;
    private readonly AddressablesPrefabService _prefabs;

    public PartFactory(DiContainer container,AddressablesPrefabService prefabs)
    {
        _container = container;
        _prefabs = prefabs;
    }

    public GameObject Create(PartConfig config, Vector3 position, Quaternion rotation)
    {
        GameObject instance = _container.InstantiatePrefab(
            config.Prefab,
            position,
            rotation,
            null);

        return instance;
    }

    public async Task<GameObject> CreateAsync(PartConfig config, Vector3 position , Quaternion rotation)
    {
        
        return await CreateFromAddressables(config, position, rotation);
    }

    public async Task<GameObject> CreateFromAddressables(PartConfig config, Vector3 position, Quaternion rotation)
    {
        if (config == null)
        {
            Debug.LogError("[PartFactory] Config is null");
            return null;
        }

        if (string.IsNullOrEmpty(config.PrefabAddress))
        {
            Debug.LogError("[PartFactory] PrefabAddress is empty");
            return null;
        }

        GameObject instance =
            await _prefabs.InstantiateAsync(
                config.PrefabAddress,
                position,
                rotation,
                parent: null);

        return instance;
    }


}
