using UnityEngine;
using UnityEngine.InputSystem;

public class VRSurfacePainter : MonoBehaviour
{
    [Header("Pen")]
    [SerializeField] private Transform penTip;

    [SerializeField] private float penRadius = 0.003f;

    [SerializeField] private float castDistance = 0.03f;

    [Header("Input")]
    [SerializeField] private InputActionProperty triggerAction;

    [Header("Canvas")]
    [SerializeField] private MeshRenderer canvasRenderer;

    [SerializeField] private LayerMask drawingLayer;

    [Header("Brush")]
    [SerializeField] private int textureSize = 1024;

    [SerializeField] private int brushRadius = 5;

    [SerializeField] private Color brushColor = Color.black;

    private Texture2D _texture;

    private bool _hasPreviousUv;

    private Vector2 _previousUv;

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

        FillTexture(Color.white);

        canvasRenderer.material.mainTexture = _texture;
    }

    private void Update()
    {
        bool triggerPressed =
            triggerAction.action.ReadValue<float>() > 0.1f;

        if (!triggerPressed)
        {
            _hasPreviousUv = false;
            return;
        }

        if (!TryGetUv(out Vector2 uv))
        {
            _hasPreviousUv = false;
            return;
        }

        if (!_hasPreviousUv)
        {
            DrawAtUv(uv);

            _previousUv = uv;

            _hasPreviousUv = true;

            return;
        }

        DrawInterpolated(_previousUv, uv);

        _previousUv = uv;
    }

    private void LateUpdate()
    {
        if (!_dirty)
            return;

        _texture.Apply();

        _dirty = false;
    }

    private bool TryGetUv(out Vector2 uv)
    {
        uv = default;

        // Немного отодвигаем начало луча назад
        Vector3 origin =
            penTip.position -
            penTip.forward * penRadius;

        bool success =
            Physics.SphereCast(
                origin,
                penRadius,
                penTip.forward,
                out RaycastHit hit,
                castDistance,
                drawingLayer);

        if (!success)
            return false;

        uv = hit.textureCoord;

        return true;
    }

    private void DrawInterpolated(
        Vector2 fromUv,
        Vector2 toUv)
    {
        float distance =
            Vector2.Distance(
                fromUv,
                toUv);

        int steps =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    distance * textureSize));

        for (int i = 0; i <= steps; i++)
        {
            float t =
                i / (float)steps;

            Vector2 uv =
                Vector2.Lerp(
                    fromUv,
                    toUv,
                    t);

            DrawAtUv(uv);
        }
    }

    private void DrawAtUv(
        Vector2 uv)
    {
        int x =
            Mathf.RoundToInt(
                uv.x * textureSize);

        int y =
            Mathf.RoundToInt(
                uv.y * textureSize);

        DrawCircle(
            x,
            y);

        _dirty = true;
    }

    private void DrawCircle(
        int centerX,
        int centerY)
    {
        for (int x = -brushRadius; x <= brushRadius; x++)
        {
            for (int y = -brushRadius; y <= brushRadius; y++)
            {
                if (x * x + y * y >
                    brushRadius * brushRadius)
                    continue;

                int px =
                    centerX + x;

                int py =
                    centerY + y;

                if (px < 0 ||
                    px >= textureSize)
                    continue;

                if (py < 0 ||
                    py >= textureSize)
                    continue;

                _texture.SetPixel(
                    px,
                    py,
                    brushColor);
            }
        }
    }

    private void FillTexture(
        Color color)
    {
        Color[] pixels =
            new Color[
                textureSize * textureSize];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        _texture.SetPixels(
            pixels);

        _texture.Apply();
    }
}