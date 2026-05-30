using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Zenject;

public class Clean_AssemblySystem : IInitializable, IAssemblyQuery
{
    private readonly IEventBus _eventBus;
    private readonly IAppLogger _logger;
    private readonly IPartConfigRegistry _configs;
    private readonly IPartFactory _factory;
    private readonly PartViewRegistry _viewRegistry;
    private readonly Transform _spawnPoint;
    private readonly ISocketResolver _socketResolver;
    private readonly ISaveService _saveService;
    private SelectionService _selectionService;

    // ХРАНИЛИЩЕ СОСТОЯНИЙ
    private readonly Dictionary<string, PartDomainState> _parts = new Dictionary<string, PartDomainState>();

    // drone state
    private Dictionary<string, DroneDomainState> _drones = new Dictionary<string, DroneDomainState>();
    // drone persistent metadata
    private readonly Dictionary<string, DroneMetadata> _dronesMetadata = new();
    // drone computed
    private readonly Dictionary<string, DroneComputedState> _droneComputed = new();

    private UndoRedoService _undoRedo;
    private DiContainer _container;

    private const string DEFAULT_PART_MATERIAL = "DefaultBlackMat";

    public Clean_AssemblySystem(
        IEventBus eventBus,
        IAppLogger logger,
        IPartConfigRegistry configs,
        IPartFactory factory,
        PartViewRegistry viewRegistry,
        DiContainer container,
        ISocketResolver socketResolver,
        SelectionService selectionService,
        ISaveService saveService)
    {
        _eventBus = eventBus;
        _logger = logger;
        _configs = configs;
        _factory = factory;
        _viewRegistry = viewRegistry;
        _container = container;
        _socketResolver = socketResolver;
        _selectionService = selectionService;
        _saveService = saveService;
    }

    public void Initialize()
    {
        _eventBus.Subscribe<Clean_CreatePartRequestEvent>(OnCreateRequested);
        _eventBus.Subscribe<Clean_DeletePartRequest>(OnDeleteRequested);
        _eventBus.Subscribe<Clean_DuiblicatePartRequest>(OnDublicateRequested);
        _eventBus.Subscribe<PartSocketAttachRequest>(OnAttachRequested);
        _eventBus.Subscribe<PartSocketDetachRequest>(OnDetachRequested);
        _eventBus.Subscribe<ApplyPartVisualCommand>(OnApplyPartVisual);
        _eventBus.Subscribe<PartTransformChangedEvent>(OnPartTransformChanged);


        _undoRedo = new UndoRedoService(
                   capture: BuildSaveData,
                   restore: LoadSaveData);

        _undoRedo.Initialize();

        
        SubscribesForSnapshots();

        Debug.Log($"---------Application.persistentDataPath {Application.persistentDataPath}");
    }

    private void OnPartTransformChanged(PartTransformChangedEvent @event)  /// todo по несколько раз сохраняется. Нужно разделить события чтобы не дублировалось сохранение.
    {
        //var view = _viewRegistry.Get(@event.instanceId);

        //if (view == null)
        //    return;

        bool changed =
            Vector3.Distance(@event.StartPosition, @event.position) > 0.01f ||
            Quaternion.Angle(@event.StartRotation, @event.rotation) > 0.1f;

        if (!changed)
            return;

        var part = _parts[@event.instanceId];

        if (part.LifecycleState == PartLifecycleState.Installed)
            return;

        _undoRedo.Record();
    }



    /// <summary>
    /// Подписки на события при которых состояние сборки сохраняется для истории отмены
    /// </summary>
    private void SubscribesForSnapshots()
    {
        _eventBus.Subscribe<AssemblyChangedEvent>(OnPartChanged);  
        //_eventBus.Subscribe<Clean_PartDeletedEvent>(OnPartChanged);
        _eventBus.Subscribe<PartVisualChangedEvent>(OnPartChanged);

    }


    public void Undo()
    {
        _undoRedo.Undo();
    }

    public void Redo()
    {
        _undoRedo.Redo();
    }


    private void OnPartChanged(object _)
    {

        Debug.Log($"_undoRedo {_undoRedo!= null}");
        _undoRedo.Record();

    }




