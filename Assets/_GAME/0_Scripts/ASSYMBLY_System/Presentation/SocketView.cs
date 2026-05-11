using System;
using System.ComponentModel.Design;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Zenject;

public class SocketView : MonoBehaviour
{
    //#region OLD
   

    private IEventBus _eventBus;  /// todo Как получить зависимости Zenject?????????

    public string SocketId => _socketId;

    //public string[] MatchPartIdlist;

    public PartType[] AllowedTypes;

    public Transform AttachPoint;
    private Rigidbody _rigidbody;
    private Collider _collider;
    private DronePartView _parentView;




    //public void Init(IEventBus eventBus, Clean_AssemblySystem assemblySystem)
    //{

    //    _eventBus = eventBus;
    //    _assemblySystem = assemblySystem;
    //}


    //private void Awake()
    //{
    //    //_socketInteractor.selectEntered.AddListener(OnSelectEntered);

    //    //_socketInteractor.hoverEntered.AddListener(OnHoverEntered);
    //    //_collider = GetComponent<Collider>();
    //    //_rigidbody = GetComponent<Rigidbody>();

    //    // hover only
    //    _socketInteractor.hoverEntered.AddListener(OnHoverEntered);
    //    _socketInteractor.hoverExited.AddListener(OnHoverExited);
    //}



    //private void OnDestroy()
    //{
    //    _socketInteractor.hoverEntered.RemoveListener(OnHoverEntered);
    //    _socketInteractor.hoverExited.RemoveListener(OnHoverExited);
    //}

    //private void OnHoverEntered(HoverEnterEventArgs args)
    //{
    //    DronePartView part =
    //        args.interactableObject.transform.GetComponent<DronePartView>();

    //    if (part == null)
    //        return;

    //    _hoveredPart = part;

    //    XRGrabInteractable grab =
    //        part.GetComponent<XRGrabInteractable>();

    //    if (grab != null)
    //    {
    //        grab.selectExited.AddListener(OnPartReleased);
    //    }

    //    Debug.Log($"Hover Enter: {part.InstanceId}");
    //}

    //private void OnHoverExited(HoverExitEventArgs args)
    //{
    //    DronePartView part =
    //        args.interactableObject.transform.GetComponent<DronePartView>();

    //    if (part == null)
    //        return;

    //    XRGrabInteractable grab =
    //        part.GetComponent<XRGrabInteractable>();

    //    if (grab != null)
    //    {
    //        grab.selectExited.RemoveListener(OnPartReleased);
    //    }

    //    if (_hoveredPart == part)
    //    {
    //        _hoveredPart = null;
    //    }

    //    Debug.Log($"Hover Exit: {part.InstanceId}");
    //}

    //private void OnPartReleased(SelectExitEventArgs args)
    //{
    //    if (_hoveredPart == null)
    //        return;

    //    Debug.Log($"Try Attach {_hoveredPart.InstanceId}");

    //    _parentView = GetComponentInParent<DronePartView>();
    //    var parentPartId = _parentView.InstanceId;
    //    var childPartId = _hoveredPart.GetComponent<DronePartView>().InstanceId;
    //    var socketId = SocketId;

    //    // запрос на аттач
    //    _eventBus.Publish(new PartSocketAttachRequest { PartInstanceId = parentPartId, AttachedPartId = childPartId, AttachedSocketId = socketId, Timestamp = DateTime.Now });


    //    //_assemblySystem.TryAttach(
    //    //    _socketId,
    //    //    _hoveredPart.InstanceId);

    //    XRGrabInteractable grab =
    //        _hoveredPart.GetComponent<XRGrabInteractable>();

    //    if (grab != null)
    //    {
    //        grab.selectExited.RemoveListener(OnPartReleased);
    //    }

    //    _hoveredPart = null;
    //} 
    //#endregion

    [Header("XR")]
    [SerializeField]
    private XRSocketInteractor _socketInteractor;

    [Header("Socket")]
    [SerializeField]
    private string _socketId;

    [SerializeField]
    private Transform _previewAnchor;

    [Header("Preview Materials")]
    [SerializeField]
    private Material _validPreviewMaterial;

    [SerializeField]
    private Material _invalidPreviewMaterial;

    private DronePartView _hoveredPart;

    private bool _isValidHover;

    private SocketPreviewSystem _previewSystem;

    private Clean_AssemblySystem _assemblySystem;
    


    public void Init(IEventBus eventBus, Clean_AssemblySystem assemblySystem)
    {

        _eventBus = eventBus;
        _assemblySystem = assemblySystem;
        _eventBus.Subscribe<PartSocketAttachedEvent>(OnPartAttached);
    }


    private void Awake()
    {
        _previewSystem = new SocketPreviewSystem(
            _validPreviewMaterial,
            _invalidPreviewMaterial);

        _socketInteractor.hoverEntered.AddListener(OnHoverEntered);
        _socketInteractor.hoverExited.AddListener(OnHoverExited);
    }

    
    private void OnDestroy()
    {
        _socketInteractor.hoverEntered.RemoveListener(OnHoverEntered);
        _socketInteractor.hoverExited.RemoveListener(OnHoverExited);

        _previewSystem.HidePreview();
    }

    private void OnHoverEntered(
        HoverEnterEventArgs args)
    {
        DronePartView part =
            args.interactableObject.transform
                .GetComponent<DronePartView>();

        if (part == null)
            return;
        if ((args.interactableObject as XRGrabInteractable).isSelected )
        //if (_assemblySystem.IsInHands(part))
        {

            Debug.Log($"777777_parentView == _assemblySystem.ReturnSelectedPart() {this}");
            return;
        }



        _hoveredPart = part;


        _isValidHover =
            _assemblySystem.CanAttach(                
                part.InstanceId,
                this);

        _previewSystem.ShowPreview(  /// todo Показывать превью только если сокет не в руке
            part,
            _previewAnchor,
            _isValidHover);

        XRGrabInteractable grab =
            part.GetComponent<XRGrabInteractable>();

        if (grab != null)
        {
            grab.selectExited.AddListener(OnPartReleased);
        }
    }

    private void OnHoverExited(
        HoverExitEventArgs args)
    {
        DronePartView part =
            args.interactableObject.transform
                .GetComponent<DronePartView>();

        if (part == null)
            return;

        XRGrabInteractable grab =
            part.GetComponent<XRGrabInteractable>();

        if (grab != null)
        {
            grab.selectExited.RemoveListener(OnPartReleased);
        }

        if (_hoveredPart == part)
        {
            _hoveredPart = null;
        }

        _previewSystem.HidePreview();
    }

    private void OnPartReleased(
        SelectExitEventArgs args)
    {
        if (_hoveredPart == null)
            return;

        if (!_isValidHover)
        {
            Debug.Log("INVALID ATTACH");

            _previewSystem.HidePreview();

            return;
        }


        _parentView = GetComponentInParent<DronePartView>();
        var parentPartId = _parentView.InstanceId;
        var childPartId = _hoveredPart.GetComponent<DronePartView>().InstanceId;
        var socketId = SocketId;

        // запрос на аттач
        _eventBus.Publish(new PartSocketAttachRequest { PartInstanceId = childPartId, AttachedPartId = parentPartId, AttachedSocketId = socketId, Timestamp = DateTime.Now });


        //bool success =
        //    _assemblySystem.TryAttach(
        //        _socketId,
        //        _hoveredPart.InstanceId);

        //if (success)
        //{
            
        //}
    }

    private void OnPartAttached(PartSocketAttachedEvent @event)
    {
        Debug.Log("ATTACH SUCCESS");

        _previewSystem.HidePreview();

    }




}