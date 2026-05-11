using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HoverOnlySocketInteractor : XRSocketInteractor
{
    public override bool CanSelect(IXRSelectInteractable interactable)
    {
        return false;
    }
}