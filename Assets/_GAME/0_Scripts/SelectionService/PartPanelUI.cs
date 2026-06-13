using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class PartPanelUI : MonoBehaviour 
{
    InspectorService _inspector;
    AddressablesAssetService _assets;
    PartTransformAdjustmentService _partAdjustment;


    [SerializeField] private TMP_Text weight;
    [SerializeField] private TMP_Text material;
    [SerializeField] private TMP_Text color;
    [SerializeField] private TMP_Text name;
    [SerializeField] private Image colorImage;
    [SerializeField] private GameObject panel;




    [SerializeField]
    private float _moveStep = 0.05f; // 50 мм

    [SerializeField]
    private float _rotationStep = 1f; // 5 градус

    private string _selectedPartInstanceId;

    private PartTransformAdjustmentService _adjustmentService;

    public void MoveXPlus() => Move(AdjustmentAxis.X, true);
    public void MoveXMinus() => Move(AdjustmentAxis.X, false);

    public void MoveYPlus() => Move(AdjustmentAxis.Y, true);
    public void MoveYMinus() => Move(AdjustmentAxis.Y, false);

    public void MoveZPlus() => Move(AdjustmentAxis.Z, true);
    public void MoveZMinus() => Move(AdjustmentAxis.Z, false);

    public void RotateXPlus() => Rotate(AdjustmentAxis.X, true);
    public void RotateXMinus() => Rotate(AdjustmentAxis.X, false);

    public void RotateYPlus() => Rotate(AdjustmentAxis.Y, true);
    public void RotateYMinus() => Rotate(AdjustmentAxis.Y, false);

    public void RotateZPlus() => Rotate(AdjustmentAxis.Z, true);
    public void RotateZMinus() => Rotate(AdjustmentAxis.Z, false);


   

    [Inject]
    public void Construct(
        InspectorService inspector,
        PartTransformAdjustmentService partAdjustment,
        AddressablesAssetService assetService)
    {
        _inspector = inspector;
        _partAdjustment = partAdjustment;
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





    private void OnUpdated(InspectionContext context)
    {
        panel.SetActive(true);


        Render(context.Part);

        if (context.IsRootPart)
        {
            // можно добавить бейдж "ROOT PART"
        }
    }

    private async void Render(PartViewModel vm)
    {
        // цвет, материал, вес
        Debug.Log($"RRRRRRRRRRRender  PartPanelUI {this}");

        Debug.Log($"Part  {vm.InstanceId} HAS Material {vm.Material} Color {vm.Color} Weight {vm.Weight}");

        SetPart(vm.InstanceId);

        weight.text = vm.Weight.ToString();
        name.text = vm.Name;

        //var mat = await _assets.Load<Material>(vm.Material) ;  
        
        colorImage.color = vm.Color;
        material.text = vm.Material ;  
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }


    public void SetPart(string instanceId)
    {
        _selectedPartInstanceId = instanceId;
    }

    public void Move(
        AdjustmentAxis axis,
        bool positive)
    {

        Debug.Log($"uuuuuuuuuuMove {this}");
        if(string.IsNullOrEmpty( _selectedPartInstanceId )) return;
        Vector3 direction = GetAxis(axis);

        if (!positive)
            direction = -direction;

        _partAdjustment.Move(
            _selectedPartInstanceId,
            direction * _moveStep);
    }

    public void Rotate(
        AdjustmentAxis axis,
        bool positive)
    {

        Debug.Log($"uuuuuuuuuRotate {this}");
        if(string.IsNullOrEmpty( _selectedPartInstanceId )) return;
        Vector3 euler = GetAxis(axis);

        if (!positive)
            euler = -euler;

        _partAdjustment.Rotate(
            _selectedPartInstanceId,
            euler * _rotationStep);
    }

    private static Vector3 GetAxis(
        AdjustmentAxis axis)
    {
        return axis switch
        {
            AdjustmentAxis.X => Vector3.right,
            AdjustmentAxis.Y => Vector3.up,
            AdjustmentAxis.Z => Vector3.forward,
            _ => Vector3.zero
        };
    }

    public void ResetRotation()
    {
        _partAdjustment.ResetRotation(_selectedPartInstanceId);
    }

    public void ResetPosition()
    {
        _partAdjustment.ResetPosition(_selectedPartInstanceId);
    }

}