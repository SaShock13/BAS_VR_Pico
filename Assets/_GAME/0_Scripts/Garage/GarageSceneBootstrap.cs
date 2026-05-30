using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Zenject;

public class GarageSceneBootstrap : MonoBehaviour
{
    [SerializeField]
    private Transform[] _slots;

    [Inject]
    private IGarageService _garage;

    [Inject]
    private Clean_AssemblySystem _assembly;

    [SerializeField] private XRBaseInteractor[] _interactors;

    [Inject]
    public ISelectionService Selection;

    [Inject]
    private PartHighlightService _highlightService;

    [Inject]
    private IEventBus _eventBus;

    private void Start()
    {
        
        LoadGarage();
    }

    private void OnEnable()
    {
        foreach (var interactor in _interactors)
        {
            interactor.selectEntered.AddListener(OnSelectEntered);
            interactor.selectExited.AddListener(OnSelectExited);
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

        for (int i = 0; i < count; i++)
        {


            Debug.Log($"xxxxxxmetaData.Name {drones[i].metaData.Name}");
            await _assembly.SpawnAssembly(
                drones[i].Assembly,
                _slots[i].position);

            Debug.Log($"xxxxxxmetaData.Name {drones[i].metaData.Name}");
            _assembly.RenameDrone(
            drones[i].DroneId,
            drones[i].metaData.Name);
        }
        _assembly.FinishBatchLoad();



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

        if (args.interactableObject.transform.TryGetComponent<DronePartView>(out var dronePartView))
        {
            Debug.Log($"xxxxxxxxOnSelectEntered {dronePartView.InstanceId != null}");


            var domain = _assembly.GetPartDomainState(dronePartView.InstanceId);

            var selectedDroneId = domain.DroneId;


            Debug.Log($"xxxxxxxxxOnSelectEntered selectedDroneId{selectedDroneId}");

            Selection.Select(
            new SelectionTarget(SelectionType.Part, dronePartView.InstanceId));
           
        }
    }



    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.F11))
        {
            ClearGarage();
        }


        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SceneManager.LoadScene(0);  // для теста переход в конструктор
        }



    }

    private void ClearGarage()
    {
        _garage.Clear();
        _assembly.ClearCurrentAssembly();
    }

    private async void LoadGarage()
    {
        _assembly.ClearCurrentAssembly();
        await RestoreGarage();
    }

    
}
