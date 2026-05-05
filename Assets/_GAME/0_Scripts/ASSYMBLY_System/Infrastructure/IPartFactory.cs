using UnityEngine;

public interface IPartFactory
{
    GameObject Create(PartConfig config, Vector3 position, Quaternion rotation);
}
