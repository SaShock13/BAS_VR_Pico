using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class Clean_AssemblySystem : IInitializable
{
    private readonly IEventBus _eventBus;
    private readonly IPartConfigRepository _repository;
    private readonly IPartFactory _factory;
    private readonly PartViewRegistry _viewRegistry;
    private readonly Transform _spawnPoint;
    private readonly ISocketResolver _socketResolver;
    private readonly ISaveService _saveService;

    // ХРАНИЛИЩЕ СОСТОЯНИЙ
    private readonly Dictionary<string, PartDomainState> _parts =
        new Dictionary<string, PartDomainState>();


    private UndoRedoService _undoRedo;

    public Clean_AssemblySystem(
        IEventBus eventBus,
        IPartConfigRepository repository,
        IPartFactory factory,
        PartViewRegistry viewRegistry,
        ISocketResolver socketResolver,
        ISaveService saveService)
    {
        _eventBus = eventBus;
        _repository = repository;
        _factory = factory;
        _viewRegistry = viewRegistry;
        _socketResolver = socketResolver;
        _saveService = saveService;
    }

    public void Initialize()
    {
        _eventBus.Subscribe<Clean_CreatePartRequestEvent>(OnCreateRequested);
        _eventBus.Subscribe<Clean_DeletePartRequest>(OnDeleteRequested);
        _eventBus.Subscribe<Clean_DuiblicatePartRequest>(OnDublicateRequested);
        _eventBus.Subscribe<ApplyPartVisualCommand>(OnApplyPartVisual);


        _undoRedo = new UndoRedoService(
                   capture: BuildSaveData,
                   restore: LoadSaveData);

        _undoRedo.Initialize();

        
        SubscribesForSnapshots();

        Debug.Log($"---------Application.persistentDataPath {Application.persistentDataPath}");
    }


    private void SubscribesForSnapshots()
    {
        _eventBus.Subscribe<Clean_PartCreatedEvent>(OnPartChanged);
        _eventBus.Subscribe<Clean_PartDeletedEvent>(OnPartChanged);

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
        _undoRedo.Record();
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
        DeletePart(@event.InstanceId);
    }

    private void OnCreateRequested(Clean_CreatePartRequestEvent @event)
    {
        Debug.Log($"_repository {_repository!=null}");
        Debug.Log($"@event PartId {@event.PartId != null}");
         CreatePart(@event.PartId);
    }

    public void Dispose()
    {
        //_eventBus.Unsubscribe<Clean_CreatePartRequestEvent>(OnCreateRequested);
    }


    private void CreatePart(string partId)
    {
        // 1. Генерация ID экземпляра
        string instanceId = System.Guid.NewGuid().ToString();

        // 2. Создание доменного состояния
        PartDomainState domainState = new PartDomainState(instanceId, partId);
        _parts.Add(instanceId, domainState);

        // 3. Получение конфигурации
        PartConfig config = _repository.Get(partId);

        // 4. Создание Unity-объекта
        GameObject go = _factory.Create(
            config,
            Vector3.zero,
            Quaternion.identity
        );

        // 5. Инициализация и связь Unity ↔ Domain
        var view = go.AddComponent<DronePartView>();
        view.Init(instanceId);


        // 6. Уведомление
        _eventBus.Publish(new Clean_PartCreatedEvent {InstanceId = instanceId, GameObject = go,Timestamp = DateTime.Now } );
    }

    private void DeletePart(string instanceId)
    {
        var domainState = GetDomainState(instanceId);
        if (domainState != null ) 
        {
            _parts.Remove(instanceId); 

            // todo Обработка ошибок
            _eventBus.Publish(new Clean_PartDeletedEvent { InstanceId = instanceId, Timestamp = DateTime.Now } );
        }
        else
        {
            _eventBus.Publish(new Clean_PartCantBeDeletedEvent {InstanceId = instanceId,Timestamp = DateTime.Now } );

        }


    }

    private void DublicatePart(string instanceId)
    {

        if (!_viewRegistry.TryGet(instanceId, out var sourceView))
            return;

        Vector3 spawnPos = sourceView.transform.position + Vector3.up * 0.2f;
        Quaternion spawnRot = sourceView.transform.rotation;

        // 1. Генерация ID экземпляра
        string dublicateInstanceId = Guid.NewGuid().ToString();



        var partId = GetDomainState(instanceId).PartId;


        // 2. Создание доменного состояния
        PartDomainState domainState = new PartDomainState(dublicateInstanceId, partId);

        _parts.Add(dublicateInstanceId, domainState);

        // 3. Получение конфигурации
        PartConfig config = _repository.Get(partId);

        // 4. Создание Unity-объекта
        GameObject go = _factory.Create(
            config,
            spawnPos,
            spawnRot
        );

        // 5. Инициализация и связь Unity ↔ Domain
        var view = go.AddComponent<DronePartView>();
        view.Init(dublicateInstanceId);


        // 6. Уведомление
        _eventBus.Publish(new Clean_PartCreatedEvent { InstanceId = dublicateInstanceId, GameObject = go, Timestamp = DateTime.Now });
    }


    // Пример доступа к состоянию
    public PartDomainState GetDomainState(string instanceId)
    {
        return _parts[instanceId];
    }


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


    public AssemblySaveData BuildSaveData()
    {
        var result = new AssemblySaveData();

        foreach (var state in _parts.Values)
        {
            //DronePartView view = new DronePartView();

            _viewRegistry.TryGet(state.InstanceId, out DronePartView view);

            var data = PartMapper.ToSaveData(state, view.transform);
            result.Parts.Add(data);
        }

        return result;
    }

    #region LoadData

    public void LoadSaveData(AssemblySaveData saveData)
    {

        if (saveData == null)
            throw new ArgumentNullException(nameof(saveData));

        // 0
        ClearCurrentAssembly();

        // 1
        var domains = BuildDomain(saveData);

        // 2
        var views = CreateViews(domains);

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
            result.Add(domain.InstanceId, domain);
        }

        return result;
    }

    private Dictionary<string, DronePartView> CreateViews(
    Dictionary<string, PartDomainState> domains)
    {
        var result = new Dictionary<string, DronePartView>();

        foreach (var pair in domains)
        {
            var domain = pair.Value;

            var config = _repository.Get(domain.PartId);

            var go = _factory.Create(config, Vector3.zero, Quaternion.identity);

            var view = go.GetComponent<DronePartView>();
            if (view == null)
                view = go.AddComponent<DronePartView>();

            view.Init(domain.InstanceId);

            result.Add(domain.InstanceId, view);
            _viewRegistry.Register(domain.InstanceId, view.gameObject);
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

            PartMapper.ApplyToView(partData, view, _socketResolver);
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