    private void OnAttachRequested(PartSocketAttachRequest request)
    {
        Debug.Log($"OnAttachRequested {this}");

        var partDomain = GetPartDomainState(request.PartInstanceId);
        Debug.Log($"partDomain {partDomain}");
        _viewRegistry.TryGet(partDomain.InstanceId, out var partView);
        Debug.Log($"partView {partView}");

        _viewRegistry.TryGet(request.AttachedPartId, out var attachedView);
        Debug.Log($"attachedView {attachedView}");

        var attachedSocket = attachedView.GetSocket(request.AttachedSocketId);
        Debug.Log($"attachedSocket {attachedSocket}");
        if (IsCanAttach(partDomain, attachedSocket))
        {
            // Прикрепляем домен
            partDomain.AttachToPartSocket(request.AttachedPartId, request.AttachedSocketId);


            // Прикрепляем view
            partView.AttachTo(attachedSocket.transform); /// TODO Прикреплять нужно во вью по событию!


            // Пересчитываем дроны
           RebuildDrones();

            _eventBus.Publish(new PartSocketAttachedEvent() { Timestamp = DateTime.Now });

            _eventBus.Publish(new AssemblyChangedEvent { Timestamp = DateTime.Now });  // для Снапшота
        }
        else Debug.Log($"!!!!!! {partView.transform.name} Can NOT be Attached {this}");



    }


    private void OnDetachRequested(PartSocketDetachRequest request)
    {
        Debug.Log($"OnDetachRequested {this}");

        var partDomain = GetPartDomainState(request.PartInstanceId);
        Debug.Log($"Detach partDomain {partDomain}");


        _viewRegistry.TryGet(partDomain.InstanceId, out var partView);
        Debug.Log($"Detach partView {partView}");

        

        partDomain.Detach();
        partView.Detach();

        // Пересчитываем дроны
        RebuildDrones();

        _eventBus.Publish(new PartSocketDetachedEvent() { Timestamp = DateTime.Now });

        //_eventBus.Publish(new AssemblyChangedEvent { Timestamp = DateTime.Now });  // для Снапшота
    }



    public bool IsInHands(DronePartView part)
    {
        if (part == null)
            return false;

        XRGrabInteractable grab =
            part.GetComponent<XRGrabInteractable>();

        if (grab == null)
            return false;

        return grab.isSelected;
    }


    /// <summary>
    /// Публичный метод проверки совместимости
    /// </summary>
    /// <param name="instanceId"></param>
    /// <param name="socketView"></param>
    /// <returns></returns>

    public bool CanAttach(string instanceId, SocketView socketView)
    {
        var partDomain = GetPartDomainState(instanceId);

        if (IsSocketOccupied(socketView.ParentView.InstanceId, socketView.SocketId)) // todo как получить здесь instanceId детали с сокетом? Как еще проверить на занятость сокет?
        {

            Debug.Log($"33333 Socket is already Occupied!!!! {this}");
            return false;
        }
        Debug.Log($"33333 Socket is FREE!!!! {this}");

        return IsCanAttach(partDomain, socketView);
    }


    public bool IsSocketOccupied(string AttachedPartInstanceId, string socketId) // todo   оптимизировать проверку
    {
        return _parts.Values.Any(p =>
            p.AttachedPartInstanceId == AttachedPartInstanceId &&
            p.AttachedSocketId == socketId);
    }

    /// <summary>
    /// пРОХОДИМ ПО ВСЕМ ТИПАМ СОКЕТА, Если хоть один совпадает = true
    /// </summary>
    /// <param name="partDomain"></param>
    /// <param name="attachedSocket"></param>
    /// <returns></returns>
    private  bool IsCanAttach(PartDomainState partDomain, SocketView attachedSocket) 
    {

        var childConfig =
        _configs.Get(partDomain.PartId);        

        foreach (var allowedType in attachedSocket.AllowedTypes)
        {
            if (allowedType == childConfig.PartType) return true;

        }
        return false;
    }

    private void OnDublicateRequested(Clean_DuiblicatePartRequest @event)
    {
        DublicatePart(@event.InstanceId);
    }

    private void OnApplyPartVisual(ApplyPartVisualCommand command)
    {
        var partState = GetPartDomainState(command.InstanceId);
        partState.SetVisual(command.Visual);

        _eventBus.Publish(new PartVisualChangedEvent(
            command.InstanceId,
            command.Visual
        ));
    }

