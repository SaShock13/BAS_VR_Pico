using TMPro;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(TextMeshProUGUI))]
public sealed class CurvedPanelText : MonoBehaviour
{
    [SerializeField] private Vector3[] contour;
    private TextMeshProUGUI _text;

    private void OnEnable()
    {
        _text = GetComponent<TextMeshProUGUI>();
        _text.OnPreRenderText += Bend;
        _text.SetVerticesDirty();
    }

    private void OnDisable()
    {
        if (!_text) return;
        _text.OnPreRenderText -= Bend;
        _text.SetVerticesDirty();
    }

    private void Bend(TMP_TextInfo info)
    {
        if (contour == null || contour.Length < 2) return;
        for (int material = 0; material < info.meshInfo.Length; material++)
        {
            var mesh = info.meshInfo[material];
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                var point = mesh.vertices[i];
                int segment = 1;
                while (segment < contour.Length - 1 && contour[segment].x < point.x) segment++;
                var a = contour[segment - 1];
                var b = contour[segment];
                point.z = Mathf.Lerp(a.z, b.z, Mathf.InverseLerp(a.x, b.x, point.x)) - 2f;
                mesh.vertices[i] = point;
            }
        }
    }
}
