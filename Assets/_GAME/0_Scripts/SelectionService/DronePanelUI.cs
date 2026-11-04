using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class DronePanelUI : MonoBehaviour
{
    private InspectorService _inspector;
    private DroneValidationStateService _validation;
    private IEventBus _bus;
    private string _droneId;

    [SerializeField] private TMP_Text weight;
    [SerializeField] private TMP_Text name;
    [SerializeField] private GameObject panel;
    [SerializeField] private Button validateButton;
    [SerializeField] private TMP_Text validationStatus;
    [SerializeField] private TMP_Text validationDetails;

    [Inject]
    public void Construct(InspectorService inspector, DroneValidationStateService validation, IEventBus bus)
    {
        Bind(inspector);
        _validation = validation;
        _bus = bus;
        _validation.Changed += RenderValidation;
        validateButton.onClick.AddListener(ValidateSelectedDrone);
        Hide();
    }

    public void Bind(InspectorService inspector)
    {
        if (_inspector != null)
        {
            _inspector.Updated -= OnUpdated;
            _inspector.Cleared -= Hide;
        }
        _inspector = inspector;
        _inspector.Updated += OnUpdated;
        _inspector.Cleared += Hide;
    }

    private void OnUpdated(InspectionContext context)
    {
        if (context.Drone == null) { Hide(); return; }
        _droneId = context.Drone.Id;
        weight.text = context.Drone.TotalWeight.ToString();
        name.text = context.Drone.Name;
        panel.SetActive(true);
        RenderValidation();
    }

    private void ValidateSelectedDrone()
    {
        if (!string.IsNullOrEmpty(_droneId))
            _bus.Publish(new ValidateDroneRequest { DroneId = _droneId });
    }

    private void RenderValidation()
    {
        var result = _validation?.GetResult(_droneId);
        validateButton.interactable = !string.IsNullOrEmpty(_droneId);
        validationStatus.text = result == null ? "Валидация не пройдена: требуется проверка"
            : result.StatusText;
        validationStatus.color = result?.IsReady == true ? new Color(0.25f, 0.85f, 0.4f)
            : new Color(1f, 0.7f, 0.3f);
        validationDetails.text = result?.GetDetailsText() ?? string.Empty;
    }

    private void Hide()
    {
        _droneId = null;
        weight.text = string.Empty;
        name.text = string.Empty;
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_inspector != null)
        {
            _inspector.Updated -= OnUpdated;
            _inspector.Cleared -= Hide;
        }
        if (_validation != null) _validation.Changed -= RenderValidation;
        if (validateButton != null) validateButton.onClick.RemoveListener(ValidateSelectedDrone);
    }
}