    private void OnDeleteRequested(Clean_DeletePartRequest @event)
    {
        //Debug
            var domainState = GetPartDomainState(@event.InstanceId);
        if(domainState!= null)
        {
            _viewRegistry.TryGet(@event.InstanceId, out var view);
            var name = view.name;



            Debug.Log($"DDDDDDDDDDDOnDeleteRequested {name}  - Domain found == {domainState != null} ");
            DeletePart(@event.InstanceId);
        }
        else Debug.Log($"domainState == null {this}");

    }

    private void OnCreateRequested(Clean_CreatePartRequestEvent @event)
    {
        Debug.Log($"_repository {_configs!=null}");
        Debug.Log($"@event PartId {@event.PartId != null}");
        CreatePartAsync(@event.PartId);

        Debug.Log($"000000  Возврат в OnCreateRequested {this}");
    }

    public void Dispose()
    {
       // _eventBus.Unsubscribe<Clean_CreatePartRequestEvent>(OnCreateRequested);
    }

    // доступа к состоянию
    public PartDomainState GetPartDomainState(string instanceId)
    {
        return _parts[instanceId];
    }

    public DroneDomainState GetDroneDomainState(string instanceId)
    {
        return _drones[instanceId];
    }



    internal void RemoveDrone(string instanceId)
    {
        _drones.Remove(instanceId);

        DeletePart(instanceId);

        RebuildDrones();
    }


    #region DroneRebuild

    public void RebuildDrones() /// todo Оптимизировать .Сделать кэш для быстрого поиска
    {
        Debug.Log($"0000000RebuildDrones ");  // todo Имя дрона Всегда новое получается. Но нужно чтобы назвать дрон и закрепить это имя!!

        _drones.Clear();
        _droneComputed.Clear();

        HashSet<string> visited = new();

        int autoNameIndex = 0;

        foreach (var part in _parts.Values)
        {
            // ROOT = BODY без родителя
            if (part.Type != PartType.Body)
                continue;

            if (part.AttachedPartInstanceId != null)
                continue;

            if (visited.Contains(part.InstanceId))
                continue;

            // STABLE ID
            string droneId = part.InstanceId;

            DroneDomainState drone =
                new(droneId);

            BuildDroneRecursive(
                rootPart: part,
                drone: drone,
                visited: visited);

            // записываем droneId деталям
            foreach (var partId in drone.partInstanseIds)
            {
                _parts[partId].DroneId = droneId;
            }

            _drones.Add(droneId, drone);

            EnsureMetadataExists(
                droneId,
                autoNameIndex++);

            //EnsureRuntimeExists(droneId);

            RecalculateComputed(drone);

            DebugDrone(drone);
        }

        Debug.Log($"0000000=== FOUND {_drones.Count} DRONES ===");
    }

    public void RenameDrone(
        string droneId,
        string newName)
    {

            Debug.Log($"0000000TRY RenameDrone {this}");
        if (_dronesMetadata.TryGetValue(droneId, out var metadata))
        {
            metadata.Name = newName;


            Debug.Log($"000000000RenameDrone {this}");
        }
    }

    public string GetDroneName(string droneId)
    {
        if (_dronesMetadata.TryGetValue(droneId, out var metadata))
            return metadata.Name;

        return "Unknown";
    }

    public DroneComputedState GetComputed(string droneId)
    {
        _droneComputed.TryGetValue(droneId, out var computed);

        return computed;
    }

    private void BuildDroneRecursive(
        PartDomainState rootPart,
        DroneDomainState drone,
        HashSet<string> visited)
    {
        if (visited.Contains(rootPart.InstanceId))
            return;

        visited.Add(rootPart.InstanceId);

        drone.partInstanseIds.Add(rootPart.InstanceId);

        foreach (var part in _parts.Values)
        {
            if (part.AttachedPartInstanceId ==
                rootPart.InstanceId)
            {
                BuildDroneRecursive(
                    part,
                    drone,
                    visited);
            }
        }
    }

    #endregion

    #region COMPUTED

