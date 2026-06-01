using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class PartPanelUI : MonoBehaviour
{
    InspectorService _inspector;
    AddressablesAssetService _assets;


    [SerializeField] private TMP_Text weight;
    [SerializeField] private TMP_Text material;
    [SerializeField] private TMP_Text color;
    [SerializeField] private TMP_Text name;
    [SerializeField] private Image colorImage;

    [Inject]
    public void Construct(InspectorService inspector, AddressablesAssetService assetService)
    {
        _inspector = inspector;
        _assets = assetService;
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

    private async void Render(PartViewModel vm)
    {
        // цвет, материал, вес
        Debug.Log($"RRRRRRRRRRRender  PartPanelUI {this}");

        Debug.Log($"Part  {vm.Id} HAS Material {vm.Material} Color {vm.Color} Weight {vm.Weight}");

        weight.text = vm.Weight.ToString();
        name.text = vm.Name;
        var mat = await _assets.Load<Material>(vm.Material) ;  
        colorImage.color = mat.color;
        material.text = mat.name ;  
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}