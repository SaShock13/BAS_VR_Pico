using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.XR.PXR.Debugger;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Zenject;

public class DronePartView : MonoBehaviour
{
    [field: SerializeField]
    public string InstanceId { get; private set; }

    [SerializeField] private Renderer _renderer;
    [SerializeField] private bool _isSnapToSocketPosition = false;


    private XRGrabInteractable _interactable;

    private MaterialPropertyBlock _mpb;
    private Rigidbody _rigidBody;
    private Color _color;
    private Color highlightedColor;
    private bool highlighted = false;
    private bool selected = false;

    private Vector3 _startPosition;
    private Quaternion _startRotation;

    private Dictionary<string, SocketView> _sockets;
    private IEventBus _eventBus;
    private Clean_AssemblySystem _assembly;
    private SelectionService _selectionService;
    private AddressablesAssetService _assets;

    [Inject]
    public void Construct(Clean_AssemblySystem assembly, AddressablesAssetService assets)
    {

        Debug.Log($"22222222 Construct Zenject {this}");
        _assembly = assembly;
        _assets = assets;
    }
    private void Awake()
    {
        XRGrabInteractable grab = GetComponent<XRGrabInteractable>();

        if (grab != null)
        {
            grab.retainTransformParent = false;
        }

        _mpb = new MaterialPropertyBlock();
        _rigidBody = GetComponent<Rigidbody>();
        _interactable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        _interactable.selectExited.AddListener(OnReleased);
        _interactable.selectEntered.AddListener(OnGrab);
    }

    private void OnGrab(SelectEnterEventArgs arg0)
    {
        _startPosition = transform.position;
        _startRotation = transform.rotation;
    }

    private void OnReleased(SelectExitEventArgs arg0)
    {


        Debug.Log($"position {transform.position} rotation {transform.rotation} StartPosition {_startPosition} _startRotation {_startRotation} message {this}");

        _eventBus.Publish(new PartTransformChangedEvent
        {
            instanceId = InstanceId,
            position = transform.position,
            rotation = transform.rotation,
            StartPosition = _startPosition,
            StartRotation = _startRotation,
            
        });
    }


    public SocketView GetSocket(string socketId)
    {
        return _sockets[socketId];
    }

    public void AttachTo(Transform parent)
    {


        transform.SetParent(parent);
        _rigidBody.isKinematic = true;

        if (_isSnapToSocketPosition)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity; 
        }

        Debug.Log($"{gameObject.name} AttachTo {parent.name}");
    }

    public void Detach()
    {
        transform.SetParent(null);
    }


    private void InitializeSockets()
    {
        _sockets = new Dictionary<string, SocketView>();

        var sockets = GetComponentsInChildren<SocketView>();



        Debug.Log($"!!!!!!!!!_assembly {_assembly != null}");
        foreach (var socket in sockets)
        {
            _sockets.Add(socket.SocketId, socket);
            socket.Init(_eventBus,_assembly);
        }
    }

    public void Init(string instanceId,IEventBus eventBus)
    {

        Debug.Log($"!!!!PArt Initialized with ID  {instanceId}");
        InstanceId = instanceId;
        _renderer = GetComponentInChildren<Renderer>();
        _color = _renderer.material.color;
        _eventBus = eventBus;
        InitializeSockets();
    }

    // PREVIEW — вызывается каждый кадр
    public async Task ApplyVisualPreview(PartVisualProperties visual)
    {
        Material mat = await _assets.Load<Material>(visual.MaterialAddress);

        Debug.Log($"!!!!!!!visual.MatAddress{visual.MaterialAddress}");

        _renderer.sharedMaterial = mat;
        _color = _renderer.sharedMaterial.color;

        _renderer.GetPropertyBlock(_mpb);

        _mpb.SetColor(ShaderIds.BaseColor, _color);
        _mpb.SetFloat(ShaderIds.Smoothness, visual.Smoothness);

        _renderer.SetPropertyBlock(_mpb);
    }

    // COMMIT — когда состояние подтверждено
    public void ApplyVisualCommitted(PartVisualProperties visual)
    {
        ApplyVisualPreview(visual);
    }

    public void Highlight(bool on)
    {

        _renderer.GetPropertyBlock(_mpb);
        //_color = _mpb.GetColor("_BaseColor");

        if (on&& !highlighted )
        {
            highlightedColor = _color * 1.2f;
            _mpb.SetColor(ShaderIds.BaseColor, highlightedColor);
            highlighted = true;
        }
        else if (!on && highlighted)
        {
            _mpb.SetColor(ShaderIds.BaseColor, _color);
            highlighted = false;

        }
       
        _renderer.SetPropertyBlock(_mpb);
    }

    public void VisualSelection(bool on)
    {
        
        if (on && !selected)
        {
           
            selected = true;
        }
        else if (selected)
        {
            
            selected = false;

        }
        
    }

   
}
