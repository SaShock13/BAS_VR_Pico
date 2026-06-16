using System;
using System.Collections.Generic;
using System.Linq;
using ColorPicker;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Button = UnityEngine.UI.Button;

public class PartPanelUI : MonoBehaviour 
{
    InspectorService _inspector;
    AddressablesAssetService _assets;
    PartTransformAdjustmentService _partAdjustment;
    PartViewRegistry _partViewRegistry;
    IVisualPresetRepository _presetRepository;
    IMaterialRegistry _materialsRepository;
    Clean_AssemblySystem _assembly;
    IEventBus _eventBus;

    [SerializeField] private TMP_Text weight;
    [SerializeField] private TMP_Text material;
    [SerializeField] private TMP_Text color;
    [SerializeField] private TMP_Text name;
    [SerializeField] private TMP_Dropdown _presetDropdown;
    [SerializeField] private TMP_Dropdown _materialDropdown;
    [SerializeField] private Button colorButton;
    [SerializeField] private Button savePresetBtn;
    [SerializeField] private Button deletePresetBtn;
    [SerializeField] private Image colorButtonImage;
    [SerializeField] private Slider _smoothnessSlider;
    [SerializeField] private SliderEndDragHandler _smoothnessEndDragHandler;
    [SerializeField] private Slider _metalnessSlider;
    [SerializeField] private SliderEndDragHandler _metalnessEndDragHandler;
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject enterNamePanel;
    [SerializeField] private Button okNameBtn;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private NewColorPicker _colorPicker;


    private List<VisualPreset> _presets;
    private List<MaterialDefinition> _materials;

    [SerializeField]
    private float _moveStep = 0.05f; // 50 мм

    [SerializeField]
    private float _rotationStep = 1f; // 5 градус

