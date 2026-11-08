#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

public static class ControllerButtonHintMeshBuilder
{
    // Extract once in the Editor. Never bake/read controller meshes in the player.
    public static Mesh Build(GameObject controllerPrefab, string boneName, string outputPath)
    {
        var skin = System.Array.Find(controllerPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true),
            renderer => System.Array.Exists(renderer.bones, bone => bone != null && bone.name == boneName));
        if (skin == null)
            throw new System.InvalidOperationException("Controller has no skinned mesh.");
        int boneIndex = System.Array.FindIndex(skin.bones, bone => bone != null && bone.name == boneName);
        if (boneIndex < 0)
            throw new System.InvalidOperationException("Button bone not found: " + boneName);

        var source = skin.sharedMesh;
        var counts = source.GetBonesPerVertex();
        var weights = source.GetAllBoneWeights();
        var selected = new bool[source.vertexCount];
        int weightIndex = 0;
        for (int vertex = 0; vertex < selected.Length; vertex++)
        {
            float influence = 0f;
            for (int i = 0; i < counts[vertex]; i++)
            {
                var weight = weights[weightIndex++];
                if (weight.boneIndex == boneIndex)
                    influence += weight.weight;
            }
            selected[vertex] = influence > 0.999f;
        }

        using var meshData = MeshUtility.AcquireReadOnlyMeshData(source);
        var data = meshData[0];
        using var positions = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp);
        using var normals = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp);
        data.GetVertices(positions);
        data.GetNormals(normals);
        var toBone = source.bindposes[boneIndex];
        var normalToBone = toBone.inverse.transpose;
        var outputVertices = new List<Vector3>();
        var outputNormals = new List<Vector3>();
        var outputTriangles = new List<int>();
        var remap = new Dictionary<int, int>();

        int Remap(int index)
        {
            if (remap.TryGetValue(index, out int mapped))
                return mapped;
            mapped = outputVertices.Count;
            remap.Add(index, mapped);
            var normal = normalToBone.MultiplyVector(normals[index]).normalized;
            // 0.08 mm prevents z-fighting while matching the actual button surface.
            outputVertices.Add(toBone.MultiplyPoint3x4(positions[index]) + normal * 0.00008f);
            outputNormals.Add(normal);
            return mapped;
        }

        for (int submesh = 0; submesh < data.subMeshCount; submesh++)
        {
            using var indices = new NativeArray<int>(data.GetSubMesh(submesh).indexCount, Allocator.Temp);
            data.GetIndices(indices, submesh);
            for (int i = 0; i < indices.Length; i += 3)
            {
                int a = indices[i], b = indices[i + 1], c = indices[i + 2];
                if (!selected[a] || !selected[b] || !selected[c])
                    continue;
                outputTriangles.Add(Remap(a));
                outputTriangles.Add(Remap(b));
                outputTriangles.Add(Remap(c));
            }
        }
        if (outputTriangles.Count == 0)
            throw new System.InvalidOperationException("No rigid button triangles found.");
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(outputPath);
        if (mesh == null)
        {
            mesh = new Mesh { name = System.IO.Path.GetFileNameWithoutExtension(outputPath) };
            AssetDatabase.CreateAsset(mesh, outputPath);
        }
        Undo.RecordObject(mesh, "Build controller button surface");
        mesh.Clear();
        mesh.SetVertices(outputVertices);
        mesh.SetNormals(outputNormals);
        mesh.SetTriangles(outputTriangles, 0);
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        AssetDatabase.SaveAssets();
        return mesh;
    }
}
#endif
