#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NewPartsVerification
{
    [MenuItem("Tools/BAS VR/Verify New Parts Operations")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode verification only.");
        NewPartsMigration.Validate();
        var configs = AssetDatabase.FindAssets("t:PartConfig", new[] { NewPartsMigration.ConfigRoot })
            .Select(g => AssetDatabase.LoadAssetAtPath<PartConfig>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        var repository = new PartConfigRepository(configs);
        var scene = EditorSceneManager.NewPreviewScene();
        var bus = new VerificationBus();
        int verified = 0, attachments = 0;
        try
        {
            foreach (var config in configs)
            {
                if (repository.Get(config.PartId) != config) throw new Exception("Repository lookup failed.");
                var registry = new PartViewRegistry(bus);
                var view = Create(config, scene, bus);
                var copy = Create(config, scene, bus);
                registry.Register(view.InstanceId, view.gameObject);
                registry.Register(copy.InstanceId, copy.gameObject);
                if (view.InstanceId == copy.InstanceId || !registry.TryGet(copy.InstanceId, out var registered) || registered != copy)
                    throw new Exception("Instance registration failed: " + config.name);
                var visual = new PartVisualProperties { Color = Color.cyan, MaterialId = "PlasticMAt", Smoothness = 0.63f, Metallic = 0.27f };
                var domain = new PartDomainState(view.InstanceId, config.PartId, config.PartType, visual);
                view.transform.position = new Vector3(1, 2, 3);
                view.transform.rotation = Quaternion.Euler(10, 20, 30);
                view.transform.localScale *= 0.1375f;
                var saved = JsonUtility.FromJson<PartSaveData>(JsonUtility.ToJson(PartMapper.ToSaveData(domain, view.transform)));
                var restored = PartMapper.ToDomain(saved);
                if (restored.PartId != config.PartId || restored.InstanceId != view.InstanceId || restored.VisualProperties.Color != Color.cyan ||
                    Vector3.Distance(saved.Transform.LocalPosition, view.transform.position) > 0.0001f ||
                    !saved.Transform.HasLocalScale || Vector3.Distance(saved.Transform.LocalScale, view.transform.localScale) > 0.0001f)
                    throw new Exception("Save roundtrip failed: " + config.name);
                PartMapper.ApplySavedScale(saved.Transform, copy.transform);
                if (Vector3.Distance(copy.transform.localScale, view.transform.localScale) > 0.0001f)
                    throw new Exception("Scale restore failed: " + config.name);
                var legacy = JsonUtility.FromJson<TransformSaveData>("{\"LocalPosition\":{\"x\":0,\"y\":0,\"z\":0}}");
                var beforeLegacy = copy.transform.localScale;
                PartMapper.ApplySavedScale(legacy, copy.transform);
                if (copy.transform.localScale != beforeLegacy) throw new Exception("Legacy save changed prefab scale.");

                // Verify the actual property-block setters on every mesh, without loading a runtime material.
                view.ApplyPreviewSmoothness(0.63f);
                view.ApplyPreviewMetallness(0.27f);
                view.Highlight(true);
                view.Highlight(false);
                foreach (var renderer in view.GetComponentsInChildren<Renderer>())
                {
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    if (!Mathf.Approximately(block.GetFloat(ShaderIds.Smoothness), 0.63f) ||
                        !Mathf.Approximately(block.GetFloat(ShaderIds.Metallic), 0.27f))
                        throw new Exception("Multi-mesh appearance failed: " + config.name);
                }

                var parentConfig = configs.FirstOrDefault(c => c.Prefab.GetComponentsInChildren<SocketView>(true)
                    .Any(s => s.AllowedTypes.Contains(config.PartType)));
                if (parentConfig != null && config.PartType != PartType.Body)
                {
                    var parent = Create(parentConfig, scene, bus);
                    registry.Register(parent.InstanceId, parent.gameObject);
                    var socket = parent.GetSocketByType(config.PartType);
                    domain.AttachToPartSocket(parent.InstanceId, socket.SocketId);
                    view.AttachTo(socket.transform);
                    var installed = JsonUtility.FromJson<PartSaveData>(JsonUtility.ToJson(PartMapper.ToSaveData(domain, view.transform)));
                    var restoredInstalled = PartMapper.ToDomain(installed);
                    if (restoredInstalled.AttachedSocketId != socket.SocketId || restoredInstalled.AttachedPartInstanceId != parent.InstanceId)
                        throw new Exception("Attachment save failed: " + config.name);
                    view.Detach();
                    var resolvedParent = registry.TryGet(installed.AttachedPartId, out var foundParent) ? foundParent : null;
                    if (resolvedParent == null || resolvedParent.GetSocket(installed.AttachedSocketId) != socket)
                        throw new Exception("Saved socket cannot be resolved.");
                    // Apply the same parent/local transform path used by PartMapper, without async material loading in Edit Mode.
                    view.AttachTo(socket.transform);
                    view.transform.localPosition = installed.Transform.LocalPosition;
                    view.transform.localRotation = installed.Transform.LocalRotation;
                    if (!registry.TryGetAllChildrenIds(parent.InstanceId, out var children) || !children.Contains(view.InstanceId))
                        throw new Exception("Child discovery for delete/duplicate failed.");
                    view.Detach();
                    domain.Detach();
                    if (view.transform.parent != null || domain.AttachedSocketId != null) throw new Exception("Detach failed.");
                    Destroy(parent);
                    attachments++;
                }
                Destroy(view);
                Destroy(copy);
                registry.Clear();
                if (bus.GetSubscriberCount<PartSocketAttachedEvent>() != 0) throw new Exception("Socket subscription leaked: " + config.name);
                verified++;
            }
        }
        finally { bus.Dispose(); EditorSceneManager.ClosePreviewScene(scene); }
        Debug.Log($"NewParts verification passed: {verified} assets instantiated twice, registered, save-roundtripped and multi-mesh styled; {attachments} attachment/restore/detach paths; no socket subscription leaks. Play Mode not entered.");
    }

    private static DronePartView Create(PartConfig config, UnityEngine.SceneManagement.Scene scene, IEventBus bus)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(config.Prefab, scene);
        var view = go.AddComponent<DronePartView>();
        // MonoBehaviour runtime callbacks do not run automatically in Edit Mode.
        typeof(DronePartView).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
        view.Init(Guid.NewGuid().ToString(), bus);
        return view;
    }

    private static void Destroy(DronePartView view)
    {
        foreach (var socket in view.GetComponentsInChildren<SocketView>(true))
            typeof(SocketView).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(socket, null);
        UnityEngine.Object.DestroyImmediate(view.gameObject);
    }

    private sealed class VerificationBus : IEventBus
    {
        private readonly Dictionary<Type, int> counts = new Dictionary<Type, int>();
        public void Initialize() { }
        public IDisposable Subscribe<T>(Action<T> handler, EventPriority priority = EventPriority.Normal) where T : IAppEvent
        {
            var type = typeof(T);
            counts.TryGetValue(type, out int count);
            counts[type] = count + 1;
            return new Subscription(() => counts[type]--);
        }
        public IDisposable Subscribe<T>(Action<T> handler, Func<T, bool> filter, EventPriority priority = EventPriority.Normal) where T : IAppEvent => Subscribe(handler, priority);
        public void Publish<T>(T value) where T : IAppEvent { }
        public Task PublishAsync<T>(T value, CancellationToken token = default) where T : IAppEvent => Task.CompletedTask;
        public int GetSubscriberCount<T>() where T : IAppEvent => counts.TryGetValue(typeof(T), out int count) ? count : 0;
        public void Dispose() => counts.Clear();
    }

    private sealed class Subscription : IDisposable
    {
        private Action dispose;
        public Subscription(Action dispose) { this.dispose = dispose; }
        public void Dispose() { var action = dispose; dispose = null; action?.Invoke(); }
    }
}
#endif
