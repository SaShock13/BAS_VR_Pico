using System.Threading.Tasks;

public interface IAppState: IInitializableState
{
    AppState StateId { get; }
    Task EnterAsync(StateTransitionContext context);
    Task ExitAsync();
}
