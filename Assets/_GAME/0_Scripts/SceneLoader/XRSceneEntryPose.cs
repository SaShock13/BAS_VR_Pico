using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(XROrigin))]
public sealed class XRSceneEntryPose : MonoBehaviour
{
    private IEnumerator Start()
    {
        var rig = GetComponent<XROrigin>();
        var floorPosition = rig.Origin.transform.position;
        var forward = rig.Origin.transform.forward;

        // Let the pose driver apply the first headset pose before removing its horizontal offset.
        float deadline = Time.realtimeSinceStartup + 1f;
        do
        {
            yield return null;
            var headset = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (headset.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked)
                break;
        }
        while (Time.realtimeSinceStartup < deadline);

        if (rig.Camera == null) yield break;
        var controller = rig.Origin.GetComponent<CharacterController>();
        bool controllerEnabled = controller != null && controller.enabled;
        if (controllerEnabled) controller.enabled = false;
        rig.MatchOriginUpCameraForward(Vector3.up, forward);
        var height = rig.Camera.transform.position.y - rig.Origin.transform.position.y;
        rig.MoveCameraToWorldLocation(floorPosition + Vector3.up * height);
        if (controllerEnabled) controller.enabled = true;
    }
}
