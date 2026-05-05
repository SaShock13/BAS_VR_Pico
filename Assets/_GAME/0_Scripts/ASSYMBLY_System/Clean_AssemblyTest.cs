using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;
using Zenject;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using static UnityEngine.Rendering.GPUSort;
using Pico.Platform;

public class Clean_AssemblyTest : MonoBehaviour
{

    [SerializeField] private string partId;
    IEventBus _eventBus;
    private SelectionService _selectionService;
    private PartHighlightService _highlightService;

    [SerializeField] private XRBaseInteractor[] _interactors;


    [Inject]
    public void Construct(IEventBus eventBus,SelectionService selectionService, PartHighlightService highlightService)
    {

        _eventBus = eventBus;
        _selectionService = selectionService;
        _eventBus.Subscribe<Clean_PartCreatedEvent>(OnPartCreated);
        _highlightService = highlightService;
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
        Debug.Log($"Clean_AssemblyTest OnHoverEXit {this}");
        _highlightService.Exit();
    }

    private void OnHoverEnter(HoverEnterEventArgs arg0)
    {

        Debug.Log($"Clean_AssemblyTest OnHoverEnter {this}");
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
            _selectionService.Select(dronePartView.InstanceId);

                
        }
    }





    private void OnPartCreated(Clean_PartCreatedEvent @event)
    {

        Debug.Log($"OnPartCreated event handled / Instance {@event.InstanceId}");
    }

    private void Update()
    {  

        if (Input.GetKeyDown(KeyCode.Y))
        {
            _eventBus.Publish(new Clean_CreatePartRequestEvent{PartId = partId, Timestamp = DateTime.UtcNow});
        }

        if (Input.GetKeyDown(KeyCode.I))
        {

            Debug.Log($"SelectedPartId {_selectionService.SelectedPartId}");
            if (_selectionService.SelectedPartId != null)
            {
                _eventBus.Publish(new Clean_DeletePartRequest { InstanceId = _selectionService.SelectedPartId, Timestamp = DateTime.UtcNow });
            }


        }



        if (Input.GetKeyDown(KeyCode.U))
        {

            if (_selectionService.SelectedPartId != null)

            {
                Debug.Log($"SelectedPartId {_selectionService.SelectedPartId}");
                _eventBus.Publish(new Clean_DuiblicatePartRequest { InstanceId = _selectionService.SelectedPartId, Timestamp = DateTime.UtcNow });
            }
          
        }



    }
}