    private void RecalculateComputed(
        DroneDomainState drone)
    {
        DroneComputedState computed =
            new()
            {
                DroneId = drone.InstanceId
            };

        float totalMass = 0f;
        float totalThrust = 0f;

        foreach (var partId in drone.partInstanseIds)
        {
            PartDomainState part = _parts[partId];

            var config = _configs.Get(part.PartId);

            //if (!_configMap.TryGetValue(
            //        part.PartId,
            //        out var config))
            //{
            //    continue;
            //}

            totalMass += config.Mass;

            //if (part.Type == PartType.Motor)
            //{
            //    totalThrust += config.MaxThrust;
            //}
        }

        computed.TotalMass = totalMass;
        computed.TotalThrust = totalThrust;

        if (totalMass > 0.01f)
        {
            computed.PowerToWeight =
                totalThrust / totalMass;
        }

        _droneComputed[drone.InstanceId] = computed;
    }

    #endregion

    #region METADATA

    private void EnsureMetadataExists(
        string droneId,
        int autoIndex)
    {
        if (_dronesMetadata.ContainsKey(droneId))
            return;

        DroneMetadata metadata =
            new()
            {
                DroneId = droneId,
                Name = $"Drone_{autoIndex}"
            };

        _dronesMetadata.Add(droneId, metadata);
    }

    #endregion

    //#region RUNTIME

    //private void EnsureRuntimeExists(
    //    string droneId)
    //{
    //    if (_runtime.ContainsKey(droneId))
    //        return;

    //    DroneRuntimeState runtime =
    //        new()
    //        {
    //            DroneId = droneId
    //        };

    //    _runtime.Add(droneId, runtime);
    //}

    //#endregion

    #region DEBUG

    private void DebugDrone(
        DroneDomainState drone)
    {
        string name =
            GetDroneName(drone.InstanceId);

        DroneComputedState computed =
            GetComputed(drone.InstanceId);

        Debug.Log(
            $"0000000000DRONE: {name}");

        Debug.Log(
            $"0000000Mass: {computed.TotalMass}");

        Debug.Log(
            $"00000000000Thrust: {computed.TotalThrust}");

        Debug.Log(
            $"00000000PTW: {computed.PowerToWeight}");

        foreach (var partId in drone.partInstanseIds)
        {
            Debug.Log(
                $"0000000000000PART: {partId}");
        }
    }
     

    //private void BuildDroneRecursive(
    //PartDomainState rootPart,
    //DroneDomainState drone,
    //HashSet<string> visited)
    //{
    //    if (visited.Contains(rootPart.InstanceId))
    //        return;

    //    visited.Add(rootPart.InstanceId); 

    //    drone.partInstanseIds.Add(rootPart.InstanceId);




    //    //part.RootInstanceId = rootPart.InstanceId; // задаем корневую деталь всем. НЕ ПРАВИЛЬНО РАБОТАЕТ
    //    //rootPart.DroneId = drone.InstanceId;

    //    //Debug.Log($"!!!!!!!! PArt {rootPart.InstanceId} is in drone {drone.InstanceId}");

    //    // ИЩЕМ ДЕТЕЙ
    //    foreach (var part in _parts.Values)
    //    {

    //        if (part.AttachedPartInstanceId ==
    //            rootPart.InstanceId)
    //        {
    //            BuildDroneRecursive(
    //                part,
    //                drone,
    //                visited);
    //        }

    //    }
    //}
    //private void CalculateDroneStats(
    //    DroneDomainState drone)
    //{
    //    float mass = 0f;

    //    Debug.Log($"+++CalculateDroneStats {this}");

    //    foreach (var partInstanseId in drone.partInstanseIds)
    //    {

    //        Debug.Log($"+++partId {partInstanseId}");
    //        var domain = _parts[partInstanseId];
    //        var config = _configs.Get(domain.PartId);
    //        mass += config.Mass;
    //    }

    //    drone.TotalMass = mass;
        

    //    Debug.Log($"+++drone.TotalMass {drone.TotalMass}");
    //} 

    #endregion


