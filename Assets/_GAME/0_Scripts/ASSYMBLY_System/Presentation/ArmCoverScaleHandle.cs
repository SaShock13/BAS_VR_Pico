using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class ArmCoverScaleHandle : XRBaseInteractable
{
    [SerializeField, Range(0, 4)] private int axis;
    private ArmCoverAdjustmentView _owner;
    private IXRSelectInteractor _interactor;
    private Vector3 _origin;
    private Vector3 _direction;
    private Vector3 _startScale;
    private float _worldExtent;

    public int Axis => axis;
    public void Configure(ArmCoverAdjustmentView owner) => _owner = owner;

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        if (_owner == null || !_owner.BeginDrag(this, axis, out _direction, out _worldExtent, out _startScale)) return;
        _interactor = args.interactorObject;
        _origin = _interactor.GetAttachTransform(this).position;
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);
        if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic || _interactor == null || _owner == null) return;
        float distance = Vector3.Dot(_interactor.GetAttachTransform(this).position - _origin, _direction);
        if (axis == 3) { _owner.DragAlong(this, distance); return; }
        if (axis == 4)
        {
            _owner.Drag(this, ArmCoverScaleService.UniformScale(_startScale, 1f + distance / _worldExtent));
            return;
        }
        var scale = _startScale;
        scale[axis] += distance / _worldExtent;
        _owner.Drag(this, scale);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        _interactor = null;
        _owner?.EndDrag(this);
        base.OnSelectExited(args);
    }

    protected override void OnDisable()
    {
        _interactor = null;
        _owner?.EndDrag(this);
        base.OnDisable();
    }
}
