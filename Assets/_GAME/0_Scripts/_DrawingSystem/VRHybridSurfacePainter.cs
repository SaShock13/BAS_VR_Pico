using UnityEngine;
using UnityEngine.InputSystem;

public class VRHybridSurfacePainter : MonoBehaviour
{
    [Header("Pen")]
    [SerializeField] private Transform penTip;

    [Header("Canvas")]
    [SerializeField] private BoxCollider canvasCollider;
    [SerializeField] private MeshRenderer canvasRenderer;

    [Header("Input")]
    [SerializeField] private InputActionProperty triggerAction;

    [Header("Settings")]
    [SerializeField] private LayerMask drawingLayer;

    [SerializeField] private int textureSize = 1024;
    [SerializeField] private int brushSize = 8;
    [SerializeField] private float minDistance = 0.002f;

    [SerializeField] private Color brushColor = Color.black;

    private Texture2D texture;

    private bool isDrawing;

    private Vector3 lastPoint;



    [SerializeField] private Transform TestpenTip;
    [SerializeField] private BoxCollider TestCollider;

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
        texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

        Fill(Color.white);

        canvasRenderer.material.mainTexture = texture;
    }

    private void Update()
    {
        isDrawing = triggerAction.action.ReadValue<float>() > 0.1f;



        //Debug.Log($"Test Closest point to {TestpenTip.position} is {TestCollider.ClosestPoint(TestpenTip.position)}");

        //Debug.Log($"isDrawing {isDrawing}");
        if (!isDrawing)
        {
            lastPoint = Vector3.zero;
            return;
        }

        if (!TryGetSurfacePoint(out Vector3 point, out RaycastHit hit))
            return;

        if (lastPoint == Vector3.zero)
        {
            lastPoint = point;


            Debug.Log($"lastPoint was zero and set to {lastPoint}");
            DrawAt(hit.textureCoord);
            return;
        }
        else Debug.Log($"lastPoint {lastPoint}");

            // интерполяция между точками
            float dist = Vector3.Distance(lastPoint, point);

        int steps = Mathf.CeilToInt(dist / minDistance);

        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;

            Vector3 interp = Vector3.Lerp(lastPoint, point, t);

            if (TryRayFromPoint(interp, out RaycastHit h))
            {
                DrawAt(h.textureCoord);
            }
        }

        lastPoint = point;
    }

    // -----------------------------
    // SURFACE PROJECTION CORE
    // -----------------------------

    private bool TryGetSurfacePoint(out Vector3 closestPoint, out RaycastHit hit)
    {
        closestPoint = canvasCollider.ClosestPoint(penTip.position);


        Debug.Log($"closestPoint {closestPoint}");
        Debug.Log($"penTip.position {penTip.position}");

        // raycast из проекции обратно в поверхность
        Vector3 dir = (closestPoint - penTip.position).normalized;

        if (Physics.Raycast(penTip.position, dir, out hit, 0.05f, drawingLayer))
        {

            Debug.Log($"Raycast hited {this}");
            return true;
        }

        // fallback: короткий каст из самой точки
        if (Physics.Raycast(closestPoint + Vector3.up * 0.01f, Vector3.down, out hit, 0.05f, drawingLayer))
        {
            Debug.Log($"Fallback Raycast hited {this}");
            return true;
        }

        return false;
    }

    private bool TryRayFromPoint(Vector3 worldPoint, out RaycastHit hit)
    {
        return Physics.Raycast(worldPoint + Vector3.up * 0.01f, Vector3.down, out hit, 0.05f, drawingLayer);
    }

    // -----------------------------
    // DRAWING
    // -----------------------------

    private void DrawAt(Vector2 uv)
    {

        Debug.Log($"DrawAt {uv}");
        int x = (int)(uv.x * textureSize);
        int y = (int)(uv.y * textureSize);

        DrawCircle(x, y);

        texture.Apply();
    }

    private void DrawCircle(int cx, int cy)
    {
        for (int x = -brushSize; x <= brushSize; x++)
        {
            for (int y = -brushSize; y <= brushSize; y++)
            {
                if (x * x + y * y > brushSize * brushSize)
                    continue;

                int px = cx + x;
                int py = cy + y;

                if (px < 0 || px >= textureSize || py < 0 || py >= textureSize)
                    continue;

                texture.SetPixel(px, py, brushColor);
            }
        }
    }

    private void Fill(Color c)
    {
        var pixels = new Color[textureSize * textureSize];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = c;

        texture.SetPixels(pixels);
        texture.Apply();
    }
}