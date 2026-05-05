using UnityEngine;
using Zenject;

public class UndoRedoView : MonoBehaviour
{
    private Clean_AssemblySystem _assemblySystem;

    [Inject]
    public void Construct(Clean_AssemblySystem assemblySystem)
    {
        _assemblySystem = assemblySystem;
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.KeypadMinus))
            _assemblySystem.Undo();

        if (Input.GetKeyDown(KeyCode.KeypadPlus))
            _assemblySystem.Redo();
    }


}
