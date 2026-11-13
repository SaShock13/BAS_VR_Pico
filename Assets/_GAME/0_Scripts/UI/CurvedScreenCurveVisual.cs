using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public sealed class CurvedScreenCurveVisual : MonoBehaviour, ICurveInteractionDataProvider
{
    [SerializeField] private NearFarInteractor interactor;

    private ICurveInteractionDataProvider Source => interactor;

    public bool isActive => interactor != null && Source.isActive;
    public bool hasValidSelect => Source.hasValidSelect;
    public Transform curveOrigin => Source.curveOrigin;
    public NativeArray<Vector3> samplePoints => Source.samplePoints;
    public Vector3 lastSamplePoint => Source.lastSamplePoint;

    public EndPointType TryGetCurveEndPoint(out Vector3 endPoint,
        bool snapToSelectedAttachIfAvailable = false, bool snapToSnapVolumeIfAvailable = false)
    {
        var type = Source.TryGetCurveEndPoint(out endPoint, snapToSelectedAttachIfAvailable,
            snapToSnapVolumeIfAvailable);
        if (type == EndPointType.UI &&
            interactor.TryGetCurrentUIRaycastResult(out var hit) &&
            hit.module is CurvedScreenRaycaster raycaster &&
            raycaster.TryGetSurfacePoint(interactor, out var surfacePoint))
        {
            endPoint = surfacePoint;
        }

        return type;
    }

    public EndPointType TryGetCurveEndNormal(out Vector3 endNormal,
        bool snapToSelectedAttachIfAvailable = false)
    {
        return Source.TryGetCurveEndNormal(out endNormal, snapToSelectedAttachIfAvailable);
    }
}
