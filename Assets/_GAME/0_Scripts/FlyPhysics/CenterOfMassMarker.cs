using UnityEngine;

public class CenterOfMassMarker : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawSphere(transform.position, 0.03f);

        Gizmos.DrawLine(
            transform.position,
            transform.position + Vector3.up * 0.1f);
    }
}
