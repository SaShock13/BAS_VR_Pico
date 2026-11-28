#if UNITY_EDITOR
using System;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public static class GaragePortalVerification
{
    private sealed class Logger : IAppLogger
    {
        public bool IsDebugEnabled => false;
        public void Initialize() { }
        public void Log(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
    }

    [MenuItem("Tools/BAS VR/Verify Garage Portals")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode only.");
        var original = SceneManager.GetActiveScene();
        var scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scratch);
        try
        {
            using (var bus = new EventBus(new Logger(), new DiContainer()))
            {
                bus.Initialize();
                int requests = 0;
                using (bus.Subscribe<GaragePortalRequest>(request => requests++))
                {
                    var portal = new GameObject("Test portal", typeof(GaragePortalTrigger));
                    var trigger = portal.GetComponent<GaragePortalTrigger>();
                    trigger.Construct(bus);
                    var part = new GameObject("Part", typeof(BoxCollider));
                    portal.SendMessage("OnTriggerEnter", part.GetComponent<Collider>());
                    var player = new GameObject("Player", typeof(CharacterController), typeof(XROrigin));
                    var hand = new GameObject("Hand", typeof(BoxCollider));
                    hand.transform.SetParent(player.transform);
                    portal.SendMessage("OnTriggerEnter", hand.GetComponent<Collider>());
                    Check(requests == 0, "Hands and parts must be ignored.");
                    portal.SendMessage("OnTriggerEnter", player.GetComponent<CharacterController>());
                    Check(requests == 1, "Player body must publish a request.");
                    trigger.enabled = false;
                    portal.SendMessage("OnTriggerEnter", player.GetComponent<CharacterController>());
                    Check(requests == 1, "Disabled portal must be ignored.");
                }
            }
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(scratch, true);
        }
        foreach (var name in new[] { "ConstructorScene", "GarageScene" })
        {
            var path = "Assets/Scenes/" + name + ".unity";
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var portal = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GaragePortalTrigger>(true)).Single();
                var box = portal.GetComponent<BoxCollider>();
                var body = portal.GetComponent<Rigidbody>();
                Check(box.isTrigger && body.isKinematic && !body.useGravity, "Invalid portal physics.");
                var player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CharacterController>(true)).Single();
                Check(!Physics.GetIgnoreLayerCollision(portal.gameObject.layer, player.gameObject.layer), "Player/portal layers cannot interact.");
                Check(!box.bounds.Intersects(player.bounds), "Portal overlaps player spawn.");
                bool back = new SerializedObject(portal).FindProperty("returnToAssembly").boolValue;
                Check(back == (name == "GarageScene"), "Wrong portal direction.");
                Check(EditorBuildSettings.scenes.Any(entry => entry.enabled && entry.path == path), "Scene is not enabled in build.");
                Debug.Log("PASS: " + name + " portal physics, direction, spawn separation and Build Settings.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        SceneManager.SetActiveScene(original);
        Debug.Log("PASS: portal player filtering. No Play Mode or scene navigation invoked.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
#endif
