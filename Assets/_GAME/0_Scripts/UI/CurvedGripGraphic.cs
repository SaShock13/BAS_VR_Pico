using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class CurvedGripGraphic : MaskableGraphic
{
    [SerializeField] private Vector3[] points;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points == null) return;
        var halfHeight = Vector3.up * (rectTransform.rect.height * .5f);
        for (int i = 0; i < points.Length; i++)
        {
            vh.AddVert(points[i] - halfHeight, color, Vector2.zero);
            vh.AddVert(points[i] + halfHeight, color, Vector2.zero);
            if (i == 0) continue;
            int start = (i - 1) * 2;
            vh.AddTriangle(start, start + 1, start + 3);
            vh.AddTriangle(start, start + 3, start + 2);
        }
    }
}
