using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

public class DronePartView : MonoBehaviour
{
    public string InstanceId { get; private set; }

    [SerializeField] private Renderer _renderer;

    private MaterialPropertyBlock _mpb;
    private Color _color;
    private Color highlightedColor;
    private bool highlighted = false;
    private bool selected = false;

    private Dictionary<string, SocketView> _sockets;
    private IEventBus _eventBus;
    private Clean_AssemblySystem _assembly;
    private SelectionService _selectionService;

    [Inject]
    public void Construct(Clean_AssemblySystem assembly)
    {

        Debug.Log($"22222222 Construct Zenject {this}");
        _assembly = assembly;
    }

    public SocketView GetSocket(string socketId)
    {
        return _sockets[socketId];
    }

    public void AttachTo(Transform parent)
    {


        transform.SetParent(parent);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        Debug.Log($"{gameObject.name} AttachTo {parent.name}");
    }


    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();

        //InitializeSockets();
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
    public void ApplyVisualPreview(PartVisualProperties visual)
    {
        _color = visual.Color;
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", _color);
        _mpb.SetFloat("_Smoothness", visual.Smoothness);
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
            _mpb.SetColor("_BaseColor", highlightedColor);
            highlighted = true;
        }
        else if (!on && highlighted)
        {
            _mpb.SetColor("_BaseColor", _color);
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
