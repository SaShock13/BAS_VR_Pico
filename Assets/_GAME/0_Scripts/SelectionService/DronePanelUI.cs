using System;
using TMPro;
using UnityEngine;
using Zenject;

public class DronePanelUI : MonoBehaviour
{

    InspectorService _inspector;
    Clean_AssemblySystem _assembly;



    [SerializeField] private TMP_Text weight ;
    [SerializeField] private TMP_Text name ;
    [SerializeField] private GameObject panel ;

    [Inject]
    public void Construct(InspectorService inspector, Clean_AssemblySystem assembly)
    {
        _inspector = inspector;
        _assembly = assembly;
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

        panel.SetActive(true);

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
        weight.text = vm.TotalWeight.ToString();
        name.text = vm.Name;
        // вес, моторы и т.д.
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}