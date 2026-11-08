using System;
using System.Collections.Generic;
using System.Linq;
using ColorPicker;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class PartPanelUI : MonoBehaviour
{
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
    [SerializeField] private float _moveStep = 0.05f;
    [SerializeField] private float _rotationStep = 1f;

    private InspectorService _inspector;
    private PartTransformAdjustmentService _partAdjustment;
    private IVisualPresetRepository _presetRepository;
    private IMaterialRegistry _materialsRepository;
    private Clean_AssemblySystem _assembly;
    private IEventBus _eventBus;
    private List<VisualPreset> _presets;
    private List<MaterialDefinition> _materials;
    private string _selectedPartInstanceId;
    private PartViewRegistry _views;
    private bool _slidersChanged;

    [Inject]
    public void Construct(InspectorService inspector, PartTransformAdjustmentService partAdjustment,
        IVisualPresetRepository presetRepository, IMaterialRegistry materialsRepository,
        IEventBus eventBus, Clean_AssemblySystem assembly, PartViewRegistry views)
    {
        _inspector = inspector;
        _partAdjustment = partAdjustment;
        _presetRepository = presetRepository;
        _materialsRepository = materialsRepository;
        _eventBus = eventBus;
        _assembly = assembly;
        _views = views;

        RefreshPresets();
        _presetRepository.Changed += RefreshPresets;
        _materials = _materialsRepository.GetAll().ToList();
        _materialDropdown.ClearOptions();
        _materialDropdown.AddOptions(_materials.Select(x => x.DisplayName).ToList());
        if (colorButtonImage == null) colorButtonImage = colorButton.GetComponent<Image>();

        _presetDropdown.onValueChanged.AddListener(OnPresetSelected);
        _materialDropdown.onValueChanged.AddListener(OnMaterialSelected);
        savePresetBtn.onClick.AddListener(OnSavePresetClicked);
        deletePresetBtn.onClick.AddListener(OnDeletePresetClicked);
        okNameBtn.onClick.AddListener(OnOkNameBtnClicked);
        colorButton.onClick.AddListener(OnColorClicked);
        _colorPicker.ColorSelectionChanged += OnColorChanged;
        _smoothnessSlider.onValueChanged.AddListener(OnSmoothnessChanged);
        _metalnessSlider.onValueChanged.AddListener(OnMetalnessChanged);
        _smoothnessEndDragHandler.Released += CommitSliders;
        _metalnessEndDragHandler.Released += CommitSliders;
        _inspector.Updated += OnUpdated;
        _inspector.Cleared += Hide;
        Hide();
        _inspector.RefreshSelectedPart();
    }

    private void OnDestroy()
    {
        if (_inspector == null) return;
        _presetRepository.Changed -= RefreshPresets;
        _inspector.Updated -= OnUpdated;
        _inspector.Cleared -= Hide;
        _presetDropdown.onValueChanged.RemoveListener(OnPresetSelected);
        _materialDropdown.onValueChanged.RemoveListener(OnMaterialSelected);
        savePresetBtn.onClick.RemoveListener(OnSavePresetClicked);
        deletePresetBtn.onClick.RemoveListener(OnDeletePresetClicked);
        okNameBtn.onClick.RemoveListener(OnOkNameBtnClicked);
        colorButton.onClick.RemoveListener(OnColorClicked);
        _colorPicker.ColorSelectionChanged -= OnColorChanged;
        _smoothnessSlider.onValueChanged.RemoveListener(OnSmoothnessChanged);
        _metalnessSlider.onValueChanged.RemoveListener(OnMetalnessChanged);
        _smoothnessEndDragHandler.Released -= CommitSliders;
        _metalnessEndDragHandler.Released -= CommitSliders;
    }

    private bool TryGetPart(out PartDomainState part)
    {
        part = null;
        return !string.IsNullOrEmpty(_selectedPartInstanceId) &&
            _assembly.TryGetPartDomainState(_selectedPartInstanceId, out part);
    }

    private void OnSmoothnessChanged(float value)
    {
        if (!TryGetPart(out _)) return;
        _slidersChanged = true;
        if (_views.TryGet(_selectedPartInstanceId, out var view)) view.ApplyPreviewSmoothness(value);
    }

    private void OnMetalnessChanged(float value)
    {
        if (!TryGetPart(out _)) return;
        _slidersChanged = true;
        if (_views.TryGet(_selectedPartInstanceId, out var view)) view.ApplyPreviewMetallness(value);
    }

    private void CommitSliders()
    {
        if (!_slidersChanged) return;
        _slidersChanged = false;
        if (!TryGetPart(out var part)) return;
        var visual = part.VisualProperties;
        visual.Smoothness = _smoothnessSlider.value;
        visual.Metallic = _metalnessSlider.value;
        ApplyVisual(visual);
    }

    private void OnMaterialSelected(int index)
    {
        if (index < 0 || index >= _materials.Count || !TryGetPart(out var part)) return;
        var visual = part.VisualProperties;
        visual.MaterialId = _materials[index].Id;
        ApplyVisual(visual);
    }

    private void OnColorClicked()
    {
        if (!TryGetPart(out _)) return;
        _colorPicker.transform.SetAsLastSibling();
        _colorPicker.gameObject.SetActive(!_colorPicker.gameObject.activeSelf);
    }

    private void OnColorChanged(Color value)
    {
        _colorPicker.gameObject.SetActive(false);
        if (!TryGetPart(out var part)) return;
        var visual = part.VisualProperties;
        visual.Color = value;
        ApplyVisual(visual);
    }

    private void ApplyVisual(PartVisualProperties visual)
    {
        if (TryGetPart(out _))
            _eventBus.Publish(new ApplyPartVisualCommand(_selectedPartInstanceId, visual));
    }

    private void OnPresetSelected(int index)
    {
        if (index > 0 && index <= _presets.Count) ApplyVisual(_presets[index - 1].Visual);
    }

    private void OnSavePresetClicked()
    {
        if (!TryGetPart(out _)) return;
        nameInputField.SetTextWithoutNotify("");
        enterNamePanel.SetActive(true);
    }

    private void OnOkNameBtnClicked()
    {
        if (TryGetPart(out var part))
        {
            var presetName = nameInputField.text.Trim();
            _presetRepository.Save(new VisualPreset
            {
                Id = Guid.NewGuid().ToString(),
                Name = string.IsNullOrEmpty(presetName) ? $"Пресет {_presets.Count + 1}" : presetName,
                Visual = part.VisualProperties
            });
        }
        enterNamePanel.SetActive(false);
    }

    private void OnDeletePresetClicked()
    {
        int index = _presetDropdown.value - 1;
        if (index < 0 || index >= _presets.Count) return;
        _presetRepository.Remove(_presets[index].Id);
    }

    private void RefreshPresets()
    {
        _presets = _presetRepository.GetAll().ToList();
        var options = _presets.Select(x => x.Name).ToList();
        options.Insert(0, "Пресеты");
        _presetDropdown.ClearOptions();
        _presetDropdown.AddOptions(options);
        _presetDropdown.SetValueWithoutNotify(0);
    }

    private void OnUpdated(InspectionContext context)
    {
        bool changedPart = _selectedPartInstanceId != context.Part.InstanceId;
        if (changedPart) CommitSliders();
        SetPart(context.Part.InstanceId);
        if (!TryGetPart(out var part)) { Hide(); return; }
        if (changedPart)
        {
            _colorPicker.gameObject.SetActive(false);
            enterNamePanel.SetActive(false);
        }
        var vm = context.Part;
        weight.text = vm.Weight.ToString();
        name.text = vm.Name;
        material.text = vm.Material;
        colorButtonImage.color = vm.Color;
        if (color != null) color.text = ColorUtility.ToHtmlStringRGB(vm.Color);
        if (changedPart) _presetDropdown.SetValueWithoutNotify(0);
        int index = _materials.FindIndex(x => x.Id == part.VisualProperties.MaterialId);
        _materialDropdown.SetValueWithoutNotify(index);
        _metalnessSlider.SetValueWithoutNotify(vm.Metallness);
        _smoothnessSlider.SetValueWithoutNotify(vm.Smoothness);
        panel.SetActive(true);
    }

    private void Hide()
    {
        CommitSliders();
        _selectedPartInstanceId = null;
        _colorPicker.gameObject.SetActive(false);
        enterNamePanel.SetActive(false);
        panel.SetActive(false);
    }

    public void SetPart(string instanceId) => _selectedPartInstanceId = instanceId;

    public void DeleteSelectedPart()
    {
        if (!TryGetPart(out var part)) return;
        string instanceId = part.InstanceId;
        CommitSliders();
        _eventBus.Publish(new Clean_DeletePartRequest { InstanceId = instanceId, Timestamp = DateTime.UtcNow });
        _inspector.RefreshSelectedPart();
    }

    public void DuplicateSelectedPart()
    {
        if (!TryGetPart(out var part)) return;
        string instanceId = part.InstanceId;
        CommitSliders();
        _eventBus.Publish(new Clean_DuiblicatePartRequest { InstanceId = instanceId, Timestamp = DateTime.UtcNow });
    }

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

    public void Move(AdjustmentAxis axis, bool positive)
    {
        if (TryGetPart(out _))
            _partAdjustment.Move(_selectedPartInstanceId, GetAxis(axis) * (positive ? _moveStep : -_moveStep));
    }

    public void Rotate(AdjustmentAxis axis, bool positive)
    {
        if (TryGetPart(out _))
            _partAdjustment.Rotate(_selectedPartInstanceId, GetAxis(axis) * (positive ? _rotationStep : -_rotationStep));
    }

    public void ResetRotation()
    {
        if (TryGetPart(out _)) _partAdjustment.ResetRotation(_selectedPartInstanceId);
    }

    public void ResetPosition()
    {
        if (TryGetPart(out _)) _partAdjustment.ResetPosition(_selectedPartInstanceId);
    }

    private static Vector3 GetAxis(AdjustmentAxis axis) => axis switch
    {
        AdjustmentAxis.X => Vector3.right,
        AdjustmentAxis.Y => Vector3.up,
        AdjustmentAxis.Z => Vector3.forward,
        _ => Vector3.zero
    };
}