    private string _selectedPartInstanceId;
    private PartDomainState _selectedDomain;
    private DronePartView _selectedView;

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
        IVisualPresetRepository presetRepository,
        IMaterialRegistry materialsRepository,
        PartViewRegistry partViewRegistry,
        IEventBus eventBus,
        Clean_AssemblySystem assembly,
        AddressablesAssetService assetService)
    {
        _inspector = inspector;
        _partAdjustment = partAdjustment;
        _assets = assetService;
        _eventBus = eventBus;
        _assembly = assembly;
        _presetRepository = presetRepository;
        _materialsRepository = materialsRepository;
        _partViewRegistry = partViewRegistry;
        _inspector.Updated += OnUpdated;
        _inspector.Cleared += Hide;
        _presetDropdown.captionText.text = "Пресеты";
        _presetDropdown.onValueChanged.AddListener(OnPresetSelected);
        savePresetBtn.onClick.AddListener(OnSavePresetClicked);
        deletePresetBtn.onClick.AddListener(OnDeletePresetClicked);
        okNameBtn.onClick.AddListener(OnOkNameBtnClicked);

    }

    private void OnDeletePresetClicked()
    {
        int index = _presetDropdown.value - 1;  // Первый пункт выпадающего списка - Загшлушка.

        if (index < 0 || index >= _presets.Count)
            return;

        var selectedPreset = _presets[index];

        _presetRepository.Remove(selectedPreset.Id);

        RefreshPresets();

        //_presetDropdown.SetValueWithoutNotify(0);
    }

    private void OnOkNameBtnClicked()
    {
        SavePreset(nameInputField.text);
        enterNamePanel.SetActive(false);
    }

    private void OnSavePresetClicked()
    {
        enterNamePanel.SetActive(true);
    }


    private void SavePreset(string presetName = "")
    {
        int number = _presetRepository.GetAll().Count() + 1;
        string newName = $"Пресет {number}";

        if(!string.IsNullOrEmpty(presetName))
        {
            newName = presetName;
        }

        var newPreset = new VisualPreset()
        {
            Id = Guid.NewGuid().ToString(),
            Name = newName,
            Visual = _selectedDomain.VisualProperties
        };

              


        _presetRepository.Save(newPreset);
        RefreshPresets();
    }

    private void RefreshPresets()
    {
        _presets = _presetRepository.GetAll().ToList();

        _presetDropdown.ClearOptions();



        var options = _presets
            .Select(x => x.Name)
            .ToList();


        options.Insert(0, "Пресеты");
        _presetDropdown.AddOptions(options);

        
    }

    private void Start()
    {
        RefreshPresets();
        FillMaterials();
        _materialDropdown.onValueChanged.AddListener(OnMateriaLSelected);
        _colorPicker.gameObject.SetActive(false);
        _colorPicker.ColorSelectionChanged += OnColorChanged;
        _smoothnessSlider.onValueChanged.AddListener (OnSmoothnessChanged);
        _smoothnessEndDragHandler.Released += OnSmoothnessReleased;
        //_smoothnessSlider..AddListener (OnSmoothnessChanged);
        _metalnessSlider.onValueChanged.AddListener(OnMetallnessChanged);
        _metalnessEndDragHandler.Released += OnMetalnessReleased;
        //_metalnessSlider.onValueChanged.AddListener(OnMetallnessChanged);
        colorButtonImage = colorButton.GetComponent<Image>();
        colorButton.onClick.AddListener(OnColorClicked);
    }

    private void OnMetallnessChanged(float value)
    {
        //ПРЯМОЕ ПЕРЕКЛЮЧЕНИЕ ПАРАМЕТРА
        _selectedView.ApplyPreviewMetallness(value);

    }

    private void OnSmoothnessChanged(float value)
    {

        _selectedView.ApplyPreviewSmoothness(value);
        //ПРЯМОЕ ПЕРЕКЛЮЧЕНИЕ ПАРАМЕТРА
    }

    private void OnMetalnessReleased()
    {
        var currentVisual = _selectedDomain.VisualProperties;

        currentVisual.Metallic = _metalnessSlider.value;


        _eventBus.Publish(new ApplyPartVisualCommand(_selectedPartInstanceId, currentVisual));
    }

    private void OnSmoothnessReleased()
    {
        var currentVisual = _selectedDomain.VisualProperties;

        currentVisual.Smoothness = _smoothnessSlider.value;


        _eventBus.Publish(new ApplyPartVisualCommand(_selectedPartInstanceId, currentVisual));
    }


    private void OnColorClicked()
    {

        _colorPicker.gameObject.SetActive(true);
    }

    private void OnColorChanged(Color color)
    {
        Debug.Log($"New color {color}");
        _colorPicker.gameObject.SetActive(false);
        ApplyColor(color);
    }

    private void ApplyColor(Color color)
    {
        var currentVisual = _selectedDomain.VisualProperties;

        currentVisual.Color = color;


        _eventBus.Publish(new ApplyPartVisualCommand(_selectedPartInstanceId, currentVisual));                      
    }

    private void FillMaterials()
    {
        _materials = _materialsRepository.GetAll().ToList();
        _materialDropdown.ClearOptions();



        var options = _materials
            .Select(x => x.DisplayName)
            .ToList();


        _materialDropdown.AddOptions(options);
    }

    private void OnMateriaLSelected(int index)
    {
        var definition = _materials[index];


        ApplyMaterial(definition.Id);
    }

    private void ApplyMaterial(string id)
    {
        var currentVisual = _selectedDomain.VisualProperties;

        currentVisual.MaterialId = id;


        _eventBus.Publish(new ApplyPartVisualCommand(_selectedPartInstanceId, currentVisual) );
    }

    private void OnPresetSelected(int index)
    {

        if (index == 0) return;
        Debug.Log($"ppppppppindex {index}");
        VisualPreset preset =
            _presets[index-1];

        Debug.Log(
            $"pppppppppSelected preset: {preset.Name}");

        ApplyPreset(preset);
    }

    private void ApplyPreset(VisualPreset preset)
    {
        _eventBus.Publish(new ApplyPartVisualCommand(_selectedPartInstanceId, preset.Visual) );
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
        SetPart(vm.InstanceId);
        // цвет, материал, вес
        Debug.Log($"RRRRRRRRRRRender  PartPanelUI {this}");

        Debug.Log($"Part  {vm.InstanceId} HAS Material {vm.Material} Color {vm.Color} Weight {vm.Weight}");

        weight.text = vm.Weight.ToString();
        name.text = vm.Name;

        colorButtonImage.color = vm.Color;

        _presetDropdown.value = 0;

        int currentMaterialIndex = _materials.FindIndex(    x => x.Id == _selectedDomain.VisualProperties.MaterialId);
        _materialDropdown.value = currentMaterialIndex;

        colorButtonImage.color = vm.Color;
        _metalnessSlider.value = vm.Metallness;
        _smoothnessSlider.value = vm.Smoothness;
        material.text = vm.Material ;  
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }


    public void SetPart(string instanceId)
    {
        _selectedPartInstanceId = instanceId;
        _selectedDomain = _assembly.GetPartDomainState(instanceId);
        _partViewRegistry.TryGet(instanceId, out _selectedView);
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