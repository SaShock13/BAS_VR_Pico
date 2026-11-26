using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Canvas))]
public sealed class CurvedScreenRaycaster : GraphicRaycaster
{
    [SerializeField] private MeshCollider screen;
    [SerializeField] private LayerMask occlusionMask = Physics.DefaultRaycastLayers;

    private readonly List<RaycastResult> canvasHits = new List<RaycastResult>(16);
    private readonly Dictionary<IUIInteractor, Vector3> surfacePoints = new Dictionary<IUIInteractor, Vector3>(2);
    private CurvedScreenRaycaster parentRaycaster;
    private readonly RaycastHit[] occlusionHits = new RaycastHit[64];
    private readonly Dictionary<Collider, bool> grabTriggers = new Dictionary<Collider, bool>(256);

    private Camera inputCamera;
    private bool projectingCanvas;
    private Camera CanvasCamera => GetParentRaycaster()?.CanvasCamera ?? base.eventCamera;

    // EventSystem compares camera depth before hit distance. The offscreen render
    // camera must not give this canvas priority over nearer world-space UI.
    public override Camera eventCamera => projectingCanvas ? CanvasCamera : inputCamera ?? CanvasCamera;

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
        grabTriggers.Clear();
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

        inputCamera = (tracked.currentInputModule as UIInputModule)?.uiCamera;
        var camera = CanvasCamera;
        if (camera == null || !camera.isActiveAndEnabled || camera.targetTexture == null)
            return;

        var points = tracked.rayPoints;
        var distanceTravelled = 0f;
        for (var index = 1; index < points.Count; index++)
        {
            var segment = points[index] - points[index - 1];
            var length = segment.magnitude;
            var segmentStartDistance = distanceTravelled;
            distanceTravelled += length;
            if (length <= Mathf.Epsilon)
                continue;
            var count = Physics.RaycastNonAlloc(points[index - 1], segment / length, occlusionHits,
                length, (parent != null ? parent.occlusionMask.value : occlusionMask.value) |
                (1 << surface.gameObject.layer), QueryTriggerInteraction.Collide);
            // Never allow UI through an ambiguous, saturated query.
            if (count == occlusionHits.Length)
                break;
            var nearest = float.PositiveInfinity;
            var hit = default(RaycastHit);
            for (var i = 0; i < count; i++)
            {
                var candidate = occlusionHits[i];
                // Socket and proximity triggers are not physical blockers. Grab
                // colliders are, even when the part uses triggers for XR selection.
                if (candidate.collider != surface && candidate.collider.isTrigger &&
                    !IsGrabTrigger(candidate.collider))
                    continue;
                if (candidate.distance >= nearest)
                    continue;
                nearest = candidate.distance;
                hit = candidate;
            }
            if (hit.collider == null)
                continue;

            if (hit.collider == surface)
                RaycastCanvas(tracked, results, camera, hit, index, segmentStartDistance);

            // The first solid surface blocks the rest of the ray.
            break;
        }
    }

    private bool IsGrabTrigger(Collider collider)
    {
        if (grabTriggers.TryGetValue(collider, out var isGrab))
            return isGrab;
        // Bound retained references when parts are spawned and removed repeatedly.
        if (grabTriggers.Count >= 256)
            grabTriggers.Clear();
        isGrab = collider.GetComponentInParent<XRGrabInteractable>() != null;
        grabTriggers.Add(collider, isGrab);
        return isGrab;
    }

    private void RaycastCanvas(TrackedDeviceEventData pointer, List<RaycastResult> results,
        Camera canvasCamera, RaycastHit surfaceHit, int segmentIndex, float segmentStartDistance)
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
            projectingCanvas = true;
            base.Raycast(pointer, canvasHits);
            projectingCanvas = false;
            foreach (var canvasHit in canvasHits)
            {
                var result = canvasHit;
                // XRIT 3.3 projects worldPosition through its input camera to obtain
                // PointerEventData.position for controls such as Slider and ScrollRect.
                result.worldPosition = pointerWorldPosition;
                result.worldNormal = surfaceHit.normal;
                result.distance = segmentStartDistance + surfaceHit.distance;
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
            projectingCanvas = false;
            pointer.position = originalPosition;
            canvasHits.Clear();
        }
    }
}
