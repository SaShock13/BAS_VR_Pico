using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public sealed class ArmCoverAdjustmentView : MonoBehaviour
{
    [SerializeField] private GameObject controls;
    [SerializeField] private GameObject fittingContent;
    [SerializeField] private GameObject[] standardSections;
    [SerializeField] private TMP_Text dimensions;
    [SerializeField] private TMP_Text gizmoLabel;
    [SerializeField] private Transform gizmo;
    [SerializeField] private ArmCoverScaleHandle[] handles;
    [SerializeField] private Button[] alongButtons;
    [SerializeField] private GameObject[] axisControls = Array.Empty<GameObject>();
    [SerializeField] private GameObject[] motorScaleControls = Array.Empty<GameObject>();
    [SerializeField] private Button[] heightButtons = Array.Empty<Button>();
    [SerializeField] private TMP_Text fittingHint;
    [SerializeField, Range(0.01f, 0.25f)] private float scaleStep = 0.05f;

    private InspectorService _inspector;
    private ArmCoverScaleService _scales;
    private IEventBus _bus;
    private DronePartView _target;
    private ArmCoverGeometry _geometry;
    private ArmCoverScaleHandle _dragging;
    private string _id;
    private bool _showGizmo;
    private bool _uniformOnly;
    private bool _showFitting = true;
    private IDisposable _scaleSubscription;
    private IDisposable _grabSubscription;
    private IDisposable _geometrySubscription;
    private IDisposable _offsetSubscription;
    private IDisposable _rebuildSubscription;
    private float _dragStartOffset;
    private float _nextLabelTime;

    [Inject]
    public void Construct(InspectorService inspector, ArmCoverScaleService scales, IEventBus bus)
    {
        _inspector = inspector;
        _scales = scales;
        _bus = bus;
        foreach (var handle in handles) handle.Configure(this);
        _inspector.Updated += OnInspected;
        _inspector.Cleared += Clear;
        _scaleSubscription = bus.Subscribe<ArmCoverScaleChangedEvent>(OnScaleChanged);
        _offsetSubscription = bus.Subscribe<ArmCoverOffsetChangedEvent>(e => { if (e.InstanceId == _id) Refresh(); });
        _grabSubscription = bus.Subscribe<PartGrabStartedEvent>(_ => HideGizmo());
        _geometrySubscription = bus.Subscribe<AssemblyGeometryChangedEvent>(_ => Refresh());
        _rebuildSubscription = bus.Subscribe<DronesRebuiltEvent>(_ => Refresh());
        Clear();
        _inspector.RefreshSelectedPart();
    }

    private void OnDestroy()
    {
        if (_inspector != null) { _inspector.Updated -= OnInspected; _inspector.Cleared -= Clear; }
        _scaleSubscription?.Dispose();
        _grabSubscription?.Dispose();
        _geometrySubscription?.Dispose();
        _offsetSubscription?.Dispose();
        _rebuildSubscription?.Dispose();
    }

    private void OnDisable() => HideGizmo();

    private void OnInspected(InspectionContext context)
    {
        if (_id != context.Part.InstanceId) { HideGizmo(); _showFitting = true; }
        _id = context.Part.InstanceId;
        if (!_scales.TryGetCover(_id, out _target)) { Clear(); return; }
        _geometry = _target.GetComponent<ArmCoverGeometry>();
        controls.SetActive(true);
        SetSections();
        Refresh();
    }

    private void Clear()
    {
        HideGizmo();
        _target = null;
        _geometry = null;
        _id = null;
        if (controls != null) controls.SetActive(false);
        if (standardSections != null) foreach (var section in standardSections) if (section != null) section.SetActive(true);
    }

    public void ToggleProperties()
    {
        _showFitting = !_showFitting;
        if (!_showFitting) HideGizmo();
        SetSections();
    }

    private void SetSections()
    {
        if (fittingContent != null) fittingContent.SetActive(_showFitting);
        if (standardSections != null) foreach (var section in standardSections) if (section != null) section.SetActive(!_showFitting);
    }

    private void OnScaleChanged(ArmCoverScaleChangedEvent e)
    {
        if (e.InstanceId == _id) Refresh();
    }

    public void LengthPlus() => Step(0, 1);
    public void LengthMinus() => Step(0, -1);
    public void HeightPlus() => Step(1, 1);
    public void HeightMinus() => Step(1, -1);
    public void WidthPlus() => Step(2, 1);
    public void WidthMinus() => Step(2, -1);
    public void AlongPlus() => MoveAlong(0.001f);
    public void AlongMinus() => MoveAlong(-0.001f);
    public void ResetAlong() { if (_dragging == null) RequestOffset(0); }
    private void MoveAlong(float delta)
    {
        if (_target != null && _dragging == null) RequestOffset(_target.transform.localPosition.x + delta);
    }
    private void RequestOffset(float offset)
    {
        if (_target == null || _bus == null) return;
        _bus.Publish(new SetArmCoverOffsetRequest { InstanceId = _id, Offset = offset, Timestamp = DateTime.UtcNow });
    }
    public void ResetScale()
    {
        if (_dragging == null && _target != null && _bus != null)
            _bus.Publish(new SetArmCoverScaleRequest { InstanceId = _id, Scale = Vector3.one,
                ResetToDefault = true, Timestamp = DateTime.UtcNow });
    }

    public void UniformPlus() => UniformStep(1f + scaleStep);
    public void UniformMinus() => UniformStep(1f - scaleStep);
    private void UniformStep(float factor)
    {
        if (_target == null || _dragging != null) return;
        RequestScale(ArmCoverScaleService.UniformScale(PartSpawnAnimator.GetStableLocalScale(_target.transform), factor));
    }

    private void Step(int axis, int sign)
    {
        if (_target == null || _dragging != null) return;
        if (_scales.UsesUniformScaleOnly(_id) && (axis != 1 || !_scales.CanScaleHeight(_id))) return;
        var scale = PartSpawnAnimator.GetStableLocalScale(_target.transform);
        scale[axis] += scaleStep * sign;
        RequestScale(scale);
    }

    private void RequestScale(Vector3 scale)
    {
        if (_target == null || _bus == null) return;
        _bus.Publish(new SetArmCoverScaleRequest { InstanceId = _id, Scale = scale, Timestamp = DateTime.UtcNow });
    }

    public void ToggleGizmo()
    {
        if (_scales != null && _scales.UsesUniformScaleOnly(_id)) { ToggleUniformGizmo(); return; }
        if (_target == null || _target.IsGrabbed || PartSpawnAnimator.IsAnimating(_target.transform)) return;
        _showGizmo = !_showGizmo || _uniformOnly;
        _uniformOnly = false;
        Refresh();
    }

    public void ToggleUniformGizmo()
    {
        if (_target == null || _target.IsGrabbed || PartSpawnAnimator.IsAnimating(_target.transform)) return;
        _showGizmo = !_showGizmo || !_uniformOnly;
        _uniformOnly = true;
        Refresh();
    }

    private void HideGizmo()
    {
        if (_dragging != null) CommitDrag();
        _showGizmo = false;
        _uniformOnly = false;
        _dragging = null;
        if (gizmo != null) gizmo.gameObject.SetActive(false);
        if (gizmoLabel != null) gizmoLabel.text = "Ручки: выкл.";
    }

    private void Refresh()
    {
        bool motorCover = _scales != null && _scales.UsesUniformScaleOnly(_id);
        bool uniform = _uniformOnly || motorCover;
        bool canHeight = _scales != null && _scales.CanScaleHeight(_id);
        if (heightButtons != null) foreach (var button in heightButtons)
            if (button != null) { button.gameObject.SetActive(true); button.interactable = canHeight; }
        if (axisControls != null) foreach (var control in axisControls) if (control != null) control.SetActive(!motorCover);
        if (motorScaleControls != null) foreach (var control in motorScaleControls) if (control != null) control.SetActive(motorCover);
        if (fittingHint != null) fittingHint.text = motorCover
            ? "Общий масштаб — до и после установки. Высота — только после установки; зелёная ручка или кнопки."
            : "Красная — длина; зелёная — высота; синяя — ширина; жёлтая — вдоль луча. XYZ — все оси пропорционально.";
        bool canMove = _scales != null && _scales.CanMoveAlong(_id);
        if (alongButtons != null) foreach (var button in alongButtons) if (button != null) button.interactable = canMove;
        if (handles != null) foreach (var handle in handles)
            handle.gameObject.SetActive(((handle.Axis == 4 ? uniform : !uniform) ||
                (motorCover && handle.Axis == 1 && canHeight)) &&
                (handle.Axis != 3 || canMove));
        if (_target == null || _geometry == null) { HideGizmo(); return; }
        var t = _target.transform;
        var scale = PartSpawnAnimator.GetStableLocalScale(t);
        var size = Vector3.Scale(_geometry.LocalBounds.size, scale) * 1000f;
        if (_dragging == null || Time.unscaledTime >= _nextLabelTime)
        {
            dimensions.SetText("Д {0:1} / В {1:1} / Ш {2:1} мм • Сдвиг {3:1} мм",
                size.x, size.y, size.z, _scales.GetOffset(_id) * 1000f);
            _nextLabelTime = Time.unscaledTime + 0.1f;
        }
        gizmo.gameObject.SetActive(_showGizmo);
        gizmoLabel.text = _showGizmo ? "Ручки: вкл." : "Ручки: выкл.";
        if (!_showGizmo) return;
        gizmo.SetPositionAndRotation(t.position, t.rotation);
        for (int i = 0; i < handles.Length; i++)
        {
            var h = handles[i];
            int a = h.Axis;
            var point = _geometry.LocalBounds.center;
            if (a == 4) point = _geometry.LocalBounds.max;
            else if (a == 3) point.z -= _geometry.LocalBounds.extents.z;
            else point[a] += _geometry.LocalBounds.extents[a];
            var dir = t.TransformDirection(a == 4 ? Vector3.one.normalized : a == 3 ? Vector3.back : Axis(a));
            h.transform.position = t.TransformPoint(point) + dir * 0.035f;
            h.transform.rotation = a == 3 && t.parent != null ? t.parent.rotation : t.rotation;
        }
    }

    public bool BeginDrag(ArmCoverScaleHandle handle, int axis, out Vector3 direction,
        out float extent, out Vector3 startScale)
    {
        direction = Vector3.zero; extent = 1; startScale = Vector3.one;
        if (!_showGizmo || _dragging != null || _target == null || _geometry == null ||
            _target.IsGrabbed || PartSpawnAnimator.IsAnimating(_target.transform)) return false;
        if (axis == 3 && !_scales.CanMoveAlong(_id)) return false;
        if (axis >= 0 && axis <= 2 && _scales.UsesUniformScaleOnly(_id) &&
            (axis != 1 || !_scales.CanScaleHeight(_id))) return false;
        _dragging = handle;
        int scaleAxis = axis == 3 || axis == 4 ? 0 : axis;
        direction = axis == 3 && _target.transform.parent != null
            ? _target.transform.parent.TransformDirection(Vector3.right).normalized
            : _target.transform.TransformDirection(axis == 4 ? Vector3.one.normalized : Axis(scaleAxis)).normalized;
        startScale = _target.transform.localScale;
        _dragStartOffset = _target.transform.localPosition.x;
        // Position of the positive side relative to the root pivot determines handle travel.
        var edge = _geometry.LocalBounds.max[scaleAxis];
        var parent = _target.transform.parent;
        extent = Mathf.Max(0.005f, Mathf.Abs(edge) *
            (parent != null ? parent.TransformVector(_target.transform.localRotation * Axis(scaleAxis)).magnitude : 1f));
        if (axis == 4) extent = Mathf.Max(0.015f, _target.transform.TransformVector(_geometry.LocalBounds.max).magnitude);
        return true;
    }

    public void Drag(ArmCoverScaleHandle handle, Vector3 scale)
    {
        if (_dragging == handle && _scales.PreviewScale(_id, scale)) Refresh();
    }

    public void DragAlong(ArmCoverScaleHandle handle, float worldDistance)
    {
        if (_dragging != handle || _target == null) return;
        var parent = _target.transform.parent;
        float units = parent != null ? parent.TransformVector(Vector3.right).magnitude : 1f;
        if (_scales.PreviewOffset(_id, _dragStartOffset + worldDistance / Mathf.Max(0.001f, units))) Refresh();
    }

    public void EndDrag(ArmCoverScaleHandle handle)
    {
        if (_dragging != handle) return;
        _dragging = null;
        CommitDrag();
    }
    private void CommitDrag()
    {
        if (_bus != null && !string.IsNullOrEmpty(_id))
            _bus.Publish(new CommitArmCoverAdjustmentRequest { InstanceId = _id, Timestamp = DateTime.UtcNow });
    }
    private static Vector3 Axis(int axis) => axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
}
