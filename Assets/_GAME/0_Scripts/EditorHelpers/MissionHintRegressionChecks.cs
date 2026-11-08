#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public static class MissionHintRegressionChecks
{
    class Subscription : IDisposable { readonly Action remove; public Subscription(Action action) { remove=action; } public void Dispose() { remove(); } }
    class Bus : IEventBus
    {
        readonly Dictionary<Type,List<Delegate>> handlers=new Dictionary<Type,List<Delegate>>();
        public void Initialize() {}
        public void Dispose() { handlers.Clear(); }
        public IDisposable Subscribe<T>(Action<T> handler, EventPriority priority=EventPriority.Normal) where T:IAppEvent { return Subscribe(handler,null,priority); }
        public IDisposable Subscribe<T>(Action<T> handler, Func<T,bool> filter, EventPriority priority=EventPriority.Normal) where T:IAppEvent
        {
            if(!handlers.TryGetValue(typeof(T),out var list)) handlers[typeof(T)]=list=new List<Delegate>();
            Action<T> callback=e=> { if(filter==null || filter(e)) handler(e); };
            list.Add(callback); return new Subscription(()=>list.Remove(callback));
        }
        public void Publish<T>(T e) where T:IAppEvent { if(handlers.TryGetValue(typeof(T),out var list)) foreach(var h in list.ToArray()) ((Action<T>)h)(e); }
        public Task PublishAsync<T>(T e,CancellationToken cancellationToken=default) where T:IAppEvent { Publish(e); return Task.CompletedTask; }
        public int GetSubscriberCount<T>() where T:IAppEvent { return handlers.TryGetValue(typeof(T),out var list)?list.Count:0; }
    }
    class Hints : IHintScenarioController { public void StartScenario(HintContext c) {} public void StopScenario() {} }
    class Activity : IUserActivityService { public float IdleTime {get;set;}=100; public void NotifyActivity() {} }
    static void Set(object target,string field,object value) { target.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value); }
    static void Late(object target) { target.GetType().GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,null); }
    [MenuItem("Tools/BAS VR/Tests/Mission and Hint Regression Checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run these checks outside Play Mode.");
        var originalScene=SceneManager.GetActiveScene();
        var testScene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try { SceneManager.SetActiveScene(testScene); RunChecks(); RunCompletionChecks(); RunToastContinuationChecks(); RunControllerButtonChecks(); RunControllerActionResolutionChecks(); }
        finally { SceneManager.SetActiveScene(originalScene); EditorSceneManager.CloseScene(testScene, true); }
    }
    private static void RunChecks()
    {
        var bus=new Bus();
        var registry=new PartViewRegistry(bus);
        var assembly=new Clean_AssemblySystem(bus,null,null,null,registry,null,null,null,null,null);
        var parts=(Dictionary<string,PartDomainState>)typeof(Clean_AssemblySystem).GetField("_parts",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(assembly);
        var step=new CreatePartStep(PartType.Body,"frame",assembly,bus,new Hints());
        step.Enter(); Assert.IsFalse(step.IsCompleted);
        parts["motor"]=new PartDomainState("motor","motor",PartType.Motor,new PartVisualProperties());
        bus.Publish(new Clean_PartCreatedEvent {InstanceId="motor"}); step.Tick(); Assert.IsFalse(step.IsCompleted);
        parts["frame"]=new PartDomainState("frame","frame",PartType.Body,new PartVisualProperties());
        bus.Publish(new Clean_PartCreatedEvent {InstanceId="frame"}); step.Tick(); Assert.IsTrue(step.IsCompleted);
        step.Exit(); Assert.AreEqual(0,bus.GetSubscriberCount<Clean_PartCreatedEvent>());
        Assert.AreEqual(0,bus.GetSubscriberCount<AssemblyChangedEvent>());
        var existing=new CreatePartStep(PartType.Motor,"motor",assembly,bus,new Hints());
        existing.Enter(); Assert.IsTrue(existing.IsCompleted); existing.Exit();
        parts.Clear();
        var loaded=new CreatePartStep(PartType.Body,"frame",assembly,bus,new Hints());
        loaded.Enter(); parts["frame"]=new PartDomainState("frame","frame",PartType.Body,new PartVisualProperties());
        bus.Publish(new DronesRebuiltEvent()); loaded.Tick(); Assert.IsTrue(loaded.IsCompleted); loaded.Exit();
        parts.Clear();
        var deleted=new CreatePartStep(PartType.Body,"frame",assembly,bus,new Hints()); deleted.Enter();
        parts["frame"]=new PartDomainState("frame","frame",PartType.Body,new PartVisualProperties());
        bus.Publish(new Clean_PartCreatedEvent {InstanceId="frame"}); parts.Clear(); deleted.Tick();
        Assert.IsFalse(deleted.IsCompleted); deleted.Exit();
        Debug.Log("PASS create step: absent, wrong type, matching type, existing part, restored state, create/delete before tick, unsubscribe.");

        var definition=ScriptableObject.CreateInstance<HintScenarioDefinition>();
        definition.ScreenHintDelay=definition.WorldHintDelay=definition.HighlightDelay=definition.ArrowDelay=0;
        var service=new HintService(); int shows=0; service.HintShown+=_=>shows++;
        var controller=new HintScenarioController(service,definition,new Activity(),bus);
        Set(controller,"_viewRegistry",registry); Set(controller,"_AssemblySystem",assembly);
        controller.Initialize();
        var objects=new List<GameObject>();
        try
        {
            controller.StartScenario(new HintContext("install",null,PartType.Motor,PartType.Motor));
            controller.Tick(); Assert.AreEqual(HintVisualType.ScreenText,service.CurrentHint.Value.VisualType);
            int count=shows; for(int i=0;i<1000;i++) controller.Tick(); Assert.AreEqual(count,shows);
            var partGo=new GameObject("HintRegressionPart"); objects.Add(partGo); partGo.SetActive(false);
            var view=partGo.AddComponent<DronePartView>();
            Set(view,"_socketsByType",new Dictionary<PartType,SocketView>());
            registry.Register("motor",partGo);
            parts["motor"]=new PartDomainState("motor","motor",PartType.Motor,new PartVisualProperties());
            bus.Publish(new Clean_PartCreatedEvent {InstanceId="motor",GameObject=partGo}); controller.Tick();
            Assert.AreEqual(HintVisualType.Highlight,service.CurrentHint.Value.VisualType);
            var socketGo=new GameObject("HintRegressionSocket"); objects.Add(socketGo); socketGo.SetActive(false);
            var socket=socketGo.AddComponent<SocketView>();
            var sockets=new Dictionary<PartType,SocketView>(); sockets[PartType.Motor]=socket;
            Set(view,"_socketsByType",sockets);
            bus.Publish(new AssemblyChangedEvent()); controller.Tick();
            Assert.AreEqual(HintVisualType.Arrow,service.CurrentHint.Value.VisualType);
            sockets.Clear(); UnityEngine.Object.DestroyImmediate(socketGo); controller.Tick();
            Assert.AreEqual(HintVisualType.Highlight,service.CurrentHint.Value.VisualType);
            parts.Clear(); registry.Clear(); UnityEngine.Object.DestroyImmediate(partGo); controller.Tick();
            Assert.AreEqual(HintVisualType.ScreenText,service.CurrentHint.Value.VisualType);
            var directGo=new GameObject("HintRegressionDirect"); objects.Add(directGo);
            controller.StartScenario(new HintContext("direct",directGo.transform)); controller.Tick();
            Assert.AreEqual(HintVisualType.WorldText,service.CurrentHint.Value.VisualType);
            controller.StartScenario(new HintContext("button",directGo.transform,pointAtTarget:true)); controller.Tick();
            Assert.AreEqual(HintVisualType.Arrow,service.CurrentHint.Value.VisualType);
            UnityEngine.Object.DestroyImmediate(directGo); controller.Tick();
            Assert.AreEqual(HintVisualType.ScreenText,service.CurrentHint.Value.VisualType);
            Debug.Log("PASS hint controller: text without targets, no repeated show while idle, late part/socket, deleted socket/part/direct target.");

            var arrowGo=new GameObject("HintRegressionArrow"); objects.Add(arrowGo);
            var arrow=arrowGo.AddComponent<ArrowHintView>();
            arrow.Show(new HintInfo("",HintType.Guidance,HintVisualType.Arrow)); Assert.IsFalse(arrowGo.activeSelf);
            var targetGo=new GameObject("HintRegressionTarget"); objects.Add(targetGo);
            arrow.Show(new HintInfo("",HintType.Guidance,HintVisualType.Arrow,null,targetGo.transform)); Assert.IsTrue(arrowGo.activeSelf);
            UnityEngine.Object.DestroyImmediate(targetGo); Late(arrow); Assert.IsFalse(arrowGo.activeSelf);
            var worldGo=new GameObject("HintRegressionWorld"); objects.Add(worldGo);
            var world=worldGo.AddComponent<WorldHintView>();
            world.Show(new HintInfo("",HintType.Guidance,HintVisualType.WorldText)); Assert.IsFalse(worldGo.activeSelf);
            Debug.Log("PASS views: null arrow/world target and destroyed arrow target.");
        }
        finally
        {
            controller.Dispose();
            Assert.AreEqual(0,bus.GetSubscriberCount<Clean_PartCreatedEvent>());
            Assert.AreEqual(0,bus.GetSubscriberCount<Clean_PartDeletedEvent>());
            Assert.AreEqual(0,bus.GetSubscriberCount<AssemblyChangedEvent>());
            Assert.AreEqual(0,bus.GetSubscriberCount<DronesRebuiltEvent>());
            foreach(var go in objects) if(go!=null) UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(definition);
            bus.Dispose();
        }
        Debug.Log("ALL CHECKS PASSED; Play Mode was not entered.");
    }

    private sealed class CompletionTestStep : MissionStep
    {
        public override string Description => "Test objective";
        public void Finish() => Complete();
    }

    private static void RunCompletionChecks()
    {
        var started = MissionEvents.MissionStarted;
        var completed = MissionEvents.MissionCompleted;
        var failed = MissionEvents.MissionFailed;
        var objective = MissionEvents.ObjectiveChanged;
        var target = MissionEvents.TargetChanged;
        MissionEvents.MissionStarted = null;
        MissionEvents.MissionCompleted = null;
        MissionEvents.MissionFailed = null;
        MissionEvents.ObjectiveChanged = null;
        MissionEvents.TargetChanged = null;

        var bus = new Bus();
        var notifications = new NotificationService();
        var feedback = new MissionFeedbackService(bus, notifications);
        var definition = ScriptableObject.CreateInstance<MissionDefinition>();
        var firstData = ScriptableObject.CreateInstance<WaitStepData>();
        var secondData = ScriptableObject.CreateInstance<WaitStepData>();
        try
        {
            definition.CompletionMessage = "Mission success";
            firstData.CompletionMessage = "First step success";
            secondData.CompletionMessage = "Second step success";
            definition.Steps.Add(firstData);
            definition.Steps.Add(secondData);
            var messages = new List<ToastNotification>();
            notifications.ToastRequested += messages.Add;
            int audioRequests = 0;
            bus.Subscribe<PlaySFXRequest>(_ => audioRequests++);
            feedback.Initialize();
            feedback.Initialize();
            Assert.AreEqual(1, bus.GetSubscriberCount<MissionCompletionFeedbackEvent>());

            var first = new CompletionTestStep();
            var second = new CompletionTestStep();
            var runtime = new MissionRuntime(new List<MissionStep> { first, second },
                new List<MissionCondition>(), definition, bus);
            runtime.Start(new MissionContext());
            runtime.Tick();
            Assert.AreEqual(0, messages.Count);
            first.Finish();
            runtime.Tick();
            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual("First step success", messages[0].Text);
            Assert.AreEqual(SFXId.MissionStepComplete, messages[0].Sound);
            second.Finish();
            runtime.Tick();
            Assert.AreEqual(MissionState.Completed, runtime.State);
            Assert.AreEqual(3, messages.Count);
            Assert.AreEqual("Second step success", messages[1].Text);
            Assert.AreEqual("Mission success", messages[2].Text);
            Assert.AreEqual(SFXId.MissionCompleteSound, messages[2].Sound);
            Assert.AreEqual(0, audioRequests, "Audio must wait until the queued toast is displayed.");
            runtime.Tick();
            Assert.AreEqual(3, messages.Count, "Completion must be emitted once.");

            messages.Clear();
            firstData.PlayCompletionSound = false;
            definition.PlayCompletionSound = false;
            var silentStep = new CompletionTestStep();
            var silent = new MissionRuntime(new List<MissionStep> { silentStep },
                new List<MissionCondition>(), definition, bus);
            silent.Start(new MissionContext());
            silentStep.Finish();
            silent.Tick();
            Assert.AreEqual(2, messages.Count);
            Assert.IsNull(messages[0].Sound);
            Assert.IsNull(messages[1].Sound);
            messages.Clear();
            var stopped = new MissionRuntime(new List<MissionStep> { new CompletionTestStep() },
                new List<MissionCondition>(), definition, bus);
            stopped.Start(new MissionContext());
            stopped.Stop();
            stopped.Tick();
            Assert.AreEqual(0, messages.Count);

            definition.Briefing = "Read this briefing";
            definition.BriefingNotificationDuration = 6.5f;
            definition.CompletionNotificationDuration = 8f;
            firstData.ObjectiveNotificationDuration = 5.25f;
            firstData.CompletionNotificationDuration = 2f;
            var timedStep = new CompletionTestStep();
            var timed = new MissionRuntime(new List<MissionStep> { timedStep },
                new List<MissionCondition>(), definition, bus);
            timed.Start(new MissionContext());
            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual(definition.Briefing, messages[0].Text);
            Assert.AreEqual(6.5f, messages[0].Duration);
            Assert.IsNull(messages[0].Sound);
            Assert.AreEqual(5.25f, timed.GetObjectiveInfo().NotificationDuration);
            timedStep.Finish();
            timed.Tick();
            Assert.AreEqual(3, messages.Count);
            Assert.AreEqual(2f, messages[1].Duration);
            Assert.AreEqual(8f, messages[2].Duration);

            messages.Clear();
            definition.BriefingNotificationDuration = 0f;
            definition.CompletionNotificationDuration = 0f;
            firstData.ObjectiveNotificationDuration = 0f;
            firstData.CompletionNotificationDuration = 0f;
            var defaultStep = new CompletionTestStep();
            var defaults = new MissionRuntime(new List<MissionStep> { defaultStep },
                new List<MissionCondition>(), definition, bus);
            defaults.Start(new MissionContext());
            Assert.AreEqual(3f, messages[0].Duration);
            Assert.AreEqual(3f, defaults.GetObjectiveInfo().NotificationDuration);
            defaultStep.Finish();
            defaults.Tick();
            Assert.AreEqual(3f, messages[1].Duration);
            Assert.AreEqual(4f, messages[2].Duration);
            messages.Clear();
            bus.Publish(new MissionBriefingEvent(" "));
            Assert.AreEqual(0, messages.Count, "An empty briefing must not produce a blank toast.");
            RunFlightHandoffChecks(bus, definition, messages);
            RunControllerStepBindingChecks(bus, definition);
            feedback.Dispose();
            Assert.AreEqual(0, bus.GetSubscriberCount<MissionCompletionFeedbackEvent>());
            Assert.AreEqual(0, bus.GetSubscriberCount<MissionBriefingEvent>());
            Debug.Log("PASS completion feedback: custom texts, step/mission sounds, order, exactly once, sound toggles, stop, disposal.");
            Debug.Log("PASS briefing and durations: custom values, 3/3/3/4 second defaults, empty briefing, disposal.");
        }
        finally
        {
            feedback.Dispose();
            bus.Dispose();
            UnityEngine.Object.DestroyImmediate(firstData);
            UnityEngine.Object.DestroyImmediate(secondData);
            UnityEngine.Object.DestroyImmediate(definition);
            MissionEvents.MissionStarted = started;
            MissionEvents.MissionCompleted = completed;
            MissionEvents.MissionFailed = failed;
            MissionEvents.ObjectiveChanged = objective;
            MissionEvents.TargetChanged = target;
        }
    }

    private static void RunFlightHandoffChecks(Bus bus, MissionDefinition definition,
        List<ToastNotification> messages)
    {
        int launches = 0, cancellations = 0;
        var ordinary = new FlightLaunchApprovedEvent(() => launches++, () => cancellations++);
        bus.Publish(ordinary);
        Assert.IsFalse(ordinary.IsDeferred, "A normal flight must not wait for tutorial feedback.");

        var data = ScriptableObject.CreateInstance<LaunchFlightStepData>();
        try
        {
            data.CompletionMessage = "";
            data.PlayCompletionSound = false;
            definition.Steps.Clear();
            definition.Steps.Add(data);
            definition.Briefing = "";
            definition.CompletionMessage = "Congratulations, entering flight";
            definition.CompletionNotificationDuration = 4f;
            definition.PlayCompletionSound = true;
            messages.Clear();
            var step = new LaunchFlightStep(bus, new Hints(), null, "Press flight");
            var runtime = new MissionRuntime(new List<MissionStep> { step },
                new List<MissionCondition>(), definition, bus);
            runtime.Start(new MissionContext());
            Assert.IsFalse(step.IsCompleted, "Entering the step must not launch or complete it.");
            var approved = new FlightLaunchApprovedEvent(() => launches++, () => cancellations++);
            bus.Publish(approved);
            Assert.IsTrue(approved.IsDeferred);
            Assert.IsTrue(step.IsCompleted);
            Assert.AreEqual(0, launches);
            runtime.Tick();
            Assert.AreEqual(MissionState.Completed, runtime.State);
            Assert.AreEqual(1, messages.Count, "Only the final mission toast should precede flight.");
            Assert.AreEqual(4f, messages[0].Duration);
            Assert.AreEqual(SFXId.MissionCompleteSound, messages[0].Sound);
            Assert.AreEqual(0, launches, "Queuing a notification must not load the flight scene.");
            messages[0].OnDismissed.Invoke();
            messages[0].OnDismissed.Invoke();
            Assert.AreEqual(1, launches, "Finishing the toast must continue the launch exactly once.");
            runtime.Stop();
            Assert.AreEqual(0, cancellations);
            Assert.AreEqual(0, bus.GetSubscriberCount<FlightLaunchApprovedEvent>());

            messages.Clear();
            var cancelledStep = new LaunchFlightStep(bus, new Hints(), null, "Press flight");
            var cancelledRuntime = new MissionRuntime(new List<MissionStep> { cancelledStep },
                new List<MissionCondition>(), definition, bus);
            cancelledRuntime.Start(new MissionContext());
            var cancelled = new FlightLaunchApprovedEvent(() => launches++, () => cancellations++);
            bus.Publish(cancelled);
            cancelledRuntime.Tick();
            messages[0].OnCancelled.Invoke();
            messages[0].OnDismissed.Invoke();
            Assert.AreEqual(1, launches, "A cancelled toast must never trigger a delayed scene load.");
            Assert.AreEqual(1, cancellations);

            messages.Clear();
            var stoppedStep = new LaunchFlightStep(bus, new Hints(), null, "Press flight");
            var stopped = new MissionRuntime(new List<MissionStep> { stoppedStep },
                new List<MissionCondition>(), definition, bus);
            stopped.Start(new MissionContext());
            var interrupted = new FlightLaunchApprovedEvent(() => launches++, () => cancellations++);
            bus.Publish(interrupted);
            stopped.Stop();
            interrupted.Continue();
            Assert.AreEqual(1, launches);
            Assert.AreEqual(2, cancellations);
            Assert.AreEqual(0, bus.GetSubscriberCount<FlightLaunchApprovedEvent>());
            Debug.Log("PASS tutorial flight: no normal delay, approved launch only, final toast duration/sound, exactly once, cancellation and mission stop.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(data);
        }
    }

    private static void RunToastContinuationChecks()
    {
        // Temporary objects live only in the isolated editor test scene.
        var cameraObject = new GameObject("ToastTestCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        var viewObject = new GameObject("ToastTestView", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        viewObject.SetActive(false);
        var presenterObject = new GameObject("ToastTestPresenter");
        presenterObject.SetActive(false);
        var bus = new Bus();
        try
        {
            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(viewObject.transform, false);
            var text = textObject.AddComponent<TMPro.TextMeshProUGUI>();
            var view = viewObject.AddComponent<ToastNotificationView>();
            Set(view, "_text", text);
            Set(view, "_canvasGroup", viewObject.GetComponent<CanvasGroup>());
            Set(view, "_viewer", camera);
            var presenter = presenterObject.AddComponent<ToastNotificationPresenter>();
            Set(presenter, "_view", view);
            Set(presenter, "_events", bus);
            Set(presenter, "_fadeDuration", 0f);
            int continued = 0, cancelled = 0, sounds = 0;
            bus.Subscribe<PlaySFXRequest>(_ => sounds++);
            var message = new ToastNotification("Entering flight", NotificationType.Info, 4f,
                SFXId.MissionCompleteSound, () => continued++, () => cancelled++);
            var show = typeof(ToastNotificationPresenter).GetMethod("ShowMessage", BindingFlags.NonPublic | BindingFlags.Instance);
            var routine = (System.Collections.IEnumerator)show.Invoke(presenter, new object[] { message });
            Assert.IsTrue(routine.MoveNext());
            Assert.AreEqual(1, sounds);
            Assert.AreEqual(0, continued);
            while (((System.Collections.IEnumerator)routine.Current).MoveNext()) { }
            Assert.IsTrue(routine.MoveNext());
            Assert.IsInstanceOf<WaitForSecondsRealtime>(routine.Current);
            Assert.AreEqual(4f, ((WaitForSecondsRealtime)routine.Current).waitTime);
            Assert.AreEqual(0, continued, "The scene must not load before the display wait.");
            Assert.IsTrue(routine.MoveNext());
            while (((System.Collections.IEnumerator)routine.Current).MoveNext()) { }
            Assert.AreEqual(0, continued);
            Assert.IsFalse(routine.MoveNext());
            Assert.AreEqual(1, continued);
            Assert.AreEqual(0, cancelled);

            var interrupted = (System.Collections.IEnumerator)show.Invoke(presenter, new object[] { message });
            Assert.IsTrue(interrupted.MoveNext());
            var queue = (Queue<ToastNotification>)typeof(ToastNotificationPresenter)
                .GetField("_queue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(presenter);
            queue.Enqueue(message);
            typeof(ToastNotificationPresenter).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(presenter, null);
            Assert.AreEqual(2, cancelled, "Both active and queued continuations must cancel on disable.");
            Assert.AreEqual(1, continued);
            Assert.AreEqual(0, queue.Count);
            Debug.Log("PASS toast continuation: sound on show, realtime duration before continuation, cancel active and queued on disable.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(presenterObject);
            if (viewObject != null) UnityEngine.Object.DestroyImmediate(viewObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            bus.Dispose();
        }
    }

    private static void RunControllerButtonChecks()
    {
        var bus = new Bus();
        var service = new ControllerButtonHintService(bus);
        var root = new GameObject("ControllerHintTestLoader");
        root.SetActive(false);
        var visualObject = new GameObject("ControllerHintTestVisual");
        var tracking = new UnityEngine.InputSystem.InputAction("Controller hint test tracking");
        using var menuHintAction = new InputAction("Menu", InputActionType.Button,
            "<XRController>{LeftHand}/{MenuButton}");
        try
        {
            service.Initialize(); service.Initialize();
            Assert.AreEqual(1, bus.GetSubscriberCount<ControllerButtonHintRequest>());
            bus.Publish(new PartsMenuVisibilityChangedEvent(true, menuHintAction));
            bus.Publish(new ControllerButtonHintRequest(menuHintAction));
            Assert.AreEqual(ControllerButtonHintId.None, service.CurrentButton);
            bus.Publish(new PartsMenuVisibilityChangedEvent(false, menuHintAction));
            Assert.AreEqual(ControllerButtonHintId.PartsMenu, service.CurrentButton);
            bus.Publish(new PartsMenuVisibilityChangedEvent(true, menuHintAction));
            Assert.AreEqual(ControllerButtonHintId.None, service.CurrentButton);
            bus.Publish(new ControllerButtonHintRequest(null));
            bus.Publish(new PartsMenuVisibilityChangedEvent(false, menuHintAction));
            Assert.AreEqual(ControllerButtonHintId.None, service.CurrentButton);

            var registry = new PartViewRegistry(bus);
            var assembly = new Clean_AssemblySystem(bus, null, null, null, registry, null, null, null, null, null);
            var parts = (Dictionary<string, PartDomainState>)typeof(Clean_AssemblySystem)
                .GetField("_parts", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(assembly);
            var step = new CreatePartStep(PartType.Body, "Create frame", assembly, bus, new Hints());
            step.Enter();
            parts["frame"] = new PartDomainState("frame", "frame", PartType.Body, new PartVisualProperties());
            bus.Publish(new Clean_PartCreatedEvent { InstanceId = "frame" });
            step.Tick(); step.Exit();
            Assert.IsTrue(step.IsCompleted);
            Assert.AreEqual(ControllerButtonHintId.None, service.CurrentButton);
            var existing = new CreatePartStep(PartType.Body, "Existing frame", assembly, bus, new Hints());
            existing.Enter();
            Assert.IsTrue(existing.IsCompleted);
            Assert.AreEqual(ControllerButtonHintId.None, service.CurrentButton);
            existing.Exit();

            var renderer = visualObject.AddComponent<MeshRenderer>();
            visualObject.transform.localScale = Vector3.one * 0.8f;
            var pressVisual = visualObject.AddComponent<ControllerButtonPressVisual>();
            pressVisual.enabled = false;
            var view = root.AddComponent<ControllerButtonHintView>();
            view.enabled = false;
            Set(view, "visual", pressVisual);
            Set(view, "buttonBoneName", "TestMenuBone");
            Set(view, "hintDelay", 0f);
            view.Construct(service);
            Assert.AreEqual(Vector3.one * 0.8f, visualObject.transform.localScale,
                "Injection before Awake must preserve the authored visual scale.");
            root.SetActive(true);
            var bone = new GameObject("TestMenuBone");
            bone.transform.SetParent(root.transform, false);
            bone.transform.localPosition = new Vector3(0.1f, 0.2f, 0.3f);
            var bind = typeof(ControllerButtonHintView).GetMethod("BindVisual", BindingFlags.NonPublic | BindingFlags.Instance);
            bind.Invoke(view, null);
            Set(pressVisual, "_startedAt", 0f);
            pressVisual.ApplyAnimation(0.128f);
            Assert.IsTrue(renderer.enabled);
            Assert.Greater(renderer.transform.localScale.x, 0f);
            var rest = new Vector3(0.1f, 0.2f, 0.3f);
            Assert.Less(Vector3.Distance(bone.transform.localPosition, rest - Vector3.up * 0.0008f), 0.000001f);
            Assert.Less(Vector3.Distance(renderer.transform.position, bone.transform.position), 0.000001f);
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            var white = block.GetColor(Shader.PropertyToID("_BaseColor"));
            Assert.AreEqual(1f, white.r); Assert.AreEqual(1f, white.g); Assert.AreEqual(1f, white.b);
            Assert.Greater(white.a, 0.85f);
            for (int i = 0; i < 1000; i++) pressVisual.ApplyAnimation(0.128f);
            Assert.Less(Vector3.Distance(bone.transform.localPosition, rest - Vector3.up * 0.0008f), 0.000001f,
                "Visual presses must not accumulate displacement.");
            var sdkPose = rest + Vector3.right * 0.01f;
            bone.transform.localPosition = sdkPose;
            pressVisual.ApplyAnimation(0.128f);
            pressVisual.Unbind();
            Assert.Less(Vector3.Distance(bone.transform.localPosition, sdkPose), 0.000001f,
                "Unbinding must restore the most recent SDK pose.");
            bind.Invoke(view, null);
            Set(pressVisual, "_startedAt", 0f);
            pressVisual.ApplyAnimation(0.128f);
            pressVisual.SetPhysicalPressed(true);
            pressVisual.ApplyAnimation(0.128f);
            Assert.IsFalse(renderer.enabled);
            Assert.Less(Vector3.Distance(bone.transform.localPosition, sdkPose), 0.000001f,
                "The real press must take priority over the visual animation.");
            pressVisual.SetPhysicalPressed(false);
            Set(pressVisual, "_startedAt", 0f);
            pressVisual.ApplyAnimation(0.128f);
            Assert.IsTrue(renderer.enabled);
            pressVisual.Unbind();
            var restRotation = Quaternion.Euler(12f, 25f, 8f);
            bone.transform.localRotation = restRotation;
            Set(pressVisual, "rotatePress", true);
            Set(pressVisual, "pressEulerAngles", new Vector3(-15f, 0f, 0f));
            bind.Invoke(view, null);
            Set(pressVisual, "_startedAt", 0f);
            for (int i = 0; i < 1000; i++) pressVisual.ApplyAnimation(0.128f);
            var expectedRotation = Quaternion.Euler(restRotation.eulerAngles + new Vector3(-15f, 0f, 0f));
            Assert.Less(Quaternion.Angle(expectedRotation, bone.transform.localRotation), 0.001f,
                "Trigger/grip rotations must not accumulate.");
            pressVisual.Unbind();
            Assert.Less(Quaternion.Angle(restRotation, bone.transform.localRotation), 0.001f);
            bind.Invoke(view, null);
            Set(pressVisual, "_startedAt", 0f);
            pressVisual.ApplyAnimation(0.128f);
            var newerSdkRotation = Quaternion.Euler(20f, 40f, 15f);
            bone.transform.localRotation = newerSdkRotation;
            pressVisual.ApplyAnimation(0.128f);
            pressVisual.Unbind();
            Assert.Less(Quaternion.Angle(newerSdkRotation, bone.transform.localRotation), 0.001f,
                "A newer SDK rotation must be restored after the visual hint.");
            Set(pressVisual, "rotatePress", false);
            UnityEngine.Object.DestroyImmediate(bone);
            bind.Invoke(view, null);
            Assert.IsFalse(renderer.enabled, "No detached visual may remain if the button is destroyed.");
            Assert.IsFalse(pressVisual.enabled);
            var replacement = new GameObject("TestMenuBone");
            replacement.transform.SetParent(root.transform, false);
            replacement.transform.localPosition = new Vector3(0.4f, 0.5f, 0.6f);
            bind.Invoke(view, null);
            Set(pressVisual, "_startedAt", 0f);
            pressVisual.ApplyAnimation(0.128f);
            Assert.IsTrue(renderer.enabled);
            Assert.Less(Vector3.Distance(renderer.transform.position, replacement.transform.position), 0.00001f);
            Set(view, "_tracking", tracking);
            Set(view, "_subscribed", true);
            typeof(ControllerButtonHintView).GetMethod("Refresh", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, null);
            Assert.IsFalse(renderer.enabled, "An untracked/disabled input action must hide the controller hint.");
            Assert.AreEqual(new Vector3(0.4f, 0.5f, 0.6f), replacement.transform.localPosition);
            Assert.IsFalse(tracking.enabled, "Hints must not enable or disable shared tracking actions.");
            typeof(ControllerButtonHintView).GetMethod("Unsubscribe", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, null);
            using (var observer = new UnityEngine.InputSystem.InputAction("Dedicated hint observer",
                UnityEngine.InputSystem.InputActionType.Value))
            {
                Set(view, "_buttonAction", observer);
                Set(view, "manageButtonAction", true);
                var acquire = typeof(ControllerButtonHintView).GetMethod("AcquireButtonAction", BindingFlags.NonPublic | BindingFlags.Instance);
                var release = typeof(ControllerButtonHintView).GetMethod("ReleaseButtonAction", BindingFlags.NonPublic | BindingFlags.Instance);
                acquire.Invoke(view, null);
                Assert.IsTrue(observer.enabled);
                release.Invoke(view, null);
                Assert.IsFalse(observer.enabled, "A dedicated observer must stop after the hint.");
                observer.Enable();
                Set(view, "manageButtonAction", false);
                acquire.Invoke(view, null);
                release.Invoke(view, null);
                Assert.IsTrue(observer.enabled, "Shared gameplay actions must stay enabled.");
                observer.Disable();
                Set(view, "_buttonAction", null);
            }
            service.Dispose();
            Assert.AreEqual(0, bus.GetSubscriberCount<ControllerButtonHintRequest>());
            Assert.AreEqual(0, bus.GetSubscriberCount<PartsMenuVisibilityChangedEvent>());
            Debug.Log("PASS controller hints: menu state, white button surface, visual press, no pose drift, SDK pose restoration, actual input priority, model replacement, tracking guard, disposal.");
        }
        finally
        {
            service.Dispose();
            tracking.Dispose();
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(visualObject);
            bus.Dispose();
        }
    }

    private sealed class ControllerHintTestFailure : MissionCondition
    {
        public override bool IsTriggered() => true;
        public override string FailReason => "Test failure";
    }

    private static void RunControllerStepBindingChecks(Bus bus, MissionDefinition definition)
    {
        var service = new ControllerButtonHintService(bus);
        var firstData = ScriptableObject.CreateInstance<WaitStepData>();
        var secondData = ScriptableObject.CreateInstance<WaitStepData>();
        var inputAsset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = inputAsset.AddActionMap("Hint tests");
        var menu = map.AddAction("Menu", InputActionType.Button, "<XRController>{LeftHand}/{MenuButton}");
        var primary = map.AddAction("Primary", InputActionType.Button, "<XRController>{LeftHand}/{PrimaryButton}");
        var menuReference = InputActionReference.Create(menu);
        var primaryReference = InputActionReference.Create(primary);
        try
        {
            service.Initialize();
            firstData.ControllerButtonAction = menuReference;
            secondData.ControllerButtonAction = primaryReference;
            definition.Steps.Clear();
            definition.Steps.Add(firstData);
            definition.Steps.Add(secondData);
            var first = new CompletionTestStep();
            var second = new CompletionTestStep();
            var runtime = new MissionRuntime(new List<MissionStep> { first, second },
                new List<MissionCondition>(), definition, bus);
            runtime.Start(new MissionContext());
            Assert.AreEqual(ControllerButtonHintId.PartsMenu, service.CurrentButton);
            runtime.Pause();
            Assert.AreEqual(ControllerButtonHintId.None, service.CurrentButton);
            runtime.Resume();
            Assert.AreEqual(ControllerButtonHintId.PartsMenu, service.CurrentButton);
            first.Finish(); runtime.Tick();
            Assert.AreEqual(ControllerButtonHintId.LeftPrimary, service.CurrentButton);
            second.Finish(); runtime.Tick();
            Assert.AreEqual(ControllerButtonHintId.None, service.CurrentButton);
            var existing = new CompletionTestStep(); existing.Finish();
            var satisfied = new MissionRuntime(new List<MissionStep> { existing },
                new List<MissionCondition>(), definition, bus);
            satisfied.Start(new MissionContext());
            Assert.AreEqual(ControllerButtonHintId.None, service.CurrentButton);
            satisfied.Stop();
            var stopped = new MissionRuntime(new List<MissionStep> { new CompletionTestStep() },
                new List<MissionCondition>(), definition, bus);
            stopped.Start(new MissionContext()); stopped.Stop();
            Assert.AreEqual(ControllerButtonHintId.None, service.CurrentButton);
            var failed = new MissionRuntime(new List<MissionStep> { new CompletionTestStep() },
                new List<MissionCondition> { new ControllerHintTestFailure() }, definition, bus);
            failed.Start(new MissionContext()); failed.Tick();
            Assert.AreEqual(MissionState.Failed, failed.State);
            Assert.AreEqual(ControllerButtonHintId.None, service.CurrentButton);
            Debug.Log("PASS controller-step bindings: arbitrary step definitions, pause/resume, next step, completion, already satisfied, stop and failure.");
        }
        finally
        {
            service.Dispose();
            definition.Steps.Clear();
            UnityEngine.Object.DestroyImmediate(firstData);
            UnityEngine.Object.DestroyImmediate(secondData);
            UnityEngine.Object.DestroyImmediate(menuReference);
            UnityEngine.Object.DestroyImmediate(primaryReference);
            UnityEngine.Object.DestroyImmediate(inputAsset);
        }
    }

    private static void RunControllerActionResolutionChecks()
    {
        foreach (var binding in ControllerButtonHintBindingsBuilder.Bindings)
        {
            string hand = binding.Left ? "LeftHand" : "RightHand";
            using var action = new InputAction("Physical button", InputActionType.Value,
                "<XRController>{" + hand + "}/{" + binding.Control + "}");
            Assert.AreEqual(ControllerButtonActionResolver.Mask(binding.Id), ControllerButtonActionResolver.Resolve(action));
        }
        var bus = new Bus();
        var service = new ControllerButtonHintService(bus);
        using var multi = new InputAction("Multi binding");
        using var remappedMenu = new InputAction("Remapped menu", InputActionType.Button,
            "<XRController>{RightHand}/{PrimaryButton}");
        try
        {
            service.Initialize();
            multi.AddBinding("<Keyboard>/space");
            multi.AddBinding("<Mouse>/leftButton");
            multi.AddBinding(new InputBinding { path = "<XRController>{LeftHand}/{GripButton}", groups = "Left" });
            multi.AddBinding(new InputBinding { path = "<XRController>{RightHand}/{GripButton}", groups = "Right" });
            bus.Publish(new ControllerButtonHintRequest(multi));
            Assert.IsTrue(service.IsHighlighted(ControllerButtonHintId.LeftGrip));
            Assert.IsTrue(service.IsHighlighted(ControllerButtonHintId.RightGrip));
            Assert.IsFalse(multi.enabled, "Resolving a gameplay action must not enable it.");
            multi.bindingMask = InputBinding.MaskByGroup("Right");
            bus.Publish(new ControllerButtonHintRequest(multi));
            Assert.IsFalse(service.IsHighlighted(ControllerButtonHintId.LeftGrip));
            Assert.IsTrue(service.IsHighlighted(ControllerButtonHintId.RightGrip));
            multi.bindingMask = null;
            multi.ApplyBindingOverride(2, "<XRController>{LeftHand}/secondaryButton");
            bus.Publish(new ControllerButtonHintRequest(multi));
            Assert.IsTrue(service.IsHighlighted(ControllerButtonHintId.LeftSecondary));
            Assert.IsFalse(service.IsHighlighted(ControllerButtonHintId.LeftGrip));
            Assert.IsTrue(service.IsHighlighted(ControllerButtonHintId.RightGrip));
            multi.ApplyBindingOverride(2, string.Empty);
            bus.Publish(new ControllerButtonHintRequest(multi));
            Assert.AreEqual(ControllerButtonHintId.RightGrip, service.CurrentButton);

            // Source action identity, not the old physical Menu ID, controls suppression.
            bus.Publish(new ControllerButtonHintRequest(remappedMenu));
            Assert.AreEqual(ControllerButtonHintId.RightPrimary, service.CurrentButton);
            bus.Publish(new PartsMenuVisibilityChangedEvent(true, remappedMenu));
            Assert.AreEqual(ControllerButtonHintId.None, service.CurrentButton);
            bus.Publish(new ControllerButtonHintRequest(multi));
            Assert.AreEqual(ControllerButtonHintId.RightGrip, service.CurrentButton);
            bus.Publish(new PartsMenuVisibilityChangedEvent(false, remappedMenu));

            int changed = 0;
            service.Changed += _ => changed++;
            using var rebound = new InputAction("Live rebind", InputActionType.Button,
                "<XRController>{LeftHand}/{PrimaryButton}");
            bus.Publish(new ControllerButtonHintRequest(rebound));
            _ = rebound.controls.Count;
            rebound.ApplyBindingOverride(0, "<XRController>{RightHand}/{SecondaryButton}");
            _ = rebound.controls.Count;
            Assert.AreEqual(ControllerButtonHintId.RightSecondary, service.CurrentButton,
                "A binding change must update the currently shown controller hint.");
            Assert.IsFalse(rebound.enabled);
            Assert.Greater(changed, 1);
            bus.Publish(new ControllerButtonHintRequest(null));
            Assert.AreEqual(ControllerButtonHintId.None, service.CurrentButton);

            Assert.AreEqual(0u, ControllerButtonActionResolver.ResolvePath("<Keyboard>/x"));
            Assert.AreEqual(0u, ControllerButtonActionResolver.ResolvePath("<Mouse>/leftButton"));
            Assert.AreEqual(ControllerButtonActionResolver.Mask(ControllerButtonHintId.LeftThumbstick),
                ControllerButtonActionResolver.ResolvePath("<XRController>{LeftHand}/primary2DAxis/x"));
            Assert.AreEqual(ControllerButtonActionResolver.Mask(ControllerButtonHintId.LeftTrigger)
                | ControllerButtonActionResolver.Mask(ControllerButtonHintId.RightTrigger),
                ControllerButtonActionResolver.ResolvePath("<XRController>/trigger"));
            Debug.Log("PASS action-based hints: all controller buttons, keyboard/mouse ignored, both hands, binding masks, overrides, live rebinding, menu action identity, empty selection.");
        }
        finally
        {
            service.Dispose();
            bus.Dispose();
        }
    }
}
#endif

