using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

// Shared fitting tools for arm and motor covers. Local axes: X length, Y height, Z width.
public sealed class ArmCoverScaleService : IInitializable, IDisposable
{
    public const float MinScale = 0.25f;
    public const float MaxScale = 3f;
    private readonly Clean_AssemblySystem _assembly;
    private readonly PartViewRegistry _views;
    private readonly IEventBus _bus;
    private IDisposable _subscription;
    private IDisposable _offsetSubscription;
    private IDisposable _commitSubscription;
    private readonly HashSet<string> _pendingChanges = new();

    public ArmCoverScaleService(Clean_AssemblySystem assembly, PartViewRegistry views, IEventBus bus)
    {
        _assembly = assembly;
        _views = views;
        _bus = bus;
    }

    public void Initialize()
    {
        _subscription = _bus.Subscribe<SetArmCoverScaleRequest>(OnRequest);
        _offsetSubscription = _bus.Subscribe<SetArmCoverOffsetRequest>(OnOffsetRequest);
        _commitSubscription = _bus.Subscribe<CommitArmCoverAdjustmentRequest>(r => Commit(r.InstanceId));
    }
    public void Dispose() { _subscription?.Dispose(); _offsetSubscription?.Dispose(); _commitSubscription?.Dispose(); _pendingChanges.Clear(); }

    private void Commit(string id)
    {
        if (string.IsNullOrEmpty(id) || !_pendingChanges.Remove(id)) return;
        _bus.Publish(new AssemblyGeometryChangedEvent());
        _bus.Publish(new AssemblyChangedEvent { Timestamp = DateTime.UtcNow });
    }

    private void OnOffsetRequest(SetArmCoverOffsetRequest request)
    {
        if (!PreviewOffset(request.InstanceId, request.Offset)) return;
        _bus.Publish(new ArmCoverOffsetChangedEvent { InstanceId = request.InstanceId, Timestamp = DateTime.UtcNow });
        Commit(request.InstanceId);
    }

    // Like the existing material slider previews, continuous dragging bypasses the allocating
    // EventBus. The release command commits geometry invalidation and the undo snapshot once.
    public bool PreviewOffset(string id, float offset)
    {
        if (!CanMoveAlong(id) || !TryGetCover(id, out var view) || view.IsGrabbed ||
            PartSpawnAnimator.IsAnimating(view.transform) || !IsFinite(offset)) return false;
        var position = view.transform.localPosition;
        position.x = Mathf.Clamp(offset, -0.1f, 0.1f);
        if ((position - view.transform.localPosition).sqrMagnitude < 0.0000000001f) return false;
        view.transform.localPosition = position;
        _assembly.TryGetPartDomainState(id, out var state);
        state.SetArmCoverOffset(position.x);
        _pendingChanges.Add(id);
        return true;
    }

    public bool TryGetCover(string id, out DronePartView view)
    {
        view = null;
        return !string.IsNullOrEmpty(id) && _assembly.TryGetPartDomainState(id, out var part) &&
            IsCover(part.Type) && _views.TryGet(id, out view);
    }

    public bool CanMoveAlong(string id)
    {
        return !string.IsNullOrEmpty(id) && _assembly.TryGetPartDomainState(id, out var part) &&
            IsCover(part.Type) && part.LifecycleState == PartLifecycleState.Installed &&
            !part.IsAttachedToZone && !string.IsNullOrEmpty(part.AttachedPartInstanceId) &&
            _assembly.TryGetPartDomainState(part.AttachedPartInstanceId, out var parent) && parent.Type == PartType.Arm;
    }

    public float GetOffset(string id) => !string.IsNullOrEmpty(id) && _assembly.TryGetPartDomainState(id, out var part)
        ? part.ArmCoverOffset : 0f;

    public static bool IsCover(PartType type) => type == PartType.ArmCover || type == PartType.MotorCover;

    public bool UsesUniformScaleOnly(string id) => !string.IsNullOrEmpty(id) &&
        _assembly.TryGetPartDomainState(id, out var part) && part.Type == PartType.MotorCover;

    public bool CanScaleHeight(string id) => !string.IsNullOrEmpty(id) &&
        _assembly.TryGetPartDomainState(id, out var part) && IsCover(part.Type) &&
        (part.Type != PartType.MotorCover || part.LifecycleState == PartLifecycleState.Installed);

    // Limit the common factor, rather than each axis, so proportional scaling keeps the fit.
    public static Vector3 UniformScale(Vector3 start, float factor)
    {
        if (!IsFinite(start) || !IsFinite(factor) || start.x <= 0 || start.y <= 0 || start.z <= 0) return start;
        float minimum = Mathf.Max(MinScale / start.x, Mathf.Max(MinScale / start.y, MinScale / start.z));
        float maximum = Mathf.Min(MaxScale / start.x, Mathf.Min(MaxScale / start.y, MaxScale / start.z));
        return start * Mathf.Clamp(factor, minimum, maximum);
    }

    private void OnRequest(SetArmCoverScaleRequest request)
    {
        if (!PreviewScale(request.InstanceId, request.ResetToDefault ? Vector3.one : request.Scale, request.ResetToDefault)) return;
        TryGetCover(request.InstanceId, out var view);
        _bus.Publish(new ArmCoverScaleChangedEvent
        {
            InstanceId = request.InstanceId, Scale = view.transform.localScale, Timestamp = DateTime.UtcNow
        });
        Commit(request.InstanceId);
    }

    public bool PreviewScale(string id, Vector3 requested)
        => PreviewScale(id, requested, false);

    private bool PreviewScale(string id, Vector3 requested, bool reset)
    {
        if (!TryGetCover(id, out var view) || view.IsGrabbed ||
            PartSpawnAnimator.IsAnimating(view.transform) || !IsFinite(requested)) return false;
        if (UsesUniformScaleOnly(id) && !reset)
        {
            var current = view.transform.localScale;
            if (current.x <= 0 || current.y <= 0 || current.z <= 0) return false;
            var factor = new Vector3(requested.x / current.x, requested.y / current.y, requested.z / current.z);
            bool uniform = Mathf.Approximately(factor.x, factor.y) && Mathf.Approximately(factor.x, factor.z);
            bool heightOnly = CanScaleHeight(id) && Mathf.Approximately(requested.x, current.x) &&
                Mathf.Approximately(requested.z, current.z);
            if (!uniform && !heightOnly) return false;
            if (uniform) requested = UniformScale(current, factor.x);
        }
        var scale = new Vector3(Mathf.Clamp(requested.x, MinScale, MaxScale),
            Mathf.Clamp(requested.y, MinScale, MaxScale), Mathf.Clamp(requested.z, MinScale, MaxScale));
        if ((view.transform.localScale - scale).sqrMagnitude < 0.00000001f) return false;
        view.transform.localScale = scale;
        _pendingChanges.Add(id);
        return true;
    }

    private static bool IsFinite(Vector3 v) => IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);
    private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
}