    #region CRUD
    private async Task CreatePartAsync(string partId)
    {
        // 1. Генерация ID экземпляра
        string instanceId = System.Guid.NewGuid().ToString();

        // 3. Получение конфигурации
        PartConfig config = _configs.Get(partId);



        // 2. Создание доменного состояния

        var defaultVisual = new PartVisualProperties() {MaterialAddress = DEFAULT_PART_MATERIAL, Smoothness = 0.5f  };

        PartDomainState domainState = new PartDomainState(instanceId, partId, config.PartType , defaultVisual);
        _parts.Add(instanceId, domainState);


        //4.Создание Unity - объекта
        GameObject go = await _factory.CreateFromAddressables(
            config,
            Vector3.zero,
            Quaternion.identity
        );

        //GameObject go = _factory.Create(
        //    config,
        //    Vector3.zero,
        //    Quaternion.identity
        //);




        // 5. Инициализация и связь Unity ↔ Domain
        var view = go.AddComponent<DronePartView>();

        // Zenject Зависимости прокидывает
       _container.InjectGameObject(go);


        view.Init(instanceId,_eventBus);

        view.ApplyVisualCommitted(domainState.VisualProperties);

        // 6. Уведомление


        Debug.Log($"!!!!!!!instanceId {instanceId} go {go!= null} message {this} ");

        _viewRegistry.Register(instanceId, go);

        domainState.isLoaded = true;

        _eventBus.Publish(new Clean_PartCreatedEvent { InstanceId = instanceId, GameObject = go, Timestamp = DateTime.Now });  /// TOdo не успевает видимо создать GO или что то не так и уже снапшот делает.

    }

    // todo оптимизировать евенты для сохранений
    private void DeletePart(string instanceId)  // !!!!!!!todo Удаление делать также всех дочерних !!!! И  домены и вью. Желательно Ао доменным связям а не по вью-родительским.
    {

        _viewRegistry.TryGetAllChildrenIds(instanceId, out List<string> allChildIds);


        Debug.Log($"ddddddddballChildIds.Count {allChildIds.Count}");
        foreach (var child in allChildIds)
        {
            //debug
            _viewRegistry.TryGet(child, out var view);
            var name = view.name;

            var domainState = GetPartDomainState(child);

            Debug.Log($"DDDDDDDDDDD child  {name} - Domain found == {domainState  != null} ");


        }

        foreach (string childId in allChildIds)
        {
            var domainState = GetPartDomainState(childId);

            if (domainState != null)
            {
                //debug
                _viewRegistry.TryGet(childId, out var view);
                var name = view.name;

                _parts.Remove(childId);
                _logger.Log($"DDDDDDDDDDD RealDeleted {name} Domain");

               
                
                _viewRegistry.Remove(childId);
                Debug.Log($"DDDDDDDDDDD RealDeleted {name}  VIEW");




                Debug.Log($"message {this}");
                // todo Обработка ошибок
                _eventBus.Publish(new Clean_PartDeletedEvent { InstanceId = instanceId, Timestamp = DateTime.Now });
            }
            else
            {
                _eventBus.Publish(new Clean_PartCantBeDeletedEvent { InstanceId = instanceId, Timestamp = DateTime.Now });

            }

        }

        // Пересчитываем дроны
        RebuildDrones();

        _eventBus.Publish(new AssemblyChangedEvent { Timestamp = DateTime.Now });  // для Снапшота


    }

    private async void DublicatePart(string instanceId)
    {

        if (!_viewRegistry.TryGet(instanceId, out var sourceView))
            return;

        Vector3 spawnPos = sourceView.transform.position + Vector3.up * 0.2f;
        Quaternion spawnRot = sourceView.transform.rotation;

        // 1. Генерация ID экземпляра
        string dublicateInstanceId = Guid.NewGuid().ToString();


        var oldDomain = GetPartDomainState(instanceId);
        var partId = oldDomain.PartId;

        // 3. Получение конфигурации
        PartConfig config = _configs.Get(partId);

        // 2. Создание доменного состояния
        PartDomainState domainState = new PartDomainState(dublicateInstanceId, partId, config.PartType, oldDomain.VisualProperties);


        _parts.Add(dublicateInstanceId, domainState);

        


        // 4. Создание Unity-объекта
        GameObject go = await _factory.CreateFromAddressables(
            config,
            spawnPos,
            spawnRot
        );

        go.name = sourceView.transform.name + "_Clone";



        // 5. Инициализация и связь Unity ↔ Domain
        var view = go.AddComponent<DronePartView>();

        // Zenject Зависимости прокидывает
        _container.InjectGameObject(go);


        view.Init(dublicateInstanceId, _eventBus);

        _viewRegistry.Register(dublicateInstanceId, go); // Обязательно регистрировать

        // Пересчитываем дроны
         RebuildDrones(); // todo А надо ли ???

        domainState.isLoaded = true; /// Обязательно помечать, иначе не запишется в SaveData

        // Применяем визуал domain на view
        view.ApplyVisualCommitted(domainState.VisualProperties);

        // 6. Уведомление
        _eventBus.Publish(new Clean_PartCreatedEvent { InstanceId = dublicateInstanceId, GameObject = go, Timestamp = DateTime.Now });
    }

