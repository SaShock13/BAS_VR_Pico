#if UNITY_EDITOR
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Zenject;

public static class MenuPartCreationVerification
{
    [MenuItem("Tools/BAS VR/Verify Menu Part Requests")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode check only.");
        var root = PrefabUtility.LoadPrefabContents("Assets/_GAME/Prefabs/UI/AssemblyMenu.prefab");
        int checkedButtons = 0;
        try
        {
            var bus = new RecordingBus();
            var container = new DiContainer();
            container.Bind<IEventBus>().FromInstance(bus);
            var publisher = root.GetComponent<MenuPartCreationView>();
            container.Inject(publisher);
            var menu = root.GetComponent<AssemblyMenuView>();
            var rows = root.GetComponentsInChildren<ObjectRowManager>(true);
            for (int category = 0; category < rows.Length; category++)
            {
                menu.SelectCategory(category);
                var row = rows[category];
                for (int page = 0; page < row.GetTotalPages(); page++)
                {
                    row.GoToPage(page);
                    foreach (var item in row.allObjects)
                    {
                        if (!item.activeInHierarchy) continue;
                        var button = item.GetComponent<Button>();
                        if (button == null || button.GetComponentInParent<Canvas>() == null ||
                            button.onClick.GetPersistentEventCount() != 1 || button.onClick.GetPersistentTarget(0) != publisher)
                            throw new Exception("Invalid XR UI callback: " + item.name);
                        button.onClick.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
                        int before = bus.Count;
                        button.onClick.Invoke();
                        var config = item.GetComponent<PartViewConfigLink>().Config;
                        var cell = (RectTransform)item.transform;
                        float cellHeight = cell.TransformVector(Vector3.up * cell.rect.height).magnitude;
                        if (bus.Count != before + 1 || bus.Last.PartId != config.PartId ||
                            bus.Last.WorldPosition != item.transform.position || bus.Last.Timestamp.Kind != DateTimeKind.Utc ||
                            bus.Last.WorldScale.HasValue ||
                            !bus.Last.MenuCellHeight.HasValue || !Mathf.Approximately(bus.Last.MenuCellHeight.Value, cellHeight))
                            throw new Exception("Incorrect creation event: " + item.name);

                        item.SetActive(false);
                        button.onClick.Invoke();
                        item.SetActive(true);
                        if (bus.Count != before + 1) throw new Exception("Hidden preview sent a request.");
                        button.interactable = false;
                        button.onClick.Invoke();
                        button.interactable = true;
                        if (bus.Count != before + 1) throw new Exception("Disabled button sent a request.");
                        checkedButtons++;
                    }
                }
            }
            int baseline = bus.Count;
            publisher.RequestCreatePart(-1);
            publisher.RequestCreatePart(int.MaxValue);
            root.SetActive(false);
            publisher.RequestCreatePart(0);
            root.SetActive(true);
            if (bus.Count != baseline) throw new Exception("Invalid request was published.");

            menu.SelectCategory(0);
            rows[0].GoToPage(0);
            var spawn = new GameObject("VerificationSpawn").transform;
            spawn.SetParent(root.transform, false);
            spawn.position = new Vector3(1, 2, 3);
            var data = new SerializedObject(publisher);
            data.FindProperty("spawnPoint").objectReferenceValue = spawn;
            data.ApplyModifiedPropertiesWithoutUndo();
            var first = rows[0].allObjects[0].GetComponent<Button>();
            first.onClick.Invoke();
            if (bus.Count != baseline + 1 || bus.Last.WorldPosition != spawn.position)
                throw new Exception("Spawn point override failed.");
            if (checkedButtons != root.GetComponentsInChildren<PartViewConfigLink>(true).Length)
                throw new Exception("Unreachable menu entries.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Debug.Log("Verified " + checkedButtons + " menu buttons: one correct creation request per click; hidden/disabled/invalid requests ignored; spawn point and Zenject injection work. Play Mode not entered.");
    }

    private sealed class RecordingBus : IEventBus
    {
        public int Count;
        public Clean_CreatePartRequestEvent Last;
        public void Initialize() { }
        public void Dispose() { }
        public int GetSubscriberCount<T>() where T : IAppEvent => 0;
        public IDisposable Subscribe<T>(Action<T> handler, EventPriority priority = EventPriority.Normal) where T : IAppEvent => throw new NotSupportedException();
        public IDisposable Subscribe<T>(Action<T> handler, Func<T, bool> filter, EventPriority priority = EventPriority.Normal) where T : IAppEvent => throw new NotSupportedException();
        public void Publish<T>(T value) where T : IAppEvent
        {
            if (value is Clean_CreatePartRequestEvent request) { Last = request; Count++; }
        }
        public Task PublishAsync<T>(T value, CancellationToken token = default) where T : IAppEvent
        {
            Publish(value);
            return Task.CompletedTask;
        }
    }
}
#endif
