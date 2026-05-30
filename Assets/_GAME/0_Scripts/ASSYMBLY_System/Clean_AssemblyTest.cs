using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Zenject;
using static UnityEngine.GraphicsBuffer;

public class Clean_AssemblyTest : MonoBehaviour
{

    [SerializeField] private string[] partIds;

    IEventBus _eventBus;
    private SelectionService _selectionService;  // todo избавиться
    public ISelectionService Selection;   
    private PartHighlightService _highlightService;
    private IGarageService _garage;

    private Clean_AssemblySystem _assemblySystem;

    private AddressablesPrefabService _prefabs;
    [SerializeField] private XRBaseInteractor[] _interactors;


    private string _mainPartId = null;


    [Inject]
    public void Construct(
        IEventBus eventBus,
        SelectionService selectionService,
        ISelectionService selection,
        PartHighlightService highlightService,
        AddressablesPrefabService prefabs, 
        IGarageService garage,
        Clean_AssemblySystem assemblySystem
        )
    {

        _eventBus = eventBus;
        _selectionService = selectionService;
        Selection = selection;
        _eventBus.Subscribe<Clean_PartCreatedEvent>(OnPartCreated);
        _highlightService = highlightService;
        _prefabs = prefabs;
        _garage = garage;
        _assemblySystem = assemblySystem;
    }

    private void Awake()
    {
        //_interactors = FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None);

        Debug.Log($"!!!!!!!!!!!!!!!!_interactors {_interactors.Length}");

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
                //var newVisual = new PartVisualProperties() { Smoothness = 1 , MaterialAddress = "PlasticAddressablesMAt" };
                //_eventBus.Publish(new ApplyPartVisualCommand (_selectionService.SelectedPartId, newVisual) {Timestamp = DateTime.UtcNow });

                //if(_selectionService.SelectedPartId == _mainPartId) return;

                //_eventBus.Publish(new PartSocketAttachRequest() { PartInstanceId = _selectionService.SelectedPartId, AttachedPartId = _mainPartId , AttachedSocketId = "engineSocket" ,Timestamp = DateTime.UtcNow });

                _assemblySystem.RenameDrone(
                                Selection.Current.Value.PartId,
                                "Interceptor");

                Debug.Log("====== SECOND REBUILD ======");

                _assemblySystem.RebuildDrones();


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
            SceneManager.LoadScene(1);  // для теста переход в Гараж
        }



    }

    public void PutSelectedDroneToGarage()
    {
        //var drone = Selection.Current.Value.PartId ;


        PartDomainState part =
            _assemblySystem.GetPartDomainState(
                Selection.Current.Value.PartId);

        Debug.Log($"gggggSelectPart  {part.PartId}");
        Debug.Log($"gggggggSelectDrone  {part.DroneId}");

        DroneDomainState drone = null;

        if (!string.IsNullOrEmpty(part.DroneId))
        {
            drone =
                _assemblySystem.GetDroneDomainState(
                    part.DroneId);
        }


        _garage.SaveDrone(drone.InstanceId);




        _assemblySystem.RemoveDrone(drone.InstanceId);
        


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
