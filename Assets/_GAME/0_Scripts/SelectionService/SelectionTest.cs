using UnityEngine;
using Zenject;

public class SelectionTest : MonoBehaviour
{
    public string PartId;
    public ISelectionService Selection;


    [Inject]
    public void Construct(ISelectionService selection)
    {
        Selection = selection;
    }


    public void Click()
    {
        Selection.Select(
            new SelectionTarget(SelectionType.Part, PartId));
    }


}
