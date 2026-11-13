using UnityEngine;

internal static class MountingBounds
{
    internal static Bounds Transform(Bounds bounds, Matrix4x4 matrix)
    {
        var e = bounds.extents;
        var transformedExtents = new Vector3(
            Mathf.Abs(matrix.m00) * e.x + Mathf.Abs(matrix.m01) * e.y + Mathf.Abs(matrix.m02) * e.z,
            Mathf.Abs(matrix.m10) * e.x + Mathf.Abs(matrix.m11) * e.y + Mathf.Abs(matrix.m12) * e.z,
            Mathf.Abs(matrix.m20) * e.x + Mathf.Abs(matrix.m21) * e.y + Mathf.Abs(matrix.m22) * e.z);
        return new Bounds(matrix.MultiplyPoint3x4(bounds.center), transformedExtents * 2f);
    }

    internal static bool Overlaps(Bounds a, Bounds b)
    {
        const float tolerance = 0.00001f;
        var delta = a.center - b.center;
        var extents = a.extents + b.extents;
        return Mathf.Abs(delta.x) < extents.x - tolerance &&
            Mathf.Abs(delta.y) < extents.y - tolerance &&
            Mathf.Abs(delta.z) < extents.z - tolerance;
    }
}
