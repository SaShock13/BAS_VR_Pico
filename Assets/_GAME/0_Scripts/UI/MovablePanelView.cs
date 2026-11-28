using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
public sealed class MovablePanelView : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable grab;
    [SerializeField] private Rigidbody body;
    [SerializeField] private Transform defaultParent;
    [SerializeField] private Vector3 defaultPosition;
    [SerializeField] private Quaternion defaultRotation = Quaternion.identity;
    [SerializeField] private Vector3 defaultScale = Vector3.one;
    [SerializeField] private MenuTooltip gripTooltip;

    private void OnEnable()
    {
        if (!grab) return;
        grab.hoverEntered.AddListener(OnHoverEntered);
        grab.hoverExited.AddListener(OnHoverExited);
        grab.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDisable()
    {
        if (grab)
        {
            grab.hoverEntered.RemoveListener(OnHoverEntered);
            grab.hoverExited.RemoveListener(OnHoverExited);
            grab.selectEntered.RemoveListener(OnGrabbed);
        }
        if (gripTooltip) gripTooltip.CancelHover();
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (gripTooltip && !grab.isSelected)
            gripTooltip.BeginHover(args.interactorObject.transform.GetInstanceID());
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (gripTooltip) gripTooltip.EndHover(args.interactorObject.transform.GetInstanceID());
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (gripTooltip) gripTooltip.CancelHover();
    }

    public void ResetPosition()
    {
        // Release first so the grab transformer cannot overwrite the reset pose.
        if (grab.isSelected && grab.interactionManager == null) return;
        while (grab.isSelected)
            grab.interactionManager.SelectCancel(grab.interactorsSelecting[0], grab);
        transform.SetParent(defaultParent, false);
        transform.SetLocalPositionAndRotation(defaultPosition, defaultRotation);
        transform.localScale = defaultScale;
        body.position = transform.position;
        body.rotation = transform.rotation;
    }
}
