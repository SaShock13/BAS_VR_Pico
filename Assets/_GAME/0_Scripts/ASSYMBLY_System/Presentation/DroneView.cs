using UnityEngine;

public class DroneView : MonoBehaviour
{

    [SerializeField] private Collider mainTrigger;
    [SerializeField] private GameObject mainTriggerGO;

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.X))
        {
            //mainTrigger.enabled = false;
            mainTriggerGO.SetActive(false);
        }
        if (Input.GetKeyUp(KeyCode.X))
        {

            //mainTrigger.enabled = true;
            mainTriggerGO.SetActive(true);
        }

    }
}
