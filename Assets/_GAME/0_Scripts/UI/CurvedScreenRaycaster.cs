using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

[RequireComponent(typeof(Canvas))]
public sealed class CurvedScreenRaycaster : GraphicRaycaster
{
    [SerializeField] private MeshCollider screen;
    [SerializeField] private LayerMask occlusionMask = Physics.DefaultRaycastLayers;

    private readonly List<RaycastResult> canvasHits = new List<RaycastResult>(16);
    private readonly Dictionary<IUIInteractor, Vector3> surfacePoints = new Dictionary<IUIInteractor, Vector3>(2);
    private CurvedScreenRaycaster parentRaycaster;

    public override Camera eventCamera => GetParentRaycaster()?.eventCamera ?? base.eventCamera;

    private CurvedScreenRaycaster GetParentRaycaster()
    {
        // TMP_Dropdown adds a raycaster to its popup and blocker by type.
        if (screen == null && parentRaycaster == null && transform.parent != null)
            parentRaycaster = transform.parent.GetComponentInParent<CurvedScreenRaycaster>();
        return parentRaycaster;
    }

    public bool TryGetSurfacePoint(IUIInteractor interactor, out Vector3 point)
    {
        return surfacePoints.TryGetValue(interactor, out point);
    }

    protected override void OnDisable()
    {
        surfacePoints.Clear();
        base.OnDisable();
    }

    public override void Raycast(PointerEventData eventData, List<RaycastResult> results)
    {
        if (!(eventData is TrackedDeviceEventData tracked))
            return;

        if (tracked.interactor != null)
            surfacePoints.Remove(tracked.interactor);

        var parent = GetParentRaycaster();
        var surface = parent != null ? parent.screen : screen;
        if (tracked.rayPoints == null ||
            surface == null || !surface.enabled || !surface.gameObject.activeInHierarchy ||
            (tracked.layerMask.value & (1 << surface.gameObject.layer)) == 0)
            return;

        if (tracked.interactor != null && tracked.interactor.TryGetUIModel(out var model) &&
            model.interactionType == UIInteractionType.Poke)
            return;

        var camera = eventCamera;
        if (camera == null || !camera.isActiveAndEnabled || camera.targetTexture == null)
            return;

        var points = tracked.rayPoints;
        for (var index = 1; index < points.Count; index++)
        {
            if (!Physics.Linecast(points[index - 1], points[index], out var hit,
                    (parent != null ? parent.occlusionMask.value : occlusionMask.value) |
                    (1 << surface.gameObject.layer), QueryTriggerInteraction.Ignore))
                continue;

            if (hit.collider == surface)
                RaycastCanvas(tracked, results, camera, hit, index);

            // The first solid surface blocks the rest of the ray.
            break;
        }
    }

    private void RaycastCanvas(TrackedDeviceEventData pointer, List<RaycastResult> results,
        Camera canvasCamera, RaycastHit surfaceHit, int segmentIndex)
    {
        var uv = surfaceHit.textureCoord;
        var texture = canvasCamera.targetTexture;
        var pixel = new Vector2(uv.x * texture.width, uv.y * texture.height);
        var originalPosition = pointer.position;
        var inputCamera = (pointer.currentInputModule as UIInputModule)?.uiCamera;
        if (inputCamera == null)
            inputCamera = canvasCamera;

        var depth = inputCamera.WorldToScreenPoint(surfaceHit.point).z;
        var pointerWorldPosition = inputCamera.ScreenToWorldPoint(
            new Vector3(pixel.x, pixel.y, Mathf.Max(inputCamera.nearClipPlane, depth)));

        try
        {
            pointer.position = pixel;
            base.Raycast(pointer, canvasHits);
            foreach (var canvasHit in canvasHits)
            {
                var result = canvasHit;
                // XRIT 3.3 projects worldPosition through its input camera to obtain
                // PointerEventData.position for controls such as Slider and ScrollRect.
                result.worldPosition = pointerWorldPosition;
                result.worldNormal = surfaceHit.normal;
                result.distance = surfaceHit.distance;
                result.index = results.Count;
                results.Add(result);
            }

            if (canvasHits.Count > 0)
            {
                pointer.rayHitIndex = segmentIndex;
                if (pointer.interactor != null)
                    surfacePoints[pointer.interactor] = surfaceHit.point;
            }
        }
        finally
        {
            pointer.position = originalPosition;
            canvasHits.Clear();
        }
    }
}
