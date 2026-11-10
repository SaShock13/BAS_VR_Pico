using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Zenject;

public class GarageSceneBootstrap : MonoBehaviour
{
    [SerializeField]
    private Transform[] _slots;
    [SerializeField]
    private float _rotationSpeed = 2;
     [SerializeField]
    private bool isDronesRotating = true;


    [Inject]
    private IGarageService _garage;

    [Inject]
    private Clean_AssemblySystem _assembly;

    [SerializeField] private XRBaseInteractor[] _interactors;
    [SerializeField] private InputActionReference leftTriggerAction;
    [SerializeField] private InputActionReference rightTriggerAction;
    [SerializeField] private ConfirmationView clearGarageConfirmation;

    [Inject]
    public ISelectionService Selection;

    [Inject]
    public INotificationService _notificationService;

    [Inject]
    private PartHighlightService _highlightService;

    [Inject]
    private IEventBus _eventBus;
    private bool _loadingGarage = true;
    private bool _leaving;

    public void ReturnToAssembly() => Leave(MenuNavigationService.ConstructorSceneName);
    public void ReturnToMainMenu() => Leave("BootMenu");

    public void ReturnSelectedDroneToAssembly()
    {
        if (_loadingGarage || _leaving || !TryGetSelectedDrone(out var drone, out var saved)) return;
        try
        {
            if (!_garage.MoveToAssembly(saved.GarageId))
            {
                _notificationService.Warning("Не удалось вернуть дрон в сборку");
                return;
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception, this);
            _notificationService.Warning("Не удалось вернуть дрон в сборку");
            return;
        }
        Selection.Clear();
        _assembly.RemoveDrone(drone.InstanceId);
        _notificationService.Info("Дрон возвращён в сборку");
    }

    private bool TryGetSelectedDrone(out DroneDomainState drone, out GarageDroneData saved)
    {
        drone = null;
        saved = null;
        var selected = Selection.Current;
        if (!selected.HasValue || !_assembly.TryGetDroneForPart(selected.Value.PartId, out drone))
            return false;
        saved = _garage.GetByDroneId(drone.InstanceId);
        return saved != null;
    }

    private void Leave(string sceneName)
    {
        if (_loadingGarage || _leaving) return;
        _leaving = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single) != null;
    }
    
    private void Start()
    {
        
        LoadGarage();

    }

    private void OnEnable()
    {
        if (leftTriggerAction != null) leftTriggerAction.action.performed += OnLeftTrigger;
        if (rightTriggerAction != null) rightTriggerAction.action.performed += OnRightTrigger;
        foreach (var interactor in _interactors)
        {
            interactor.selectEntered.AddListener(OnSelectEntered);
            interactor.hoverEntered.AddListener(OnHoverEnter);
            interactor.hoverExited.AddListener(OnHoverExit);

        }
    }

    private async System.Threading.Tasks.Task RestoreGarage()
    {

        var drones = _garage.GetAll();

        int count = Mathf.Min(
            drones.Count,
            _slots.Length);
        Transform droneTransform;
        var loadedNames = new System.Collections.Generic.List<(string id, string name)>();

        for (int i = 0; i < count; i++)
        {


            droneTransform =  await _assembly.SpawnAssembly(
                drones[i].Assembly,
                _slots[i].position);

            droneTransform.SetParent(_slots[i]);

            loadedNames.Add((droneTransform.GetComponent<DronePartView>().InstanceId, drones[i].metaData?.Name));

            // todo ОТКЛЮЧИТЬ ВОЗМОЖНОСТЬ ХВАТАНИЯ ДРОНОВ (может Interactable.trackPosition = false или как то еще). НО ХОВЕР И ТРИГГЕР ОСТАВИТЬ ДЛЯ инспектора

        }
        _assembly.FinishBatchLoad();
        foreach (var item in loadedNames)
            if (!string.IsNullOrWhiteSpace(item.name)) _assembly.RenameDrone(item.id, item.name);



    }

    private void OnHoverExit(HoverExitEventArgs arg0)
    {
        _highlightService.Exit();
    }

    private void OnHoverEnter(HoverEnterEventArgs arg0)
    {
        var view = arg0.interactableObject.transform.GetComponent<DronePartView>();
        _highlightService.Enter(view);
    }

    private void OnDisable()
    {
        if (leftTriggerAction != null) leftTriggerAction.action.performed -= OnLeftTrigger;
        if (rightTriggerAction != null) rightTriggerAction.action.performed -= OnRightTrigger;
        foreach (var interactor in _interactors)
        {
            interactor.selectEntered.RemoveListener(OnSelectEntered);
            interactor.hoverEntered.RemoveListener(OnHoverEnter);
            interactor.hoverExited.RemoveListener(OnHoverExit);
        }
    }

    private void OnLeftTrigger(InputAction.CallbackContext _) => SelectHoveredDrone(_interactors[0]);
    private void OnRightTrigger(InputAction.CallbackContext _) => SelectHoveredDrone(_interactors[1]);

    private void SelectHoveredDrone(XRBaseInteractor interactor)
    {
        foreach (var hovered in interactor.interactablesHovered)
        {
            if (TrySelectDrone(hovered.transform)) return;
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args) => TrySelectDrone(args.interactableObject.transform);

    private bool TrySelectDrone(Transform target)
    {
        var part = target.GetComponentInParent<DronePartView>();
        if (part == null || string.IsNullOrEmpty(part.InstanceId) ||
            !_assembly.TryGetDroneForPart(part.InstanceId, out _)) return false;
        if (Selection.Current?.PartId != part.InstanceId)
            Selection.Select(new SelectionTarget(SelectionType.Part, part.InstanceId));
        return true;
    }

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.F11))
        {
            RequestClearGarage();
        }


        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ReturnToAssembly();
        }

        if (Input.GetKeyDown(KeyCode.F4) && !_loadingGarage && !_leaving &&
            TryGetSelectedDrone(out var drone, out var saved))
        {
            _garage.Delete(saved.GarageId);
            Selection.Clear();
            _assembly.RemoveDrone(drone.InstanceId);
        }

        if(isDronesRotating) RotateSlots();
    }

    private void RotateSlots()
    {
        foreach (var slot in _slots)
        {
            
            slot.Rotate(Vector3.up * _rotationSpeed * Time.deltaTime);
        }
    }


    public void RequestClearGarage()
    {
        if (_loadingGarage || _leaving) return;
        clearGarageConfirmation?.Show("Удалить все дроны из гаража этой сборки?", ClearGarage);
    }

    private void ClearGarage()
    {
        if (_loadingGarage || _leaving) return;
        try
        {
            _garage.Clear();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception, this);
            _notificationService.Warning("Не удалось очистить гараж");
            return;
        }
        Selection.Clear();
        _assembly.ClearCurrentAssembly();
        _notificationService.Info("Гараж очищен");
    }

    private async void LoadGarage()
    {

        Debug.Log($"_assembly {_assembly!=null}");
        _assembly.ClearCurrentAssembly();
        try
        {
            await RestoreGarage();
            _notificationService.Info("Гараж Загружен!", 5);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception, this);
            _notificationService.Warning("Не удалось загрузить гараж");
        }
        finally
        {
            _loadingGarage = false;
        }
    }

    
}
