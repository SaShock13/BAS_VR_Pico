using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Zenject;

public class AddressablesPrefabService
{
    private readonly DiContainer _container;

    // кеш загруженных prefab-ассетов
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _prefabHandles
        = new();

    public AddressablesPrefabService(DiContainer container)
    {
        _container = container;
    }

    /// <summary>
    /// Загрузка префаба из Addressables по строке адреса
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public async Task<GameObject> LoadPrefabAsync(string address)
    {
        if (_prefabHandles.TryGetValue(address, out var cachedHandle))
        {
            return cachedHandle.Result;
        }

        AsyncOperationHandle<GameObject> handle =
            Addressables.LoadAssetAsync<GameObject>(address);

        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[AddressablesPrefabService] Failed to load prefab: {address}");
            return null;
        }

        _prefabHandles[address] = handle;

        return handle.Result;
    }


    /// <summary>
    /// Создание префаба в сцене из Addressables по строке адреса . Через контейнер Zenject
    /// </summary>
    /// <param name="address"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public async Task<GameObject> InstantiateAsync(
        string address,
        Vector3 position,
        Quaternion rotation,
        Transform parent = null)
    {
        GameObject prefab = await LoadPrefabAsync(address);

        if (prefab == null)
        {
            Debug.LogError($"[AddressablesPrefabService] Prefab is null: {address}");
            return null;
        }

        GameObject instance = _container.InstantiatePrefab(
            prefab,
            position,
            rotation,
            parent);

        _container.InjectGameObject(instance);  // todo это нужно или нет?

        return instance;
    }


    ///TODO  как загружать много обьектов подряд???
    //public  GameObject Instantiate(
    //    string address,
    //    Vector3 position,
    //    Quaternion rotation,
    //    Transform parent = null)
    //{
    //    GameObject prefab = LoadPrefab(address);

    //    if (prefab == null)
    //    {
    //        Debug.LogError($"[AddressablesPrefabService] Prefab is null: {address}");
    //        return null;
    //    }

    //    GameObject instance = _container.InstantiatePrefab(
    //        prefab,
    //        position,
    //        rotation,
    //        parent);

    //    _container.InjectGameObject(instance);  // todo это нужно или нет?

    //    return instance;
    //}

   

    /// <summary>
    /// Удаление экземпляра объекта из сцены
    /// </summary>
    /// <param name="instance"></param>
    public void ReleaseInstance(GameObject instance)
    {
        if (instance == null)
            return;

        Object.Destroy(instance);
    }

    /// <summary>
    /// Выгрузка префаба из Addressables по строке адреса
    /// </summary>
    /// <param name="address"></param>
    public void ReleasePrefab(string address)
    {
        if (_prefabHandles.TryGetValue(address, out var handle))
        {
            Addressables.Release(handle);
            _prefabHandles.Remove(address);
        }
    }


    public bool IsLoaded(string address)
    {
        return _prefabHandles.ContainsKey(address);
    }

    /// <summary>
    /// Выгрузка всех префабов из Addressables 
    /// </summary>
    public void ReleaseAll()
    {
        foreach (var handle in _prefabHandles.Values)
        {
            Addressables.Release(handle);
        }

        _prefabHandles.Clear();
    }
}