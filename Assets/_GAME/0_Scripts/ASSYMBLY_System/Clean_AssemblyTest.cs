using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Zenject;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.STP;

public class Clean_AssemblyTest : MonoBehaviour
{

    [SerializeField] private string[] partIds;


    [SerializeField] private Button[] buttons;




    private IEventBus _eventBus;
    public ISelectionService Selection;   
    private PartHighlightService _highlightService;
    private IGarageService _garage;
    private INotificationService _notifications;
    private DroneReadinessService _readinessService ;
    private IPartConfigRepository _configs;
    
    private IVisualPresetRepository _visualPresets;
    private IMaterialRegistry _materialDefs;
    private ValidateEffectsSystem _validateEffects;

    private Clean_AssemblySystem _assemblySystem;
    [Inject] private InspectorService _inspector;
    private PartViewRegistry _views;
    private DroneFocusEffect _focusEffect;
    private AddressablesPrefabService _prefabs;
    [SerializeField] private NearFarInteractor[] _interactors;

    [SerializeField] private NearFarInteractor _leftInteractor;
    [SerializeField] private NearFarInteractor _rightInteractor;

    private string _mainPartId = null;

    private bool inValidationMode = false;

    [Inject] IAppLogger _logger;

    [Inject]
    public void Construct(
        IEventBus eventBus,
        INotificationService notifications,
        ISelectionService selection,
        PartHighlightService highlightService,
        AddressablesPrefabService prefabs, 
        IGarageService garage,
        DroneReadinessService readinessService,
        IPartConfigRepository configs,
        IVisualPresetRepository visualPresets,
        IMaterialRegistry materialDefs,
        ValidateEffectsSystem validateEffects,
        DroneFocusEffect focusEffect,
        PartViewRegistry views,
        Clean_AssemblySystem assemblySystem
        )
    {

        _eventBus = eventBus;
        _notifications = notifications;
        Selection = selection;
        _eventBus.Subscribe<Clean_PartCreatedEvent>(OnPartCreated);
        _highlightService = highlightService;
        _prefabs = prefabs;
        _garage = garage;
        _readinessService = readinessService;
        _configs = configs;
        _visualPresets = visualPresets;
        _materialDefs = materialDefs;
        _validateEffects = validateEffects;
        _focusEffect = focusEffect;
        _views = views;
        _assemblySystem = assemblySystem;
    }

    private void Awake()
    {
        //_interactors = FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None);

        Debug.Log($"!!!!!!!!!!!!!!!!_interactors {_interactors.Length}");
        _assemblySystem.Load();
        CreateTestPresets();


    }


   

    private void OnButtonClick(string id)
    {
        throw new NotImplementedException();
    }

    private void OnEnable()
    {
        foreach (XRBaseInteractor interactor in _interactors)
        {

            Debug.Log($"!!!!!!!!!!!!!interactor {interactor.name} Subscribed");
            interactor.selectEntered.AddListener(OnSelectEntered);
            interactor.selectExited.AddListener(OnSelectExited);
            interactor.hoverEntered.AddListener(OnHoverEnter);
            interactor.hoverExited.AddListener(OnHoverExit);
            
        }
        //triggerAction.action.performed += OnTriggerPressed;
        leftTriggerAction.action.performed += OnLeftTrigger;
        rightTriggerAction.action.performed += OnRightTrigger;



    }




    [SerializeField]
    private InputActionReference leftTriggerAction;
    [SerializeField]
    private InputActionReference rightTriggerAction;


    private void OnDisable()
    {
        leftTriggerAction.action.performed -= OnLeftTrigger;
        rightTriggerAction.action.performed -= OnRightTrigger;
    }




    private void CreateTestPresets()
    {
        _visualPresets.Save(new VisualPreset
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Racing Red",
            Visual = new PartVisualProperties
            {
                MaterialId = "carbon",
                Color = Color.red,
                Smoothness = 0.8f,
                Metallic = 0.2f
            }
        });

