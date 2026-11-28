#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class CoverHandleRenderingVerification
{
    [MenuItem("Tools/BAS VR/Verify Transparent Cover Handles")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        var shader = Shader.Find("BAS_VR/Cover Handle Overlay");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Handle shader compilation failed.");
        var mesh = new Mesh { name = "HandleDepthTestQuad" };
        mesh.vertices = new[] { new Vector3(-.8f, -.8f), new Vector3(-.8f, .8f), new Vector3(.8f, .8f), new Vector3(.8f, -.8f) };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2 };
        var part = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        part.SetColor("_BaseColor", Color.blue);
        var handle = new Material(shader);
        handle.SetColor("_BaseColor", new Color(1, 0, 0, .55f));
        // Use the same vertex path so this low-level no-camera fixture does not depend on
        // camera-managed URP constant buffers. LEqual is the real menu shaders' depth rule.
        var menu = new Material(shader);
        menu.SetColor("_BaseColor", Color.green);
        menu.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
        var target = new RenderTexture(16, 16, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        var pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
        var previous = RenderTexture.active;
        try
        {
            target.Create();
            Color Render(float? menuZ)
            {
                using var commands = new CommandBuffer { name = "Cover handle depth regression" };
                commands.SetRenderTarget(target);
                commands.ClearRenderTarget(true, true, Color.black);
                var projection = GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(-1, 1, -1, 1, .1f, 10), true);
                // No Camera is setting up reversed-depth render state for this command buffer.
                // Use conventional near=0/far=1 depth with the explicit LEqual comparison.
                if (SystemInfo.usesReversedZBuffer) projection.SetRow(2, projection.GetRow(3) - projection.GetRow(2));
                commands.SetViewProjectionMatrices(Matrix4x4.identity, projection);
                commands.DrawMesh(mesh, Matrix4x4.Translate(new Vector3(0, 0, -1)), part, 0, 0);
                commands.DrawMesh(mesh, Matrix4x4.Translate(new Vector3(0, 0, -2)), handle, 0, 0);
                if (menuZ.HasValue) commands.DrawMesh(mesh, Matrix4x4.Translate(new Vector3(0, 0, menuZ.Value)), menu, 0, 0);
                Graphics.ExecuteCommandBuffer(commands);
                RenderTexture.active = target;
                pixel.ReadPixels(new Rect(8, 8, 1, 1), 0, 0);
                pixel.Apply();
                return pixel.GetPixel(0, 0);
            }
            var overlay = Render(null);
            Require(overlay.r > .45f && overlay.r < .65f && overlay.b > .35f,
                "Handle is occluded by the nearer part or does not alpha-blend: " + overlay);
            var near = Render(-.5f);
            Require(near.g > .9f && near.r < .1f, "Near menu does not cover the handle: " + near);
            var far = Render(-3);
            Require(far.r > .45f && far.g < .1f, "Far menu incorrectly covers the handle: " + far);
        }
        finally
        {
            RenderTexture.active = previous;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(pixel);
            UnityEngine.Object.DestroyImmediate(mesh);
            UnityEngine.Object.DestroyImmediate(part);
            UnityEngine.Object.DestroyImmediate(handle);
            UnityEngine.Object.DestroyImmediate(menu);
        }
        Debug.Log("PASS: GPU pixels verify 55% handle transparency over nearer parts; near menu covers handles; far menu does not. No camera added.");
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
#endif
