

using System.Threading.Tasks;
using UnityEngine;

public class AssemblyState : IAppState
{
    public AppState StateId { get;} = AppState.Assembly;

    private ISceneLoader _sceneLoader;
    private IEventBus _eventBus;

    public AssemblyState(ISceneLoader sceneLoader, IEventBus eventBus)
    {
        _sceneLoader = sceneLoader;
        _eventBus = eventBus;
    }

    public Task EnterAsync(StateTransitionContext context)
    {

        Debug.Log($"EnterAsync AssemblyState{this}");
        //_sceneLoader.ActivatePreloadedScene();
        //var musicEvent = new PlayMusicRequestedEvent { MusicId = SoundId.AssemblyBackgroundMusic, Loop = true };
        //_eventBus.Publish(musicEvent);
        return Task.CompletedTask;
    }

    public Task ExitAsync()
    {

        Debug.Log($"ExitAsync AssemblyState");
        return Task.CompletedTask;
        
    }

    public void StateInitialize()
    {
        Debug.Log($"Initializing AssemblyState{this}");
    }
}

