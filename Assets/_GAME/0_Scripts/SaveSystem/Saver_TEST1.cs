using UnityEngine;
using Zenject;

public class Saver_TEST1 : MonoBehaviour
{

    Clean_AssemblySystem _assembly;

    [Inject]
    public void Construct(Clean_AssemblySystem assembly)
    {
        _assembly = assembly;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
            _assembly.Save();

        if (Input.GetKeyDown(KeyCode.F9))
            _assembly.Load();
    }

}
