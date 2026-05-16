using System.Threading.Tasks;
using UnityEngine;

public interface IPartFactory
{
    Task<GameObject> CreateFromAddressables(PartConfig config, Vector3 position, Quaternion rotation);
    GameObject Create(PartConfig config, Vector3 position, Quaternion rotation);
}
