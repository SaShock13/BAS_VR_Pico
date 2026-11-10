using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public sealed class GarageDronePanel : MonoBehaviour
{
    [SerializeField] private TMP_Text droneName;
    [SerializeField] private TMP_Text weight;
    [SerializeField] private Button returnButton;
    [SerializeField] private FlightModeView flightMode;
    [SerializeField] private FlightEnvironmentSelectionView environmentPicker;

    private ISelectionService _selection;
    private Clean_AssemblySystem _assembly;
    private IGarageService _garage;

    [Inject]
    public void Construct(ISelectionService selection, Clean_AssemblySystem assembly, IGarageService garage)
    {
        _selection = selection;
        _assembly = assembly;
        _garage = garage;
        _selection.Changed += Render;
        Render(_selection.Current);
    }

    private void Render(SelectionTarget? target)
    {
        DroneDomainState drone = null;
        var saved = target.HasValue && _assembly.TryGetDroneForPart(target.Value.PartId, out drone)
            ? _garage.GetByDroneId(drone.InstanceId)
            : null;
        bool hasDrone = saved != null;
        droneName.text = hasDrone ? _assembly.GetDroneName(drone.InstanceId) : "Дрон не выбран";
        weight.text = hasDrone ? $"Масса: {_assembly.GetComputed(drone.InstanceId).TotalMass:0.##} кг" : string.Empty;
        returnButton.interactable = hasDrone;
        flightMode.SetLaunchAvailable(hasDrone);
        if (!hasDrone) environmentPicker.Close();
    }

    private void OnDestroy()
    {
        if (_selection != null) _selection.Changed -= Render;
    }
}
