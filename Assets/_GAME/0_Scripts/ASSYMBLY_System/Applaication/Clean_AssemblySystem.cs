using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Zenject;

public class Clean_AssemblySystem : IInitializable
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
    private Dictionary<string, DroneDomainState> _drones = new Dictionary<string, DroneDomainState>();

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
        _eventBus.Subscribe<ApplyPartVisualCommand>(OnApplyPartVisual);


        _undoRedo = new UndoRedoService(
                   capture: BuildSaveData,
                   restore: LoadSaveData);

        _undoRedo.Initialize();

        
        SubscribesForSnapshots();

        Debug.Log($"---------Application.persistentDataPath {Application.persistentDataPath}");
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

        var partDomain = GetDomainState(request.PartInstanceId);
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
        var partDomain = GetDomainState(instanceId);

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
        var partState = GetDomainState(command.InstanceId);
        partState.SetVisual(command.Visual);

        _eventBus.Publish(new PartVisualChangedEvent(
            command.InstanceId,
            command.Visual
        ));
    }

    private void OnDeleteRequested(Clean_DeletePartRequest @event)
    {
        //Debug
            var domainState = GetDomainState(@event.InstanceId);
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
        //_eventBus.Unsubscribe<Clean_CreatePartRequestEvent>(OnCreateRequested);
    }

    // Пример доступа к состоянию
    public PartDomainState GetDomainState(string instanceId)
    {
        return _parts[instanceId];
    }


    #region DroneRebuild

    private void RebuildDrones() /// todo Оптимизировать .Сделать кэш для быстрого поиска
    {
        _drones.Clear();

        HashSet<string> visited = new();

        int droneIndex = 0;

        // ИЩЕМ ROOT PARTS
        foreach (var part in _parts.Values)
        {
            // root = не прикреплен ни к чему
            if (part.AttachedPartInstanceId != null)
                continue;

            // уже обработан
            if (visited.Contains(part.InstanceId))
                continue;

            DroneDomainState drone =
                new($"Drone_{droneIndex++}");

            BuildDroneRecursive(
                rootPart: part,
                drone: drone,
                visited: visited);

            // если нужен body-only drone
            bool hasBody =
                drone.partInstanseIds.Any(id =>
                    _parts[id].Type == PartType.Body);

            if (!hasBody)
                continue;

            CalculateDroneStats(drone);

            _drones.Add(drone.InstanceId, drone);

        }
            Debug.Log($"+++RebuildDrones  {this}");
            foreach (var oneDrone in _drones.Values)
            {

                Debug.Log($"++++ drone.Name {oneDrone.Name}");
                foreach (var partId in oneDrone.partInstanseIds)
                {
                    _viewRegistry.TryGet(partId, out var view);

                    Debug.Log($"+++Деталь {view.name}");
                }
            }
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

        // ИЩЕМ ДЕТЕЙ
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
    private void CalculateDroneStats(
        DroneDomainState drone)
    {
        float mass = 0f;

        Debug.Log($"+++CalculateDroneStats {this}");

        foreach (var partInstanseId in drone.partInstanseIds)
        {

            Debug.Log($"+++partId {partInstanseId}");
            var domain = _parts[partInstanseId];
            var config = _configs.Get(domain.PartId);
            mass += config.Mass;
        }

        drone.TotalMass = mass;

        Debug.Log($"+++drone.TotalMass {drone.TotalMass}");
    } 
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

            var domainState = GetDomainState(child);

            Debug.Log($"DDDDDDDDDDD child  {name} - Domain found == {domainState  != null} ");


        }

        foreach (string childId in allChildIds)
        {
            var domainState = GetDomainState(childId);

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


        var oldDomain = GetDomainState(instanceId);
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

            if (found) Debug.Log($"000000view found {state.InstanceId} {found} {view.name}");
            else Debug.Log($"View with ID {state.InstanceId} NOT found ");

            var data = PartMapper.ToSaveData(state, view.transform);

            result.Parts.Add(data);
        }

        return result;
    }


    public async void LoadSaveData(AssemblySaveData saveData)
    {

        if (saveData == null)
            throw new ArgumentNullException(nameof(saveData));

        // 0
        ClearCurrentAssembly();

        // 1
        var domains = BuildDomain(saveData);

        // 2
        var views = await CreateViews(domains);

        // 3
        BindDomain(domains);

        // 4
        ApplyState(saveData, views);

        // 5
        PostInitialize();
    }

    private void ClearCurrentAssembly()
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