    #endregion


    #region Сохранения/Загрузка сцены

    public void Save()
    {
        var saveData = BuildSaveData();
        _saveService.Save(saveData);
    }

    public void Load()
    {
        var saveData = _saveService.Load();

        if (saveData == null || saveData.Parts.Count == 0)
        {
            Debug.LogWarning("Nothing to load");
            return;
        }

        LoadSaveData(saveData);

    }

    #endregion

    #region Build/LoadData

    public AssemblySaveData BuildSaveData() 
    {
        var result = new AssemblySaveData();



        foreach (var state in _parts.Values)
        {
            if (!state.isLoaded) continue;

            DronePartView view;

            var found = _viewRegistry.TryGet(state.InstanceId, out view);

            if (found) Debug.Log($"view found {state.InstanceId} {found} {view.name}");
            else Debug.Log($"View with ID {state.InstanceId} NOT found ");

            var data = PartMapper.ToSaveData(state, view.transform);

            result.Parts.Add(data);
        }

        return result;
    }

    public AssemblySaveData BuildSaveDataForDrone(string droneId)
    {
        var result = new AssemblySaveData();

        DroneDomainState drone = _drones[droneId];

        foreach (string partId in drone.partInstanseIds)
        {
            PartDomainState state = _parts[partId];

            if (!state.isLoaded)
                continue;

            if (!_viewRegistry.TryGet(state.InstanceId, out DronePartView view))
                continue;

            var data = PartMapper.ToSaveData(
                state,
                view.transform);

            result.Parts.Add(data);
        }

        return result;
    }


    public void LoadSaveData(AssemblySaveData saveData)
    {
        LoadSaveData(saveData, true);
    }


    //public async void LoadSaveData(AssemblySaveData saveData, bool clearBeforeLoad = true)
    //{

    //    if (saveData == null)
    //        throw new ArgumentNullException(nameof(saveData));

    //    // 0  -  если нужно очищаем
    //    if (clearBeforeLoad) ClearCurrentAssembly(); 

    //    // 1
    //    var domains = BuildDomain(saveData);

    //    // 2
    //    var views = await CreateViews(domains);

    //    // 3
    //    BindDomain(domains);

    //    // 4
    //    ApplyState(saveData, views);

    //    // 5
    //    PostInitialize();
    //}

    /// <summary>
    /// Метод загрузки для конструктора - с очищением сцены
    /// </summary>
    /// <param name="saveData"></param>
    /// <param name="clearBeforeLoad"></param>
    public async void LoadSaveData(
    AssemblySaveData saveData,
    bool clearBeforeLoad = true)
    {
        await InternalLoad(
            saveData,
            clearBeforeLoad,
            postInitialize: true);
    }

    /// <summary>
    /// Метод загрузки для Гаража, Аддитивно
    /// </summary>
    /// <param name="saveData"></param>
    /// <param name="spawnPosition"></param>
    /// <param name="postInitialize"></param>
    /// <returns></returns>
    public async Awaitable SpawnAssembly(
    AssemblySaveData saveData,
    Vector3 spawnPosition,
    bool postInitialize = false)
    {
        AssemblySaveData clone =
            CloneSave(saveData);

        MoveAssembly(
            clone,
            spawnPosition);

        await InternalLoad(
            clone,
            clearBeforeLoad: false,
            postInitialize: postInitialize);
    }

