using System;
using UnityEngine;
using Zenject;

public class DronePanelUI : MonoBehaviour
{

    InspectorService _inspector;

    [Inject]
    public void Construct(InspectorService inspector)
    {
        _inspector = inspector;
        _inspector.Updated += OnUpdated;

        _inspector.Cleared += Hide;

    }

    //???
    public void Bind(InspectorService inspector)
    {
        inspector.Updated += OnUpdated;
        inspector.Cleared += Hide;
    }

    private void OnUpdated(InspectionContext ctx)
    {

        gameObject.SetActive(true);

        if(ctx.Drone == null) RenderEmpty();
        else Render(ctx.Drone);
    }

    private void RenderEmpty()
    {
        Debug.Log($"RRRRRRRR Render EMPTY DRONE UI {this}");        
    }

    private void Render(DroneViewModel vm)
    {        
        Debug.Log($"RRRRRRRRRRender  DronePanelUI {this}");
        Debug.Log($"Part  Name {vm.Name} HAS  ID {vm.Id}  TotalWeight {vm.TotalWeight}");
        // вес, моторы и т.д.
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}