using UnityEngine;
using Zenject;

public class PartPanelUI : MonoBehaviour
{
    InspectorService _inspector;

    [Inject]
    public void Construct(InspectorService inspector)
    {
        _inspector = inspector;

        _inspector.Updated += OnUpdated;
        _inspector.Cleared += Hide;
    }

    //??
    public void Bind(InspectorService inspector)
    {
        inspector.Updated += OnUpdated;
        inspector.Cleared += Hide;
    }

    private void OnUpdated(InspectionContext ctx)
    {
        gameObject.SetActive(true);

        Render(ctx.Part);

        if (ctx.IsRootPart)
        {
            // можно добавить бейдж "ROOT PART"
        }
    }

    private void Render(PartViewModel vm)
    {
        // цвет, материал, вес
        Debug.Log($"RRRRRRRRRRRender  PartPanelUI {this}");

        Debug.Log($"Part  {vm.Id} HAS Material {vm.Material} Color {vm.Color} Weight {vm.Weight}");
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}