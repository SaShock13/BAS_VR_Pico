using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public static class SFXButtonVerification
{
    [MenuItem("Tools/SFX/Verify Button Event Chain")]
    public static void Verify()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Run in Edit Mode.");
        var container = new DiContainer();
        var bus = new EventBus(new ConsoleLogger(), container);
        var player = new RecordingPlayer();
        var service = new SFXService(player, bus);
        GameObject root = null;
        try
        {
            container.Bind<IEventBus>().FromInstance(bus);
            bus.Initialize();
            service.Initialize();
            root = new GameObject("Temporary SFX verification", typeof(RectTransform), typeof(Button));
            var publisher = root.AddComponent<SFXButtonSound>();
            container.Inject(publisher);
            var button = root.GetComponent<Button>();
            button.onClick.AddListener(publisher.Play);
            button.onClick.Invoke();
            if (player.Count != 1 || player.Last != SFXId.UIClick)
                throw new Exception("Button event did not reach SFXService.");
            service.Dispose();
            button.onClick.Invoke();
            if (player.Count != 1) throw new Exception("SFXService did not unsubscribe.");
            Debug.Log("PASS: Button -> PlaySFXRequest(UIClick) -> EventBus -> SFXService -> ISFXPlayer; unsubscribe verified.");
        }
        finally
        {
            service.Dispose();
            bus.Dispose();
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private sealed class RecordingPlayer : ISFXPlayer
    {
        public int Count;
        public SFXId Last;
        public SFXHandle Play(SFXId id, Vector3 position = default, float volume = 1f, Transform followTarget = null)
        {
            Count++;
            Last = id;
            return default;
        }
        public bool Stop(SFXHandle handle) => false;
        public void StopAll(SFXId id) { }
        public bool SetVolume(SFXHandle handle, float volume) => false;
    }
}
