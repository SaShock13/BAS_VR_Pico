using UnityEngine;
using UnityEngine.UI;

// Vector reset arrow; geometry is rebuilt only when the UI is dirtied.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class PanelResetIcon : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var rect = GetPixelAdjustedRect();
        var center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * .32f;
        float halfWidth = radius * .14f;
        const int segments = 20;
        for (int i = 0; i < segments; i++)
        {
            float a = (40f + 280f * i / segments) * Mathf.Deg2Rad;
            float b = (40f + 280f * (i + 1) / segments) * Mathf.Deg2Rad;
            var da = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var db = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
            int start = vh.currentVertCount;
            vh.AddVert(center + da * (radius - halfWidth), color, Vector2.zero);
            vh.AddVert(center + da * (radius + halfWidth), color, Vector2.zero);
            vh.AddVert(center + db * (radius + halfWidth), color, Vector2.zero);
            vh.AddVert(center + db * (radius - halfWidth), color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
        float angle = 320f * Mathf.Deg2Rad;
        var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        var tangent = new Vector2(-direction.y, direction.x);
        var end = center + direction * radius;
        int arrow = vh.currentVertCount;
        vh.AddVert(end + tangent * radius * .48f, color, Vector2.zero);
        vh.AddVert(end - tangent * radius * .22f + direction * radius * .42f, color, Vector2.zero);
        vh.AddVert(end - tangent * radius * .22f - direction * radius * .42f, color, Vector2.zero);
        vh.AddTriangle(arrow, arrow + 1, arrow + 2);
    }
}
