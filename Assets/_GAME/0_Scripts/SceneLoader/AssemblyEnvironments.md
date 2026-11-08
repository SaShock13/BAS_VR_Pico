# Assembly environments

Scene separation, catalog, Boot picker and runtime loading are configured.

- Core: `Assets/Scenes/ConstructorScene.unity` (name retained for existing returns from flight and garage).
- Environments: `Assets/Scenes/AssemblyEnvironment_Garage.unity` and `Assets/Scenes/AssemblyEnvironment_Outdoor.unity`.
- Catalog: `Assets/_GAME/Configs/AssemblyEnvironmentCatalog.asset`, assigned to Boot's EventBusInstaller and Core's AssemblyEnvironmentBootstrap.
- The common workspace retains the original tables, floor mesh/collider and interactive test cube at their original coordinates. Outdoor reuses the existing terrain, with garage shell removed and the existing directional light enabled without realtime shadows.
- Boot's picker uses the existing XR canvas. Its CanvasGroup blocks interaction with the underlying project menu until cancellation or scene transition.
- Selected location survives returns from flight during this application session; it is not written into project saves. Direct Core entry defaults to the first catalog entry.

## Adding another environment

1. Duplicate an environment in Unity and keep the workspace clear at its existing coordinates. Do not add cameras, EventSystems, SceneContexts or assembly services.
2. Add a catalog entry with a unique nonempty ID, title, description and full scene path. Include that scene in the active Build Profile Scene List.
3. Bake lighting per environment with Core visible as needed. The environment becomes active for its lighting settings; new objects use that scene by default. Environments are only replaced by a Single transition, not while assembling.

## Verification

`Tools/BAS VR/Assembly/Verify Environments And Selection` passed in Edit Mode: catalog/build list, common workspace, XR/DI counts, missing environment scripts, menu bindings, paging, cancellation, confirmation, loading lock/retry and legacy navigation guards. Play Mode was not entered; no project save files changed. A camera preview request failed to render, so visual QA is still pending.

Verification in Unity: compile and check Console; new project → picker → each location; saved project → picker; cancel and retry; rapid repeated confirm; missing Build Profile scene; direct ConstructorScene entry; assembly → flight → return preserves location and project; return to Boot unloads Core and environment. Check that only one XR rig, EventSystem and scene container are loaded. Profile lighting and draw calls on standalone PICO; runtime adds no Update/FixedUpdate.

Known limit: no fade or interaction gate during Core's additive load is added. The common floor and tables remain present. If loading is visible on device, cover it with the existing loading presentation before shipping. The picker itself blocks underlying menu input and repeated confirmation.
