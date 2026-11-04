using System;
using Zenject;

public sealed class AssemblyProjectMenuService : IInitializable, IDisposable
{
    private readonly IEventBus _bus;
    private readonly JsonSaveService _store = new JsonSaveService();
    private IDisposable _subscription;
    private IDisposable _navigationSubscription;
    private bool _starting;
    public AssemblyProjectMenuService(IEventBus bus) { _bus = bus; }
    public void Initialize()
    {
        _subscription = _bus.Subscribe<AssemblyProjectRequest>(Handle);
        _navigationSubscription = _bus.Subscribe<MenuNavigationStatusEvent>(status =>
        {
            if (!status.IsLoading) _starting = false;
        });
    }
    private void Handle(AssemblyProjectRequest request)
    {
        if (_starting) return;
        try
        {
            if (request.Operation == "list")
                _bus.Publish(new AssemblyProjectListEvent { Projects = _store.ListProjects() });
            else
            {
                if (request.Operation == "new") _store.CreateProject(request.Name);
                else if (request.Operation == "load") _store.SelectProject(request.Id);
                else return;
                _starting = true;
                _bus.Publish(new RequestStartConstructorEvent());
            }
        }
        catch (Exception e) { _starting = false; _bus.Publish(new AssemblyProjectListEvent { Error = e.Message }); }
    }
    public void Dispose() { _subscription?.Dispose(); _navigationSubscription?.Dispose(); }
}
