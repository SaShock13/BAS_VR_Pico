using UnityEngine;

public class SocketView : MonoBehaviour
{
    [SerializeField]
    private string _socketId;

    public string SocketId => _socketId;

    public Transform AttachPoint;
}