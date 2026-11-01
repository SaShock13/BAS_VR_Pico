using UnityEngine;
using UnityEngine.InputSystem;

public class VRContactSpherePainter : MonoBehaviour
{
    [Header("Pen")]
    [SerializeField] private Transform contactSphere;
    [SerializeField] private Transform penForward;

    [Header("Input")]
    [SerializeField] private InputActionProperty triggerAction;

    [Header("Canvas")]
    [SerializeField] private LayerMask drawingLayer;

    [SerializeField] private MeshRenderer canvasRenderer;

    [Header("Texture")]
    [SerializeField] private int textureSize = 1024;
    [SerializeField] private int brushRadius = 6;
    [SerializeField] private Color brushColor = Color.black;

    private Texture2D _texture;

    private bool _isTouching;

    private bool _hasPrevUv;
    private Vector2 _prevUv;

    private bool _dirty;

    private void OnEnable()
    {
        triggerAction.action.Enable();
    }

    private void OnDisable()
    {
        triggerAction.action.Disable();
    }

    private void Start()
    {
        _texture = new Texture2D(
            textureSize,
            textureSize,
            TextureFormat.RGBA32,
            false);

        Fill(Color.white);

        canvasRenderer.material.mainTexture = _texture;
    }

    private void Update()
    {
        bool trigger =
            triggerAction.action.ReadValue<float>() > 0.1f;

        Debug.Log($"trigger {trigger}");
        Debug.Log($"_isTouching {_isTouching}");

        if (!_isTouching || !trigger)
        {
            _hasPrevUv = false;
            return;
        }


        if (!TryGetHit(out RaycastHit hit))
        {
            _hasPrevUv = false;
            return;
        }



        Debug.Log($"HIT  {hit}");
        Vector2 uv = hit.textureCoord;
        Debug.Log($"uv  {uv}");

        if (!_hasPrevUv)
        {
            DrawAt(uv);
            _prevUv = uv;
            _hasPrevUv = true;
            return;
        }

        DrawInterpolated(_prevUv, uv);
        _prevUv = uv;
    }

    private void LateUpdate()
    {
        if (!_dirty) return;

        _texture.Apply();
        _dirty = false;
    }

    // -----------------------------
    // CONTACT
    // -----------------------------

    private void OnTriggerEnter(Collider other)
    {

        if (((1 << other.gameObject.layer) & drawingLayer) == 0)
            return;
        Debug.Log($"OnTriggerEnter {this}");

        _isTouching = true;
    }

    private void OnTriggerExit(Collider other)
    {

        Debug.Log($"OnTriggerExit {this}");
        _isTouching = false;
        _hasPrevUv = false;
    }

    // -----------------------------
    // UV GET
    // -----------------------------

    private bool TryGetHit(out RaycastHit hit)
    {
        Vector3 origin =
            contactSphere.position - penForward.forward * 0.01f;

        return Physics.Raycast(
            origin,
            penForward.forward,
            out hit,
            0.05f,
            drawingLayer);
    }

    // -----------------------------
    // DRAWING
    // -----------------------------

    private void DrawInterpolated(Vector2 from, Vector2 to)
    {
        float dist = Vector2.Distance(from, to);

        int steps = Mathf.Max(1, Mathf.CeilToInt(dist * textureSize));

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            DrawAt(Vector2.Lerp(from, to, t));
        }
    }

    private void DrawAt(Vector2 uv)
    {
        int x = Mathf.RoundToInt(uv.x * textureSize);
        int y = Mathf.RoundToInt(uv.y * textureSize);

        DrawCircle(x, y);

        _dirty = true;
    }

    private void DrawCircle(int cx, int cy)
    {
        for (int x = -brushRadius; x <= brushRadius; x++)
        {
            for (int y = -brushRadius; y <= brushRadius; y++)
            {
                if (x * x + y * y > brushRadius * brushRadius)
                    continue;

                int px = cx + x;
                int py = cy + y;

                if (px < 0 || px >= textureSize ||
                    py < 0 || py >= textureSize)
                    continue;

                _texture.SetPixel(px, py, brushColor);
            }
        }
    }

    private void Fill(Color c)
    {
        var pixels = new Color[textureSize * textureSize];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = c;

        _texture.SetPixels(pixels);
        _texture.Apply();
    }
}