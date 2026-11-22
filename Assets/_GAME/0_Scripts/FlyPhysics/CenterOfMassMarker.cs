using System;
using UnityEngine;

public class CenterOfMassMarker : MonoBehaviour
{

    [SerializeField] private bool ShowCOM = true;

    [SerializeField,Range (0.001f, 0.03f)] private float COMSize = 0.03f;

    private void OnDrawGizmos()
    {
        if (ShowCOM)
        {
            Gizmos.color = Color.pink;

            Gizmos.DrawSphere(transform.position, COMSize);

            Gizmos.DrawLine(
                transform.position,
                transform.position + Vector3.up * 0.1f);
        }
    }
    [SerializeField, Tooltip("Корень детали с MeshFilter. Если не задан, используется родитель маркера.")]
    private Transform meshRoot;

#if UNITY_EDITOR
    public void MoveToMeshCenterOfMass()
    {
        Transform root = meshRoot != null ? meshRoot : transform.parent;
        if (root == null)
        {
            Debug.LogWarning("Укажите Mesh Root или поместите маркер дочерним объектом детали.", this);
            return;
        }

        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        double totalVolume = 0d, x = 0d, y = 0d, z = 0d;
        foreach (MeshFilter filter in filters)
        {
            if (filter.sharedMesh == null)
                continue;

            // Moving the marker must not also move the geometry being measured.
            if (filter.transform == transform || filter.transform.IsChildOf(transform))
            {
                Debug.LogWarning("Маркер должен быть отдельным объектом без мешей в его иерархии.", this);
                return;
            }

            if (!TryCalculateCenterOfMass(filter.sharedMesh, out Vector3 center, out double meshVolume))
            {
                Debug.LogWarning($"Не удалось рассчитать объём меша '{filter.sharedMesh.name}'. Проверьте геометрию и Read/Write.", filter);
                return;
            }

            double worldVolume = meshVolume * Math.Abs(filter.transform.localToWorldMatrix.determinant);
            Vector3 worldCenter = filter.transform.TransformPoint(center);
            if (!IsFinite(worldVolume) || worldVolume <= 0d ||
                !IsFinite(worldCenter.x) || !IsFinite(worldCenter.y) || !IsFinite(worldCenter.z))
            {
                Debug.LogWarning("Некорректный масштаб или координаты меша.", filter);
                return;
            }

            totalVolume += worldVolume;
            x += worldCenter.x * worldVolume;
            y += worldCenter.y * worldVolume;
            z += worldCenter.z * worldVolume;
        }

        if (!IsFinite(totalVolume) || totalVolume <= 0d)
        {
            Debug.LogWarning("В корне детали не найдены меши с ненулевым объёмом.", this);
            return;
        }

        UnityEditor.Undo.RecordObject(transform, "Move marker to center of mass");
        transform.position = new Vector3((float)(x / totalVolume), (float)(y / totalVolume), (float)(z / totalVolume));
        if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(transform))
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(transform);
        UnityEditor.EditorUtility.SetDirty(transform);
        UnityEditor.SceneView.RepaintAll();
    }
#endif
    /// <summary>
    /// Calculates the volume centroid of a solid with uniform density, in mesh-local space.
    /// Requires a closed, non-self-intersecting triangle mesh with consistent winding. Runtime access requires Read/Write.
    /// Cavities must have the opposite winding to the outer surface. Topology is not validated.
    /// Call when baking or initializing, not every frame: mesh arrays allocate memory.
    /// </summary>
    /// <remarks>
    /// Returns false for missing/unreadable meshes, unsupported topology, non-finite geometry,
    /// or negligible signed volume. A true result does not prove the surface is closed.
    /// Volume is in cubed mesh units; Transform scale is not included.
    /// </remarks>
    public static bool TryCalculateCenterOfMass(
        Mesh mesh, out Vector3 localCenterOfMass, out double volume)
    {
        localCenterOfMass = Vector3.zero;
        volume = 0d;

        if (mesh == null || mesh.vertexCount < 4)
            return false;

#if UNITY_EDITOR
        if (Application.isPlaying && !mesh.isReadable)
            return false;
#else
        if (!mesh.isReadable)
            return false;
#endif

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            if (mesh.GetTopology(i) != MeshTopology.Triangles)
                return false;
        }

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        if (triangles.Length < 12 || triangles.Length % 3 != 0)
            return false;

        // Shift the reference near the geometry to reduce cancellation for offset meshes.
        Vector3 origin = vertices[triangles[0]];
        double signedVolume6 = 0d;
        double absoluteVolume6 = 0d;
        double momentX = 0d, momentY = 0d, momentZ = 0d;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i]];
            Vector3 b = vertices[triangles[i + 1]];
            Vector3 c = vertices[triangles[i + 2]];
            double ax = (double)a.x - origin.x, ay = (double)a.y - origin.y, az = (double)a.z - origin.z;
            double bx = (double)b.x - origin.x, by = (double)b.y - origin.y, bz = (double)b.z - origin.z;
            double cx = (double)c.x - origin.x, cy = (double)c.y - origin.y, cz = (double)c.z - origin.z;

            // Six times the signed volume of tetrahedron (origin, a, b, c).
            double v6 = ax * (by * cz - bz * cy)
                      + ay * (bz * cx - bx * cz)
                      + az * (bx * cy - by * cx);
            signedVolume6 += v6;
            absoluteVolume6 += Math.Abs(v6);
            momentX += (ax + bx + cx) * v6;
            momentY += (ay + by + cy) * v6;
            momentZ += (az + bz + cz) * v6;
        }

        if (!IsFinite(signedVolume6) || absoluteVolume6 == 0d ||
            Math.Abs(signedVolume6) <= absoluteVolume6 * 1e-12d)
            return false;

        // Each tetrahedron's centroid is (a + b + c) / 4 relative to origin.
        double denominator = 4d * signedVolume6;
        double x = origin.x + momentX / denominator;
        double y = origin.y + momentY / denominator;
        double z = origin.z + momentZ / denominator;
        if (!IsFinite(x) || !IsFinite(y) || !IsFinite(z) ||
            Math.Abs(x) > float.MaxValue || Math.Abs(y) > float.MaxValue || Math.Abs(z) > float.MaxValue)
            return false;

        localCenterOfMass = new Vector3((float)x, (float)y, (float)z);
        volume = Math.Abs(signedVolume6) / 6d;
        return true;
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(CenterOfMassMarker))]
public sealed class CenterOfMassMarkerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        UnityEditor.EditorGUILayout.HelpBox(
            "Равномерная плотность. Меши должны быть замкнутыми, без самопересечений и с согласованным порядком вершин. Пересекающиеся меши учитываются отдельно.",
            UnityEditor.MessageType.Info);
        using (new UnityEditor.EditorGUI.DisabledScope(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Переместить в центр масс"))
                ((CenterOfMassMarker)target).MoveToMeshCenterOfMass();
        }
    }
}
#endif