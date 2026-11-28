using UnityEngine;
using UnityEngine.InputSystem;
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
        if (Keyboard.current != null && Keyboard.current.numpadMinusKey.wasPressedThisFrame)
            _assemblySystem.Undo();

        if (Keyboard.current != null && Keyboard.current.numpadPlusKey.wasPressedThisFrame)
            _assemblySystem.Redo();
    }


}