    /// <summary>
    /// Вызывать после создания пачки дронов. Чтобы не пересчитывать каждый раз
    /// </summary>
    public void FinishBatchLoad()
    {
        PostInitialize();
    }


    /// <summary>
    /// Внутренний метод загрузки деталей из сохранения
    /// </summary>
    /// <param name="saveData"></param>
    /// <param name="clearBeforeLoad"></param>
    /// <param name="postInitialize"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    private async Awaitable InternalLoad(
    AssemblySaveData saveData,
    bool clearBeforeLoad,
    bool postInitialize)
    {
        if (saveData == null)
            throw new ArgumentNullException(nameof(saveData));

        if (clearBeforeLoad)
        {
            ClearCurrentAssembly();
        }

        var domains = BuildDomain(saveData);

        var views = await CreateViews(domains);

        BindDomain(domains);

        ApplyState(saveData, views);

        if (postInitialize)
        {
            PostInitialize();
        }
    }

    private AssemblySaveData CloneSave(
    AssemblySaveData source)
    {
        string json =
            JsonUtility.ToJson(source);

        return JsonUtility
            .FromJson<AssemblySaveData>(json);
    }

    private void MoveAssembly(
    AssemblySaveData saveData,
    Vector3 targetPosition)
    {
        if (saveData.Parts.Count == 0)
            return;

        PartSaveData root =
            FindRootPart(saveData);

        Vector3 delta =
            targetPosition - root.Transform.Position;

        foreach (var part in saveData.Parts)
        {
            part.Transform.Position += delta;
        }
    }

    private PartSaveData FindRootPart(
    AssemblySaveData saveData)
    {
        foreach (var part in saveData.Parts)
        {
            if (part.Type != PartType.Body)
                continue;

            if (!string.IsNullOrEmpty(
                    part.AttachedPartId))
                continue;

            return part;
        }

        return saveData.Parts[0];
    }

    public void ClearCurrentAssembly()
    {
        foreach (var view in _viewRegistry.GetAllGOs())
        {
            UnityEngine.Object.Destroy(view);
        }

        _viewRegistry.Clear();
        _parts.Clear();
    }

    private Dictionary<string, PartDomainState> BuildDomain(AssemblySaveData saveData)
    {
        var result = new Dictionary<string, PartDomainState>();

        foreach (var partData in saveData.Parts)
        {
            var domain = PartMapper.ToDomain(partData);

            Debug.Log($"!!!!!!!!Color of {domain.InstanceId} is  {domain.VisualProperties.Color} LOADED");
            result.Add(domain.InstanceId, domain);
        }

        return result;
    }

    private async  Task<Dictionary<string, DronePartView>> CreateViews(
    Dictionary<string, PartDomainState> domains)
    {
        var result = new Dictionary<string, DronePartView>();

        foreach (var pair in domains)
        {
            var domain = pair.Value;

            var config = _configs.Get(domain.PartId);

            GameObject go = await _factory.CreateFromAddressables(config, Vector3.zero, Quaternion.identity);

            var view = go.GetComponent<DronePartView>();
            if (view == null)
                view = go.AddComponent<DronePartView>();

            // Zenject Зависимости прокидывает
            _container.InjectGameObject(go);

            view.Init(domain.InstanceId, _eventBus);


            result.Add(domain.InstanceId, view);
            _viewRegistry.Register(domain.InstanceId, view.gameObject);
            domain.isLoaded = true;
        }

        return result;
    }

    private void BindDomain(Dictionary<string, PartDomainState> domains)
    {
        foreach (var pair in domains)
        {
            _parts.Add(pair.Key, pair.Value);
        }
    }

    private void ApplyState(
    AssemblySaveData saveData,
    Dictionary<string, DronePartView> views)
    {
        foreach (var partData in saveData.Parts)
        {
            var view = views[partData.InstanceId];


            Debug.Log($"1111partData color  {partData.VisualProperties.Color}");

            PartMapper.ApplyToView(partData, view, _viewRegistry);
        }
    }

    private void PostInitialize()
    {
        RebuildDrones();

        // например:
        // - пересчёт физики
        // - перестройка связей
        // - уведомление UI

        //_eventBus.Publish(new AssemblyLoadedEvent
        //{
        //    Timestamp = DateTime.Now
        //});
    }

    


    #endregion


}
