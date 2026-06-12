using UnityEngine;
using Zenject;

public class TestHints : MonoBehaviour
{
    [Inject] IHintScenarioController _hintScenario;
    [Inject] ISelectionService _selectionService;
    [Inject] PartViewRegistry _viewRegistry;

    bool isHintStarted = false;


    // Update is called once per frame
    void Update()
    {

        //if (Input.GetKeyDown(KeyCode.Alpha1))
        //{
        //    if (!isHintStarted)
        //    {
        //        _hintScenario.StartScenario(
        //            new HintContext(
        //        "Установите ESC на монтажную площадку",
        //        ReturnTestHintTarget()));
        //        isHintStarted = true;
        //    }
        //    else
        //    {
        //        isHintStarted = false;
        //        _hintScenario.StopScenario();
        //    }

        //}


    }

    Transform ReturnTestHintTarget()
    {
        var selectedId = _selectionService.Current.Value.PartId;
        _viewRegistry.TryGet(selectedId,out var selectedView);
        
        return selectedView!=null? selectedView.transform : transform; 
    }
}
