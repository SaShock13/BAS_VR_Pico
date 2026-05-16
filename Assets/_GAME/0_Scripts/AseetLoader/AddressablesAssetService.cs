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

    public async Task<T> Load<T>(string address)
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