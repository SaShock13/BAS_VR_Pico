using System;
using System.ComponentModel.Design;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Zenject;

public class SocketView : MountPoint
{
    //#region OLD


    private const string _validPreviewMaterialName = "GreenTrtansparent";
    private const string _invalidPreviewMaterialName = "FakeTransparentRed";

    private IEventBus _eventBus;  /// todo Как получить зависимости Zenject?????????

    public string SocketId => _socketId;
    public override string MountId => SocketId;
    public override PartType[] AcceptedTypes => AllowedTypes;
    public override DronePartView Owner => ParentView;

    public DronePartView ParentView { get { _parentView = GetComponentInParent<DronePartView>();return _parentView; } }

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
    private IDisposable _attachedSubscription;
    


    public void Init(IEventBus eventBus, Clean_AssemblySystem assemblySystem)
    {

        _eventBus = eventBus;
        _assemblySystem = assemblySystem;
        _attachedSubscription?.Dispose();
        _attachedSubscription = _eventBus.Subscribe<PartSocketAttachedEvent>(OnPartAttached);
    }


    private void Awake()
    {
        _validPreviewMaterial = Resources.Load<Material>("Materials/" +$"{_validPreviewMaterialName}");
        _invalidPreviewMaterial = Resources.Load<Material>("Materials/" +$"{_invalidPreviewMaterialName}");


        _previewSystem = new SocketPreviewSystem(
            _validPreviewMaterial,
            _invalidPreviewMaterial);

        _socketInteractor.showInteractableHoverMeshes = false;

        _socketInteractor.hoverEntered.AddListener(OnHoverEntered);
        _socketInteractor.hoverExited.AddListener(OnHoverExited);
    }

    
    private void OnDestroy()
    {
        _attachedSubscription?.Dispose();
        if (_socketInteractor != null)
        {
            _socketInteractor.hoverEntered.RemoveListener(OnHoverEntered);
            _socketInteractor.hoverExited.RemoveListener(OnHoverExited);
        }
        if (_hoveredPart != null)
        {
            _hoveredPart.RemoveHoveredSocket(this);
            if (_hoveredPart.TryGetComponent<XRGrabInteractable>(out var grab))
                grab.selectExited.RemoveListener(OnPartReleased);
        }

        _previewSystem?.HidePreview();
    }

    private void OnHoverEntered(  /// todo должно быть в сокетИнтерактор скорее всего. 
        HoverEnterEventArgs args)
    {


        Debug.Log($"" +
            $"part {args.interactableObject.transform.name} " +
            $"is hovering over Socket {SocketId}  " +
            //$"on  {_parentView.transform.name}" +
            $"");
        DronePartView part =
            args.interactableObject.transform
                .GetComponent<DronePartView>();

        if (part == null)
            return;

        // ПРоверка если Обьект не в руке, не показывают превью. 
        if (!(args.interactableObject is XRGrabInteractable heldGrab) || !heldGrab.isSelected)
        {
            return;
        }



        if (!_assemblySystem.IsCompatibleWithSocket(part.InstanceId, this))
            return;

        if (_hoveredPart != null)
        {
            _hoveredPart.RemoveHoveredSocket(this);
            if (_hoveredPart.TryGetComponent<XRGrabInteractable>(out var previousGrab))
                previousGrab.selectExited.RemoveListener(OnPartReleased);
        }

        _hoveredPart = part;
        part.AddHoveredSocket(this);


        _isValidHover =
            _assemblySystem.CanAttach(
                _hoveredPart.InstanceId,
                this);

        _previewSystem.ShowPreview(  
            part,
            _previewAnchor,
            _isValidHover);
        _previewSystem.SetVisible(!part.HasZoneCandidate);

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
            part.RemoveHoveredSocket(this);
            _hoveredPart = null;
        }

        if (_hoveredPart == null)
            _previewSystem.HidePreview();
    }

    private void OnPartReleased(
        SelectExitEventArgs args)
    {
        if (_hoveredPart == null)
            return;

        if (args.isCanceled || _hoveredPart.IsGrabbed ||
            _hoveredPart.ZoneHandledRelease || _hoveredPart.HasZoneCandidate)
        {
            _previewSystem.HidePreview();
            return;
        }

        if (!_assemblySystem.CanAttach(_hoveredPart.InstanceId, this))
        {
            Debug.Log("INVALID ATTACH");

            _previewSystem.HidePreview();

            return;
        }


        _parentView = GetComponentInParent<DronePartView>();
        var parentPartId = ParentView.InstanceId;
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

    internal void SetPreviewVisible(bool visible) => _previewSystem?.SetVisible(visible);

    private void OnPartAttached(PartSocketAttachedEvent @event)
    {
        Debug.Log("ATTACH SUCCESS");

        _previewSystem.HidePreview();

    }




}
