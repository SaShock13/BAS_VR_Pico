using UnityEngine;
using UnityEngine.InputSystem;

public class VRTexturePainterWithTrigger : MonoBehaviour
{
    [Header("Pen")]
    [SerializeField] private Transform penTip;

    [Header("Canvas")]
    [SerializeField] private MeshRenderer canvasRenderer;

    [Header("Input")]
    [SerializeField] private InputActionProperty triggerAction;

    [Header("Settings")]
    [SerializeField] private LayerMask drawingLayer;
    [SerializeField] private int textureSize = 1024;
    [SerializeField] private int brushSize = 10;
    [SerializeField] private Color brushColor = Color.black;

    private Texture2D texture;
    private bool isDrawing;

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


        Debug.Log($"isDrawing {isDrawing}");
        if (!isDrawing)
            return;

        if (Physics.Raycast(
            penTip.position,
            penTip.forward,
            out RaycastHit hit,
            0.03f,
            drawingLayer))
        {
            Draw(hit.textureCoord);
        }
    }

    private void Draw(Vector2 uv)
    {
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

    private void Fill(Color color)
    {
        var pixels = new Color[textureSize * textureSize];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        texture.SetPixels(pixels);
        texture.Apply();
    }
}