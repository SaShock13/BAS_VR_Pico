using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.XR.PXR.Debugger;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using Zenject;

public class DronePartView : MonoBehaviour,IHighlightable, IXRHoverFilter, IXRSelectFilter
{
    private bool _partInteractionAllowed = true;
    public bool canProcess => isActiveAndEnabled;
    public bool IsGrabbed => _interactable != null && _interactable.isSelected;
    public DroneGrabEnvelope GrabEnvelope { get; private set; }

    public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable) => _partInteractionAllowed;

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        // A mode change must never cancel an existing hand selection.
        var selected = interactor.interactablesSelected;
        for (int i = 0; i < selected.Count; i++)
            if (ReferenceEquals(selected[i], interactable)) return true;
        return _partInteractionAllowed;
    }

    public void SetPartInteractionAllowed(bool allowed) => _partInteractionAllowed = allowed;

    public void EncapsulateOwnGeometry(Transform reference, ref Bounds bounds, ref bool found)
    {
        if (_renderers == null) return;
        foreach (var renderer in _renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            var world = renderer.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = world.center + Vector3.Scale(world.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var point = reference.InverseTransformPoint(corner);
                if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                else bounds.Encapsulate(point);
            }
        }
    }
    [field: SerializeField]
    public string InstanceId { get; private set; }

    PartType type;
    private XRGrabInteractable _interactable;

    [Inject] IMaterialRegistry _materialRegistry;

    private MaterialPropertyBlock _mpb;
    private Rigidbody _rigidBody;
    private Color _color;
    private Color highlightedColor;
    private bool highlighted = false;
    private bool selected = false;

    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private MountingZoneView _previousZone;
    private Vector3 _previousZonePosition;
    private Quaternion _previousZoneRotation;
    private SocketPreviewSystem _zonePreview;
    private Coroutine _zonePreviewRoutine;
    private bool _zonePreviewCreated;
    private readonly HashSet<SocketView> _hoveredSockets = new HashSet<SocketView>();
    internal MountingZoneView[] MountingZones { get; private set; } = Array.Empty<MountingZoneView>();
    internal Bounds LocalMountingBounds { get; private set; }
    internal bool HasMountingBounds { get; private set; }
    internal bool ZoneHandledRelease { get; private set; }
    internal bool HasZoneCandidate =>
        _assembly != null && _assembly.TryFindMountingZone(this, out _, out _, out _);
    internal void AddHoveredSocket(SocketView socket) => _hoveredSockets.Add(socket);
    internal void RemoveHoveredSocket(SocketView socket) => _hoveredSockets.Remove(socket);

    private Dictionary<string, SocketView> _sockets;
    private Dictionary<PartType, SocketView> _socketsByType;
    private IEventBus _eventBus;
    private Clean_AssemblySystem _assembly;
    private SelectionService _selectionService;
    private AddressablesAssetService _assets;


    [SerializeField] private Renderer _renderer;
    [SerializeField] private bool _isSnapToSocketPosition = true;
    [SerializeField] private GameObject highlighter ;  // todo Хайлайт скрвис написать для подсветки. Через Дубликам меша или шейдер
                                                       
    private Material _outlineMaterial;
    private string _outlineMaterialAddress = "DefaultOutlineMat";
    private Outline _outline;

    [SerializeField]
    private Renderer[] _renderers;
    private Material[][] _materialSlots;
    private MaterialPropertyBlock[][] _prefabPropertyBlocks;
    public bool HasConstantMaterial { get; private set; }

    private static readonly int EmissionColor =
        Shader.PropertyToID("_EmissionColor");



    private Coroutine _highlightRoutine;

    public void SetHintHighlighted(bool value)
    {
        if (_highlightRoutine != null)
        {
            StopCoroutine(_highlightRoutine);
            _highlightRoutine = null;
        }

        if (value)
        {
            _highlightRoutine = StartCoroutine(BlinkRoutine());
        }
        else
        {
            Highlight(false);
        }
    }

    private IEnumerator BlinkRoutine()
    {
        const float blinkInterval = 0.5f;
        //const float duration = 5f;

        float timer = 0f;
        bool state = false;

        while (true)
        {
            state = !state;

            Highlight(state);

            yield return new WaitForSeconds(blinkInterval);

            timer += blinkInterval;
        }

        Highlight(false);

        _highlightRoutine = null;
    }


    private async void Start()
    {
        _outline = gameObject.AddComponent<Outline>();
        _outlineMaterial = await _assets.Load<Material>(_outlineMaterialAddress);
        if (this == null || _outline == null || _outlineMaterial == null) return;
        _outline.SetOutlineMaterial(_outlineMaterial);
        _outline.Initialize();
        
    }
    


    

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
        GrabEnvelope = GetComponent<DroneGrabEnvelope>();



    }

    private void OnEnable()
    {
        if (MountingZones.Length > 0) _assembly?.RegisterZoneOwner(this);
        _interactable.hoverFilters.Add(this);
        _interactable.selectFilters.Add(this);
        _interactable.selectExited.AddListener(OnReleased);
        _interactable.selectEntered.AddListener(OnGrab);
    }

    private void OnDisable()
    {
        if (MountingZones.Length > 0) _assembly?.UnregisterZoneOwner(this);
        ClearZonePreview();
        if (_interactable == null) return;
        _interactable.hoverFilters.Remove(this);
        _interactable.selectFilters.Remove(this);
        _interactable.selectExited.RemoveListener(OnReleased);
        _interactable.selectEntered.RemoveListener(OnGrab);
    }

    private void OnGrab(SelectEnterEventArgs arg0)
    {
        ZoneHandledRelease = false;
        _previousZone = transform.parent != null ? transform.parent.GetComponent<MountingZoneView>() : null;
        _previousZonePosition = transform.localPosition;
        _previousZoneRotation = transform.localRotation;
        _startPosition = transform.position;
        _startRotation = transform.rotation;
        _eventBus?.Publish(new PartGrabStartedEvent { InstanceId = InstanceId });
        if (_zonePreviewRoutine == null && _assembly != null && _eventBus != null)
            _zonePreviewRoutine = StartCoroutine(TrackZoneWhileGrabbed());
    }

    private void OnReleased(SelectExitEventArgs arg0)
    {
        if (IsGrabbed || _eventBus == null) return;
        if (!arg0.isCanceled && _assembly != null &&
            _assembly.TryFindMountingZone(this, out var zone, out _, out bool valid))
        {
            ZoneHandledRelease = true;
            if (valid)
                RequestZonePlacement(zone, false);
            else if (_previousZone == zone)
                RequestZonePlacement(zone, true);
        }
        else if (arg0.isCanceled && _previousZone != null)
        {
            RequestZonePlacement(_previousZone, true);
        }
        _previousZone = null;
        ClearZonePreview();


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
        return _sockets != null && socketId != null && _sockets.TryGetValue(socketId, out var socket)
            ? socket
            : null;
    }

    public MountingZoneView GetMountingZone(string mountId)
    {
        foreach (var zone in MountingZones)
            if (zone != null && zone.MountId == mountId) return zone;
        return null;
    }

    private IEnumerator TrackZoneWhileGrabbed()
    {
        while (IsGrabbed)
        {
            yield return null;
            if (IsGrabbed) UpdateZonePreview();
        }
        _zonePreviewRoutine = null;
    }

    private void UpdateZonePreview()
    {
        if (!_assembly.TryFindMountingZone(this, out var zone, out var position, out bool valid))
        {
            _zonePreview?.SetVisible(false);
            SetSocketPreviewsVisible(true);
            return;
        }

        SetSocketPreviewsVisible(false);
        _zonePreview ??= new SocketPreviewSystem(
            Resources.Load<Material>("Materials/GreenTrtansparent"),
            Resources.Load<Material>("Materials/FakeTransparentRed"));
        var pose = new Pose(zone.transform.TransformPoint(position), zone.transform.rotation);
        if (!_zonePreviewCreated)
        {
            _zonePreview.ShowPreview(this, pose, valid);
            _zonePreviewCreated = true;
        }
        else
        {
            _zonePreview.UpdatePreview(pose, valid);
            _zonePreview.SetVisible(true);
        }
    }

    private void ClearZonePreview()
    {
        if (_zonePreviewRoutine != null) StopCoroutine(_zonePreviewRoutine);
        _zonePreviewRoutine = null;
        _zonePreview?.HidePreview();
        _zonePreviewCreated = false;
    }

    private void SetSocketPreviewsVisible(bool visible)
    {
        foreach (var socket in _hoveredSockets)
            if (socket != null) socket.SetPreviewVisible(visible);
    }

    private void RequestZonePlacement(MountingZoneView zone, bool restore)
    {
        _eventBus.Publish(new PartZoneAttachRequest
        {
            PartInstanceId = InstanceId,
            ParentInstanceId = zone.Owner.InstanceId,
            MountId = zone.MountId,
            RestorePreviousPosition = restore,
            Timestamp = DateTime.UtcNow
        });
    }

    internal bool TryGetPreviousZonePose(MountingZoneView zone, out Pose pose)
    {
        pose = new Pose(_previousZonePosition, _previousZoneRotation);
        return _previousZone == zone;
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

        _socketsByType = new Dictionary<PartType, SocketView>();



        foreach (var socket in _sockets.Values)
        {
            foreach (var allowedtype in socket.AllowedTypes)
            {
                _socketsByType[allowedtype] = socket;
            }
        }
    }

    public SocketView GetSocketByType(PartType type)
    {
        return _socketsByType.TryGetValue(type, out var socket)
            ? socket
            : null;
    }

    public void Init(string instanceId,IEventBus eventBus, bool hasConstantMaterial = false)
    {

        Debug.Log($"!!!!PArt Initialized with ID  {instanceId}");
        InstanceId = instanceId;
        _mpb ??= new MaterialPropertyBlock();
        HasConstantMaterial = hasConstantMaterial;
        _renderers = GetComponentsInChildren<Renderer>(true);
        _materialSlots = new Material[_renderers.Length][];
        for (int i = 0; i < _renderers.Length; i++)
            _materialSlots[i] = _renderers[i].sharedMaterials;
        if (HasConstantMaterial)
        {
            _prefabPropertyBlocks = new MaterialPropertyBlock[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _prefabPropertyBlocks[i] = new MaterialPropertyBlock[_materialSlots[i].Length];
                for (int j = 0; j < _materialSlots[i].Length; j++)
                {
                    var block = new MaterialPropertyBlock();
                    _renderers[i].GetPropertyBlock(block, j);
                    _prefabPropertyBlocks[i][j] = block;
                }
            }
        }
        _renderer = _renderers[0];
        _color = _renderer.sharedMaterial != null ? _renderer.sharedMaterial.color : Color.white;
        _eventBus = eventBus;
        InitializeSockets();
        MountingZones = GetComponentsInChildren<MountingZoneView>(true);
        foreach (var zone in MountingZones)
            zone.Initialize(this);
        if (MountingZones.Length > 0 && isActiveAndEnabled)
            _assembly?.RegisterZoneOwner(this);

        HasMountingBounds = false;
        foreach (var renderer in _renderers)
        {
            if (!renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy)
                continue;

            var bounds = MountingBounds.Transform(renderer.localBounds,
                transform.worldToLocalMatrix * renderer.localToWorldMatrix);
            if (HasMountingBounds) LocalMountingBounds = Combine(LocalMountingBounds, bounds);
            else LocalMountingBounds = bounds;
            HasMountingBounds = true;
        }
    }

    private static Bounds Combine(Bounds first, Bounds second)
    {
        first.Encapsulate(second);
        return first;
    }

    public void ApplyPreviewSmoothness(float value)
    {
        if (HasConstantMaterial) return;
        foreach (var renderer in _renderers)
        {
            renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(ShaderIds.Smoothness, value);
            renderer.SetPropertyBlock(_mpb);
        }
    }

    public void ApplyPreviewMetallness(float value)
    {
        if (HasConstantMaterial) return;
        foreach (var renderer in _renderers)
        {
            renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(ShaderIds.Metallic, value);
            renderer.SetPropertyBlock(_mpb);
        }
    }

    // PREVIEW — вызывается каждый кадр
    public async Task ApplyVisualPreview(PartVisualProperties visual)
    {
        if (HasConstantMaterial) return;

        Debug.Log($"mmmmmmmTry get visual.MaterialId {visual.MaterialId}");

        MaterialDefinition def = _materialRegistry.Get(visual.MaterialId);  // Сначала получаем Definition материала

        Debug.Log($"mmmmmmmmmmMaterialDefinition name {def.DisplayName}");


        Material mat = await _assets.Load<Material>(def.MaterialReference);   //  Загружаем по ссылке
        if (this == null || mat == null) return;



        //Material mat = await _assets.Load<Material>(visual.MaterialAddress); // Загружаем по адресу

        Debug.Log($"mmmmmmm!!!!!!!material Loaded {mat!= null}");




        for (int i = 0; i < _renderers.Length; i++)
        {
            for (int j = 0; j < _materialSlots[i].Length; j++) _materialSlots[i][j] = mat;
            _renderers[i].sharedMaterials = _materialSlots[i];
        }
        _color = visual.Color == null ? _renderer.sharedMaterial.color : visual.Color;

        Debug.Log($"mmmmmmmmmmvisual.Color is {visual.Color}");

        Debug.Log($"mmmmmmmmmm_color is {_color}");
        foreach (var renderer in _renderers)
        {
            renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(ShaderIds.BaseColor, _color);
            _mpb.SetFloat(ShaderIds.Smoothness, visual.Smoothness);
            _mpb.SetFloat(ShaderIds.Metallic, visual.Metallic);
            renderer.SetPropertyBlock(_mpb);
        }


    }

    // COMMIT — когда состояние подтверждено
    public void ApplyVisualCommitted(PartVisualProperties visual)
    {
        ApplyVisualPreview(visual);
    }

    public void Highlight(bool on)
    {
        if (HasConstantMaterial)
        {
            if (highlighted == on) return;
            highlighted = on;
            for (int i = 0; i < _renderers.Length; i++)
            for (int j = 0; j < _materialSlots[i].Length; j++)
            {
                var mat = _materialSlots[i][j];
                if (mat == null || !mat.HasProperty(ShaderIds.BaseColor)) continue;
                if (on)
                {
                    _renderers[i].GetPropertyBlock(_mpb, j);
                    _mpb.SetColor(ShaderIds.BaseColor, mat.GetColor(ShaderIds.BaseColor) * 1.2f);
                    _renderers[i].SetPropertyBlock(_mpb, j);
                }
                else
                {
                    var original = _prefabPropertyBlocks[i][j];
                    _renderers[i].SetPropertyBlock(original.isEmpty ? null : original, j);
                }
            }
            return;
        }

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
       
        foreach (var renderer in _renderers)
        {
            renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(ShaderIds.BaseColor, highlighted ? highlightedColor : _color);
            renderer.SetPropertyBlock(_mpb);
        }
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
