using UnityEngine;
using UnityEngine.InputSystem;

public class DroneView : MonoBehaviour
{

    [SerializeField] private Collider mainTrigger;
    [SerializeField] private GameObject mainTriggerGO;

    private void Update()
    {

        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame)
        {
            //mainTrigger.enabled = false;
            mainTriggerGO.SetActive(false);
        }
        if (Keyboard.current != null && Keyboard.current.xKey.wasReleasedThisFrame)
        {

            //mainTrigger.enabled = true;
            mainTriggerGO.SetActive(true);
        }

    }
}