        _visualPresets.Save(new VisualPreset
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Military Green",
            Visual = new PartVisualProperties
            {
                MaterialId = "metal",
                Color = new Color(0.3f, 0.5f, 0.2f),
                Smoothness = 0.3f,
                Metallic = 0.8f
            }
        });

        _visualPresets.Save(new VisualPreset
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Chrome Black",
            Visual = new PartVisualProperties
            {
                MaterialId = "metal",
                Color = Color.black,
                Smoothness = 1f,
                Metallic = 1f
            }
        });

        Debug.Log("Test presets created");
    }

    private void OnHoverExit(HoverExitEventArgs arg0)
    {
        _highlightService.Exit();
    }


    private void OnLeftTrigger(InputAction.CallbackContext ctx)
    {
        TrySelect(_leftInteractor);
    }
    

    private void OnRightTrigger(InputAction.CallbackContext ctx)
    {
        TrySelect(_rightInteractor);
    }

    private void TrySelect(NearFarInteractor interactor)
    {
        if (interactor.interactablesHovered.Count == 0)
            return;

        var hovered = interactor.interactablesHovered[0];

        var part =
            hovered.transform.GetComponentInParent<DronePartView>();

        if (part == null)
            return;

        Selection.Select(new SelectionTarget(
            SelectionType.Part,
            part.InstanceId));
    }


    private void OnHoverEnter(HoverEnterEventArgs arg0)
    {
        var hoveredTransform = arg0.interactableObject.transform;

        var view = hoveredTransform.GetComponent<DronePartView>();
        if (view == null)
        {
            Debug.LogWarning($"Hovered object does not have a DronePartView component: {hoveredTransform.name}");
            return;
        }

        _highlightService.Enter(view);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {

        if(args.interactableObject.transform.TryGetComponent<DronePartView>( out var dronePartView))
        {
            Debug.Log($"OnSelectEntered {dronePartView.InstanceId!= null}");


            var domain = _assemblySystem.GetPartDomainState(dronePartView.InstanceId);

            var selectedDroneId = domain.DroneId;

            Selection.Select(
            new SelectionTarget(SelectionType.Part, dronePartView.InstanceId));

            //_selectionService.Select(dronePartView.InstanceId , selectedDroneId);
            // Detachment is handled by AssemblyGrabModeService through PartGrabStartedEvent.
        }
    }

   



    private void OnPartCreated(Clean_PartCreatedEvent @event)
    {

        //Debug.Log($"OnPartCreated event handled / Instance {@event.InstanceId}");

        //if (_mainPartId == null) _mainPartId = @event.InstanceId;  // самый первый деталь. Для теста
    }

    private void Update()
    {  

        if (Input.GetKeyDown(KeyCode.Y))
        {
            StartCoroutine( CreateTestPartsCoroutine());   /// Костыль - друг за другом создавать Не получается из Addressables , Нужно либо с  паузами либо механизм ожидания окончания создания

            //string rndId = partIds[UnityEngine.Random.Range(0, partIds.Length)];

            //_eventBus.Publish(new Clean_CreatePartRequestEvent { PartId = rndId, Timestamp = DateTime.UtcNow });
        }

        if (Input.GetKeyDown(KeyCode.I))
        {

            if (Selection.Current != null)
            {
                Debug.Log($"SelectedPartId {Selection.Current.Value.PartId}");


                // Тестирование удаления


                Debug.Log($"DDDDDD .Publish(new Clean_DeletePartRequest {this}");
                _eventBus.Publish(new Clean_DeletePartRequest { InstanceId = Selection.Current.Value.PartId, Timestamp = DateTime.UtcNow });


                //Тест изменения визуала

                //var allMaterials = _materialDefs.GetAll();
                //int randIndex = UnityEngine.Random.Range(0, allMaterials.Count);

                //var randColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

                //string matId = "DefaultPlasticMaterial";

                ////var newVisual = new PartVisualProperties() { Smoothness = 1, MaterialAddress = "PlasticAddressablesMAt" };
                //var newVisual = new PartVisualProperties() { Smoothness = 1, MaterialId = allMaterials[randIndex].Id , Color = randColor};


                //_eventBus.Publish(new ApplyPartVisualCommand(Selection.Current.Value.PartId, newVisual) { Timestamp = DateTime.UtcNow });



                //if(_selectionService.SelectedPartId == _mainPartId) return;

                //_eventBus.Publish(new PartSocketAttachRequest() { PartInstanceId = _selectionService.SelectedPartId, AttachedPartId = _mainPartId , AttachedSocketId = "engineSocket" ,Timestamp = DateTime.UtcNow });

                //_assemblySystem.RenameDrone(
                //                Selection.Current.Value.PartId,
                //                "Interceptor");

                //Debug.Log("====== SECOND REBUILD ======");

                //_assemblySystem.RebuildDrones();




                // Тестирование системы Проверки готовности

                //var selectedId = Selection.Current.Value.PartId;
                //if (selectedId == null) return;

                //var partDomain = _assemblySystem.GetPartDomainState(selectedId);
                //if (!string.IsNullOrEmpty(partDomain.DroneId))
                //{
                //    ValidateDrone(partDomain.DroneId);
                //}
            }


        }



        if (Input.GetKeyDown(KeyCode.U))
        {                       
                
            if (Selection.Current != null)

            {

                var selectedId = Selection.Current.Value.PartId;
                if (selectedId == null) return;

                var partDomain = _assemblySystem.GetPartDomainState(selectedId);
                if (!string.IsNullOrEmpty(partDomain.DroneId))
                {                    
                    FlightSession.Drone = _assemblySystem.BuildSaveDataForDrone(partDomain.DroneId);
                    _assemblySystem.Save();

                    SceneManager.LoadScene("FlightScene");
                }


                //Debug.Log($"SelectedPartId {_selectionService.SelectedPartId}");
                //_eventBus.Publish(new Clean_DuiblicatePartRequest { InstanceId = Selection.Current.Value.PartId, Timestamp = DateTime.UtcNow });


            }

        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            PutSelectedDroneToGarage();
            

        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _assemblySystem.Save();
            SceneManager.LoadScene("GarageScene");
        }

        if (Input.GetKeyDown(KeyCode.F11))
        {
            _assemblySystem.ClearCurrentAssembly();
        }

    }

    #region Validation Test

    public void ValidateDrone(string droneId)
    {
        if (!inValidationMode)
        {

            _views.TryGet(droneId, out var view);

            if (view != null)
            {
                _focusEffect.Initialize(view.transform);

                Debug.Log($"_validateEffectsSystem.Enter(); {this}");

                _validateEffects.Enter();
            }

            //_logger.Log($"*****Validate {droneId} ");
            DroneDomainState drone =
                _assemblySystem.GetDroneDomainState(droneId);

            _logger.Log($"********DroneDomain {drone != null} ");

            DroneRequirements requirements =
                CreateMissionRequirements();

            DroneValidationContext context =
                BuildContext(
                    drone,
                    requirements);

            DroneReadinessResult result =
                _readinessService.Validate(context);

            ShowValidationResult(result);
            inValidationMode = true;
        }else
        {
            _validateEffects.Exit();
            HideValidationResult();
            inValidationMode=false;
        }

    }

    private void HideValidationResult()
    {
        /// СКРЫТЬ UI ВАЛИДАЦИИ
    }

    /// <summary>
    /// Показать UI валидации
    /// </summary>
    /// <param name="result"></param>
    private void ShowValidationResult(DroneReadinessResult result)
    {

        foreach (var group in result.Groups)
        {
            Debug.Log(
                $"**********=== {group.GroupName} ===");

            foreach (var message in group.Messages)
            {
                Debug.Log(
                    $"***********{message.Severity}: {message.Message}");
            }
        }

        Debug.Log(
        $"******Готовность: {result.TotalScore:F0}%");

        if (result.IsReady) _logger.Log("*******Дрон готов к полету");
        else _logger.Log("******Дрон НЕ ГОТОВ к полету");
        
    }

    private DroneValidationContext BuildContext(DroneDomainState drone, DroneRequirements requirements)
    {
        List<PartDomainState> parts =
        drone.partInstanseIds
            .Select(id =>
                _assemblySystem.GetPartDomainState(id))
            .ToList();

        var rootView = _assemblySystem.GetViewById(drone.InstanceId);

        var partsByType = parts
            .GroupBy(x =>
                _configs.Get(x.PartId).PartType)
            .ToDictionary(
        g => g.Key,
        g => g.ToList());



        return new DroneValidationContext
        {
            Drone = drone,
            Parts = parts,
            Requirements = requirements,
            PartsByType = partsByType,
            droneTransform = rootView.transform
            
        };
    }

    private DroneRequirements CreateMissionRequirements()
    {
        return new DroneRequirements
        {
           

            MinFlightTimeMinutes = 4f,

            MinThrustToWeightRatio = 1.8f,

            MaxCenterOfMassOffset = 0.2f,

            CheckCollisions = true
        };
    }

    #endregion


    public void PutSelectedDroneToGarage()
    {
        // Resolve from the same current selection used by the drone panel.
        if (!_inspector.TryGetSelectedDrone(out var drone))
            return;

        var droneView = _assemblySystem.GetViewById(drone.InstanceId);

        if (!_garage.HasFreeSlot())
        {

            Debug.Log($" Гараж заполнен. Удалите один из дронов. {this}");

            if (droneView != null)
                _notifications.ShowWorld("Гараж заполнен. Удалите один из дронов", droneView.transform, NotificationType.Warning);
            else
                _notifications.Info("Гараж заполнен. Удалите один из дронов");

            // SHOW WARNING TO USER           

        }
        else
        {
            
            var droneName = _assemblySystem.GetDroneName(drone.InstanceId);
            _garage.SaveDrone(drone.InstanceId);
            Selection.Clear();
            _assemblySystem.RemoveDrone(drone.InstanceId);

            _notifications.Info($"Дрон {droneName} помещен в гараж");
        }

        //string droneId =
        //    _selection.SelectedDroneId;

        //if (string.IsNullOrEmpty(droneId))
        //    return;

        //_garage.SaveDrone(droneId);

        //_assembly.RemoveDrone(droneId);
    }




    private IEnumerator CreateTestPartsCoroutine()
    {
        foreach (var id in partIds) 
        {
            _eventBus.Publish(new Clean_CreatePartRequestEvent { PartId = id, Timestamp = DateTime.UtcNow });
        }
        yield return new WaitForSeconds(0.5f);
        _assemblySystem.RebuildDrones();
    }
}
