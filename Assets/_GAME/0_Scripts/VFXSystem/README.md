# VFXSystem

Structure follows Drone 1.0: VFXLibrary -> VFXPlayer <- VFXService.
AssemblyInstaller binds the library, player and service in the existing scene container.
The library is assigned in ConstructorScene; populate its Entries in the Inspector.
Each entry specifies Id, Prefab and Lifetime in seconds (zero defaults to one second).
Use active burst prefabs with ParticleSystem Play On Awake enabled. No looping effects,
extra lights or cameras. Keep particle counts and transparent screen coverage small.
The original third-party effect prefabs/materials are not copied from Drone 1.0.

Publish from gameplay on the Unity main thread:

```csharp
eventBus.Publish(new PlayVFXRequest
{
    VFXId = VFXId.Checkpoint,
    Position = checkpointPosition,
    Scale = Vector3.one
});
```

IVFXService.PlayVFX is also available through Zenject.
The service handles attachment, detachment, deletion and duplication events automatically.
Deletion events snapshot world position/scale before the view is destroyed.
Missing library entries are ignored. Prefabs instantiate only on events and are destroyed
after Lifetime. Up to eight simultaneous effects by default; excess requests are skipped.
There is no permanent per-frame script work. For frequent bursts, add a bounded pool.
WavyLine from Drone 1.0 is a separate checkpoint visualization and is not part of this player.
