using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Zenject;

public class Saver_TEST1 : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_InputField projectName;
    private Clean_AssemblySystem _assembly;
    private INotificationService _notifications;
    private InputAction _save, _load;
    private bool _busy;
    [Inject] public void Construct(Clean_AssemblySystem assembly, INotificationService notifications)
    { _assembly = assembly; _notifications = notifications; }
    private void Awake()
    {
        _save = new InputAction("Save active assembly", InputActionType.Button);
        _load = new InputAction("Reload active assembly", InputActionType.Button);
        _save.AddBinding("<XRController>{LeftHand}/secondaryButton");
        _load.AddBinding("<XRController>{RightHand}/secondaryButton");
#if UNITY_EDITOR
        _save.AddBinding("<Keyboard>/f5");
        _load.AddBinding("<Keyboard>/f9");
#endif
        _save.performed += OnSave;
        _load.performed += OnLoad;
    }
    private async void Start()
    {
        if (projectName != null) projectName.text = AssemblyProjectSession.ActiveName ?? "";
        if (string.IsNullOrEmpty(AssemblyProjectSession.ActiveId)) return;
        await Restore();
    }
    private void OnEnable() { _save.Enable(); _load.Enable(); }
    private void OnDisable() { _save.Disable(); _load.Disable(); }
    private void OnDestroy() { _save.Dispose(); _load.Dispose(); }
    private void OnSave(InputAction.CallbackContext _) { SaveActive(); }
    private void OnLoad(InputAction.CallbackContext _) { ReloadActive(); }
    public void SaveActive()
    {
        if (_busy) return;
        try { _assembly.Save(); _notifications.Info("Сохранено: " + AssemblyProjectSession.ActiveName, 3); }
        catch (Exception e) { Debug.LogException(e); _notifications.Info("Не удалось сохранить сборку", 5); }
    }
    public void RenameActive()
    {
        if (_busy || projectName == null) return;
        try
        {
            var store = new JsonSaveService();
            if (string.IsNullOrEmpty(AssemblyProjectSession.ActiveId)) _assembly.Save();
            store.RenameProject(AssemblyProjectSession.ActiveId, projectName.text);
            projectName.text = AssemblyProjectSession.ActiveName;
            _notifications.Info("Имя сборки: " + AssemblyProjectSession.ActiveName, 3);
        }
        catch (Exception e) { Debug.LogException(e); _notifications.Info("Не удалось переименовать сборку", 5); }
    }
    public async void ReloadActive() { if (!_busy) await Restore(); }
    private async System.Threading.Tasks.Task Restore()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            var store = new JsonSaveService();
            var data = store.Load();
            if (string.IsNullOrEmpty(AssemblyProjectSession.ActiveId)) { _notifications.Info("Сначала сохраните новую сборку", 3); return; }
            await _assembly.RestoreProjectAsync(data);
            if (this != null) _notifications.Info("Загружено: " + AssemblyProjectSession.ActiveName, 3);
        }
        catch (Exception e) { Debug.LogException(e); if (this != null) _notifications.Info("Не удалось загрузить сборку", 5); }
        finally { _busy = false; }
    }
    public void SaveAndExit() => SaveAndOpenScene("BootMenu");

    public void SaveAndOpenGarage() => SaveAndOpenScene("GarageScene");

    private void SaveAndOpenScene(string sceneName)
    {
        if (_busy) return;
        _busy = true;
        try { _assembly.Save(); SceneManager.LoadScene(sceneName); }
        catch (Exception e)
        {
            _busy = false;
            Debug.LogException(e);
            _notifications.Info("Не удалось сохранить сборку или открыть сцену", 5);
        }
    }
    public void ExitWithoutSaving()
    {
        if (!_busy) SceneManager.LoadScene("BootMenu");
    }
}
