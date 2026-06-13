using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesAssetService
{
    private readonly Dictionary<(string, Type),
        AsyncOperationHandle>
        _handles = new();

    /// <summary>
    ///  Загрузка ассета из Addressables по строке адреса
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="address"></param>
    /// <returns></returns>

    public async Task<T> Load<T>(string address)   // загрузка по address todo лучше мигрировать на AssetReference-only путь 
        where T : UnityEngine.Object
    {
        var key = (address, typeof(T));

        // already loaded
        if (_handles.TryGetValue(
            key,
            out var cachedHandle))
        {
            return cachedHandle.Result as T;
        }

        AsyncOperationHandle<T> handle =
            Addressables.LoadAssetAsync<T>(address);

        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError(
                $"Failed load: {address}");

            return null;
        }

        _handles[key] = handle;

        return handle.Result;
    }


    private readonly Dictionary<(string, Type), Task<UnityEngine.Object>>
        _loadingTasks = new();

    public async Task<T> Load<T>(AssetReference reference)
        where T : UnityEngine.Object
    {
        var key = (reference.AssetGUID, typeof(T));

        // Уже загружено
        if (_handles.TryGetValue(key, out var cachedHandle))
        {
            return cachedHandle.Result as T;
        }

        // Уже грузится
        if (_loadingTasks.TryGetValue(key, out var loadingTask))
        {
            return await loadingTask as T;
        }

        // Запускаем новую загрузку
        var task = LoadInternal<T>(reference, key);

        _loadingTasks[key] = task;

        try
        {
            return await task as T;
        }
        finally
        {
            _loadingTasks.Remove(key);
        }
    }

    private async Task<UnityEngine.Object> LoadInternal<T>(
        AssetReference reference,
        (string, Type) key)
        where T : UnityEngine.Object
    {
        AsyncOperationHandle<T> handle =
            Addressables.LoadAssetAsync<T>(
                reference.RuntimeKey);

        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError(
                $"Failed load: {reference.AssetGUID}");

            return null;
        }

        _handles[key] = handle;

        return handle.Result;
    }


    //public async Task<T> Load<T>(AssetReference reference)  /// Загрузка по AssetReference
    //where T : UnityEngine.Object
    //{
    //    var key = (reference.AssetGUID, typeof(T));

    //    if (_handles.TryGetValue(key, out var cached))
    //        return cached.Result as T;

    //    var handle = Addressables.LoadAssetAsync<T>(reference.RuntimeKey);

    //    await handle.Task;

    //    if (handle.Status != AsyncOperationStatus.Succeeded)
    //    {
    //        Debug.LogError($"Failed load: {reference.AssetGUID}");
    //        return null;
    //    }

    //    _handles[key] = handle;

    //    return handle.Result;
    //}

    public bool IsLoaded<T>(string address)
        where T : UnityEngine.Object
    {
        return _handles.ContainsKey(
            (address, typeof(T)));
    }

    /// <summary>
    /// Выгрузка ассета из Addressables по строке адреса
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="address"></param>
    public void Release<T>(string address)
        where T : UnityEngine.Object
    {
        var key = (address, typeof(T));

        if (_handles.TryGetValue(
            key,
            out var handle))
        {
            Addressables.Release(handle);

            _handles.Remove(key);
        }
    }

    /// <summary>
    /// Выгрузка всех ассетов из Addressables
    /// </summary>

    public void ReleaseAll()
    {
        foreach (var pair in _handles)
        {
            Addressables.Release(pair.Value);
        }

        _handles.Clear();
    }
}