#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PartHoverDepthVerification
{
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode check only");
        var scene = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(128, 128, 24);
        var pixels = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        var blue = new Texture2D(1, 1); blue.SetPixel(0, 0, Color.blue); blue.Apply();
        var screenMaterial = new Material(Shader.Find("BAS_VR/Alpha Screen"));
        var previousTarget = RenderTexture.active;
        try
        {
            var cameraObject = new GameObject("Depth check camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false; camera.scene = scene;
            camera.orthographic = true; camera.orthographicSize = .3f; camera.nearClipPlane = .01f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.targetTexture = target; target.Create();
            var card = new GameObject("Depth check card", typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer), typeof(Image));
            SceneManager.MoveGameObjectToScene(card, scene);
            card.transform.position = Vector3.forward;
            card.transform.localScale = Vector3.one * .001f;
            ((RectTransform)card.transform).sizeDelta = new Vector2(420, 300);
            var canvas = card.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
            var image = card.GetComponent<Image>(); image.raycastTarget = false;
            image.color = new Color(.6f, .2f, .05f, 1);
            image.material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_GAME/Prefabs/UI/PartHoverInfoDepth.mat");
            var screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            SceneManager.MoveGameObjectToScene(screen, scene);
            screen.transform.position = Vector3.forward * 2;
            screen.transform.localScale = Vector3.one * 2;
            screenMaterial.SetTexture("_BaseMap", blue);
            var renderer = screen.GetComponent<MeshRenderer>(); renderer.sharedMaterial = screenMaterial; renderer.sortingOrder = 1;
            Color Render()
            {
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); pixels.Apply();
                return pixels.GetPixel(64, 64);
            }
            var farColor = Render();
            if (farColor.r <= farColor.b) throw new Exception("Farther curved-screen material occludes card: " + farColor);
            screen.transform.position = Vector3.forward * .5f;
            var nearColor = Render();
            if (nearColor.b <= nearColor.r) throw new Exception("Nearer screen fails to occlude card: " + nearColor);
            Debug.Log("PASS card depth: farther screen hidden, nearer screen visible.");
        }
        finally
        {
            RenderTexture.active = previousTarget;
            target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(blue); UnityEngine.Object.DestroyImmediate(screenMaterial);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
#endif
