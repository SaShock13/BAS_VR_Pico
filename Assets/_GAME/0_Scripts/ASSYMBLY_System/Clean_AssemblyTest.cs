using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Zenject;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.STP;

public class Clean_AssemblyTest : MonoBehaviour
{

    [SerializeField] private string[] partIds;

    private IEventBus _eventBus;
    private SelectionService _selectionService;  // todo избавиться
    public ISelectionService Selection;   
    private PartHighlightService _highlightService;
    private IGarageService _garage;
    private INotificationService _notifications;
    private DroneReadinessService _readinessService ;
    private IPartConfigRepository _configs;

    private Clean_AssemblySystem _assemblySystem;

    private AddressablesPrefabService _prefabs;
    [SerializeField] private XRBaseInteractor[] _interactors;


    private string _mainPartId = null;

    [Inject] IAppLogger _logger;

    [Inject]
    public void Construct(
        IEventBus eventBus,
        INotificationService notifications,
        SelectionService selectionService,
        ISelectionService selection,
        PartHighlightService highlightService,
        AddressablesPrefabService prefabs, 
        IGarageService garage,
        DroneReadinessService readinessService,
        IPartConfigRepository configs,
        Clean_AssemblySystem assemblySystem
        )
    {

        _eventBus = eventBus;
        _notifications = notifications;
        _selectionService = selectionService;
        Selection = selection;
        _eventBus.Subscribe<Clean_PartCreatedEvent>(OnPartCreated);
        _highlightService = highlightService;
        _prefabs = prefabs;
        _garage = garage;
        _readinessService = readinessService;
        _configs = configs;
        _assemblySystem = assemblySystem;
    }

    private void Awake()
    {
        //_interactors = FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None);

        Debug.Log($"!!!!!!!!!!!!!!!!_interactors {_interactors.Length}");
        _assemblySystem.Load();
    }



    private void OnEnable()
    {
        foreach (var interactor in _interactors)
        {

            Debug.Log($"!!!!!!!!!!!!!interactor {interactor.name} Subscribed");
            interactor.selectEntered.AddListener(OnSelectEntered);
            interactor.selectExited.AddListener(OnSelectExited);
            interactor.hoverEntered.AddListener(OnHoverEnter);
            interactor.hoverExited.AddListener(OnHoverExit);
            
        }
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
            if(domain.LifecycleState == PartLifecycleState.Installed) _eventBus.Publish(new PartSocketDetachRequest { PartInstanceId = domain.InstanceId, Timestamp = DateTime.UtcNow });
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

        }

        if (Input.GetKeyDown(KeyCode.I))
        {

            Debug.Log($"SelectedPartId {Selection.Current.Value.PartId}");
            if (Selection.Current != null)
            {
                // Тестирование удаления




                //Debug.Log($"DDDDDD .Publish(new Clean_DeletePartRequest {this}");
                //_eventBus.Publish(new Clean_DeletePartRequest { InstanceId = _selectionService.SelectedPartId, Timestamp = DateTime.UtcNow });


                // Тест изменения визуала
                //var randColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                // var newVisual = new PartVisualProperties() { Smoothness = 1, MaterialAddress = "PlasticAddressablesMAt" };
                // _eventBus.Publish(new ApplyPartVisualCommand(Selection.Current.Value.PartId, newVisual) { Timestamp = DateTime.UtcNow });

                //if(_selectionService.SelectedPartId == _mainPartId) return;

                //_eventBus.Publish(new PartSocketAttachRequest() { PartInstanceId = _selectionService.SelectedPartId, AttachedPartId = _mainPartId , AttachedSocketId = "engineSocket" ,Timestamp = DateTime.UtcNow });

                //_assemblySystem.RenameDrone(
                //                Selection.Current.Value.PartId,
                //                "Interceptor");

                //Debug.Log("====== SECOND REBUILD ======");

                //_assemblySystem.RebuildDrones();




                // Тестирование системы Проверки готовности

                var partDomain = _assemblySystem.GetPartDomainState(Selection.Current.Value.PartId);
                if (!string.IsNullOrEmpty(partDomain.DroneId))
                {
                    ValidateDrone(partDomain.DroneId);
                }
            }


        }



        if (Input.GetKeyDown(KeyCode.U))
        {

            if (Selection.Current != null)

            {
                //Debug.Log($"SelectedPartId {_selectionService.SelectedPartId}");
                _eventBus.Publish(new Clean_DuiblicatePartRequest { InstanceId = Selection.Current.Value.PartId, Timestamp = DateTime.UtcNow });


                
               
            }
          
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            PutSelectedDroneToGarage();

        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _assemblySystem.Save();
            SceneManager.LoadScene(1);  // для теста переход в Гараж
        }

        if (Input.GetKeyDown(KeyCode.F11))
        {
            _assemblySystem.ClearCurrentAssembly();
        }

    }

    #region Validation Test

    public void ValidateDrone(string droneId)
    {

        //_logger.Log($"*****Validate {droneId} ");
        DroneDomainState drone =
            _assemblySystem.GetDroneDomainState(droneId);


        _logger.Log($"********DroneDomain {drone!=null} ");

        DroneRequirements requirements =
            CreateMissionRequirements();

        DroneValidationContext context =
            BuildContext(
                drone,
                requirements);

        DroneReadinessResult result =
            _readinessService.Validate(context);

        ShowResult(result);
    }

    private void ShowResult(DroneReadinessResult result)
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
        //var drone = Selection.Current.Value.PartId ;


        PartDomainState part =
            _assemblySystem.GetPartDomainState(
                Selection.Current.Value.PartId);

        Debug.Log($"gggggSelectPart  {part.PartId}");
        Debug.Log($"gggggggSelectDrone  {part.DroneId}");

        DroneDomainState drone = null;

        var droneView = _assemblySystem.GetViewById(part.InstanceId);

        if (!string.IsNullOrEmpty(part.DroneId))
        {
            drone =
                _assemblySystem.GetDroneDomainState(
                    part.DroneId);
        }

        if (!_garage.HasFreeSlot())
        {

            Debug.Log($" Гараж заполнен. Удалите один из дронов. {this}");

            _notifications.ShowWorld("Гараж заполнен. Удалите один из дронов", droneView.transform, NotificationType.Warning);

            // SHOW WARNING TO USER           

        }
        else
        {
            
            _garage.SaveDrone(drone.InstanceId);
            _assemblySystem.RemoveDrone(drone.InstanceId);

            _notifications.Info($"Дрон {_assemblySystem.GetDroneName(drone.InstanceId)} помещен в гараж");
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
