
    using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class LeftHandMenu : MonoBehaviour
{
    //[Header("References")]
    //[SerializeField] private GameObject menuPrefab;
    //[SerializeField] private Transform menuTransform;
    //[SerializeField] private Transform initMenuTransform;

    //[Header("Settings")]
    //[SerializeField, Range(0.1f, 0.5f)] private float appearDuration = 0.3f;
    //[SerializeField] private string leftHandMenuActionName = "Menu";

    //private GameObject menuObj;
    //private InputAction leftHandMenuAction;
    //private bool menuEnabled = false;
    //private Coroutine initCoroutine;
    //private DiContainer _container;

    //private void Awake()
    //{
    //    leftHandMenuAction = InputSystem.actions.FindAction(leftHandMenuActionName);
    //}

    //[Inject]
    //public void Construct(DiContainer container)
    //{
    //    _container = container;
    //}

    //private void Start()
    //{
    //    Debug.Log($"Start() вызван в кадре {Time.frameCount}");

    //    // Запускаем инициализацию меню
    //    initCoroutine = StartCoroutine(InitializeMenuCoroutine());

    //    // НЕ подписываемся на ввод пока меню не готово!
    //    // leftHandMenuAction.performed += OnMenuButtonPressed; // УБРАТЬ ОТСЮДА
    //}

    //private IEnumerator InitializeMenuCoroutine()
    //{
    //    Debug.Log($"1. Создаем меню (кадр {Time.frameCount})");

    //    // Создаем меню
    //    menuObj = _container.InstantiatePrefab(menuPrefab, menuTransform);
    //    menuObj.name = "LeftHandMenu_Instance";

    //    // НЕМЕДЛЕННО устанавливаем scale в 0
    //    menuObj.transform.localScale = Vector3.zero;

    //    // Сохраняем ID для отладки
    //    int objId = menuObj.GetInstanceID();
    //    Debug.Log($"2. Меню создано, ID: {objId}, scale: {menuObj.transform.localScale}");

    //    // Ждем ОДИН КАДР для создания объекта в сцене
    //    yield return null;

    //    Debug.Log($"3. Первый кадр пропущен (кадр {Time.frameCount})");

    //    // Проверяем, не уничтожили ли объект
    //    if (menuObj == null)
    //    {
    //        Debug.LogError("Меню было уничтожено!");
    //        yield break;
    //    }

    //    // Ждем еще кадр для полной инициализации Unity
    //    yield return null;

    //    Debug.Log($"4. Второй кадр пропущен (кадр {Time.frameCount})");

    //    // Теперь подписываемся на ввод
    //    leftHandMenuAction.performed += OnMenuButtonPressed;
    //    Debug.Log("5. Подписались на Input System");

    //    // Меню готово к использованию, но НЕВИДИМО (scale = 0)
    //    Debug.Log($"6. Меню готово! Объект активен: {menuObj.activeSelf}, scale: {menuObj.transform.localScale}");
    //}

    //private void OnMenuButtonPressed(InputAction.CallbackContext context)
    //{
    //    if (menuObj == null) return;

    //    Debug.Log($"Кнопка нажата! Текущий scale: {menuObj.transform.localScale}");

    //    ToggleMenu();
    //}

    //public void ToggleMenu()
    //{
    //    menuEnabled = !menuEnabled;

    //    if (menuEnabled)
    //    {
    //        ShowMenu();
    //    }
    //    else
    //    {
    //        HideMenu();
    //    }
    //}

    //private void ShowMenu()
    //{
    //    Debug.Log("Показываем меню");

    //    if (menuObj == null) return;

    //    // Останавливаем все анимации
    //    StopAllCoroutines();

    //    // Запускаем анимацию появления
    //    StartCoroutine(ShowMenuCoroutine());
    //}

    //private IEnumerator ShowMenuCoroutine()
    //{
    //    Vector3 startScale = Vector3.zero;
    //    Vector3 startPosition = initMenuTransform.position;
    //    Quaternion startRotation = initMenuTransform.rotation;

    //    float elapsed = 0f;

    //    while (elapsed < appearDuration)
    //    {
    //        elapsed += Time.deltaTime;
    //        float t = elapsed / appearDuration;

    //        menuObj.transform.position = Vector3.Lerp(startPosition, menuTransform.position, t);
    //        menuObj.transform.rotation = Quaternion.Slerp(startRotation, menuTransform.rotation, t);
    //        menuObj.transform.localScale = Vector3.Lerp(startScale, Vector3.one, t);

    //        yield return null;
    //    }

    //    menuObj.transform.position = menuTransform.position;
    //    menuObj.transform.rotation = menuTransform.rotation;
    //    menuObj.transform.localScale = Vector3.one;

    //    Debug.Log("Меню полностью показано");
    //}

    //private void HideMenu()
    //{
    //    Debug.Log("Скрываем меню");

    //    if (menuObj == null) return;

    //    StopAllCoroutines();
    //    StartCoroutine(HideMenuCoroutine());
    //}

    //private IEnumerator HideMenuCoroutine()
    //{
    //    Vector3 startScale = menuObj.transform.localScale;
    //    Vector3 targetScale = Vector3.zero;
    //    Vector3 startPosition = menuObj.transform.position;
    //    Vector3 targetPosition = initMenuTransform.position;

    //    float elapsed = 0f;

    //    while (elapsed < appearDuration * 0.5f)
    //    {
    //        elapsed += Time.deltaTime;
    //        float t = elapsed / (appearDuration * 0.5f);

    //        menuObj.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
    //        menuObj.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

    //        yield return null;
    //    }

    //    menuObj.transform.position = targetPosition;
    //    menuObj.transform.localScale = targetScale;

    //    Debug.Log("Меню полностью скрыто");
    //}

    //private void OnDestroy()
    //{
    //    // Отписываемся от событий
    //    if (leftHandMenuAction != null)
    //    {
    //        leftHandMenuAction.performed -= OnMenuButtonPressed;
    //    }

    //    // Останавливаем корутины
    //    if (initCoroutine != null)
    //    {
    //        StopCoroutine(initCoroutine);
    //    }

    //    StopAllCoroutines();
    //}

    [Header("References")]
    [SerializeField] private GameObject menuPrefab;
    [SerializeField] private GameObject sceneMenu;
    [SerializeField] private Transform menuTransform;
    [SerializeField] private Transform initMenuTransform;

    [Header("Settings")]
    [SerializeField, Range(0.1f, 0.5f)] private float appearDuration = 0.3f;
    //[SerializeField] private string leftHandMenuActionName;
    [SerializeField, Range(0.1f, 0.5f)] private float duration = 0.5f;

    private GameObject menuObj;
    [SerializeField]  private InputActionReference leftHandMenuAction;
    private bool menuEnabled = false;
    private DiContainer _container;
    private Vector3 hideScale;
    private CanvasGroup _menuInteraction;

    private IEventBus _eventBus;

    public bool MenuEnabled { get => menuEnabled; set => menuEnabled = value; }

    private void Awake()
    {
        ///leftHandMenuAction = InputSystem.actions.FindAction(leftHandMenuActionName);

        //Debug.Log($"leftHandMenuAction {leftHandMenuAction!= null}");

    }


    private void OnEnable()
    {
        
        if (leftHandMenuAction != null && leftHandMenuAction.action != null)
            leftHandMenuAction.action.performed += MenuBtnPressed;
    }

    private void OnDisable()
    {
        if (leftHandMenuAction != null && leftHandMenuAction.action != null)
            leftHandMenuAction.action.performed -= MenuBtnPressed;
        
    }

    [Inject]
    public void Construct(DiContainer container,IEventBus eventBus)
    {
        _container = container;
        _eventBus = eventBus;
    }


    private IEnumerator Start()
    {
        hideScale = new Vector3(0.02f, 0.02f, 0.02f);
        menuObj = sceneMenu != null ? sceneMenu : _container.InstantiatePrefab(menuPrefab, menuTransform);
        _menuInteraction = menuObj.GetComponent<CanvasGroup>() ?? menuObj.AddComponent<CanvasGroup>();
        SetMenuInteraction(false);
        menuObj.transform.localPosition = Vector3.zero;
        menuObj.transform.localRotation = Quaternion.identity;
        menuObj.transform.localScale = hideScale;

        if (sceneMenu != null)
        {
            menuObj.SetActive(false);
            yield break;
        }

        // Прогрев меню
        menuObj.SetActive(true);
        var samples = menuObj.transform.Find("Samples");
        var partGroups = GetFirstLevelChildren(samples);
        foreach (var partGroup in partGroups)
        {
            partGroup.gameObject.SetActive(true);
        }

        yield return null;

        foreach (var partGroup in partGroups)
        {
            partGroup.gameObject.SetActive(false);
        }
        menuObj.SetActive(false);
    }

    public static List<Transform> GetFirstLevelChildren(Transform parent)
    {
        List<Transform> children = new List<Transform>();

        if (parent == null) return children;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            children.Add(child);
        }

        return children;
    }



    private void Update()
    {

        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }

    }

    private void MenuBtnPressed(InputAction.CallbackContext context)
    {
        ToggleMenu();
    }

    //public void ToggleMenu()
    //{
    //    menuEnabled = !menuEnabled;
    //    if (menuEnabled)
    //    {
    //        menuObj.SetActive(true);
    //        MoveMenu(menuObj);
    //    }
    //    else
    //    {
    //        menuObj.SetActive(false);
    //    }
    //}


    //public async void MoveMenu(GameObject menu)
    //{
    //    // Создаем меню
    //    //GameObject menu = Instantiate(menuPrefab, transform.position, transform.rotation);
    //    menu.transform.localScale = Vector3.zero;

    //    float elapsed = 0f;
    //    Vector3 startPos = initMenuTransform.position;
    //    Vector3 startScale = Vector3.zero;

    //    while (elapsed < duration)
    //    {
    //        elapsed += Time.deltaTime;
    //        float t = elapsed / duration;

    //        menu.transform.position = Vector3.Lerp(startPos, menuTransform.position, t);
    //        menu.transform.rotation = Quaternion.Slerp(transform.rotation, menuTransform.rotation, t);
    //        menu.transform.localScale = Vector3.Lerp(startScale, Vector3.one, t);

    //        await Task.Yield();
    //    }

    //    // Финальная позиция
    //    menu.transform.position = menuTransform.position;
    //    menu.transform.rotation = menuTransform.rotation;
    //    menu.transform.localScale = Vector3.one;
    //}








    public void ToggleMenu()
    {

        if (menuObj == null) return;

        Debug.Log($"ToggleMenu {this}");
        MenuEnabled = !MenuEnabled;

        if (MenuEnabled)
        {
            ShowMenu();
        }
        else
        {
            HideMenu();
        }
    }

    private void ShowMenu()
    {
        Debug.Log("Показываем меню");

        if (menuObj == null) return;

        // Останавливаем все анимации
        StopAllCoroutines();

        // Запускаем анимацию появления
        StartCoroutine(ShowMenuCoroutine());
    }

    private IEnumerator ShowMenuCoroutine()
    {
        SetMenuInteraction(false);
        //_eventBus.Publish(new PlayOneShotRequestedEvent {SoundId = SoundId.MenuPopUp , Timestamp = System.DateTime.UtcNow});
        Vector3 startScale = hideScale;
        Vector3 startPosition = initMenuTransform.position;
        Quaternion startRotation = initMenuTransform.rotation;
        menuObj.SetActive(true);
        float elapsed = 0f;

        while (elapsed < appearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / appearDuration;

            menuObj.transform.position = Vector3.Lerp(startPosition, menuTransform.position, t);
            menuObj.transform.rotation = Quaternion.Slerp(startRotation, menuTransform.rotation, t);
            menuObj.transform.localScale = Vector3.Lerp(startScale, Vector3.one, t);

            yield return null;
        }

        menuObj.transform.position = menuTransform.position;
        menuObj.transform.rotation = menuTransform.rotation;
        menuObj.transform.localScale = Vector3.one;
        SetMenuInteraction(true);
        
        Debug.Log("Меню полностью показано");
    }

    public void HideMenu()
    {
        SetMenuInteraction(false);
        Debug.Log("Скрываем меню");

        if (menuObj == null) return;

        StopAllCoroutines();
        StartCoroutine(HideMenuCoroutine());
    }

    private IEnumerator HideMenuCoroutine()
    {
        //_eventBus.Publish(new PlayOneShotRequestedEvent { SoundId = SoundId.MenuPopDown, Timestamp = System.DateTime.UtcNow });
        Vector3 startScale = menuObj.transform.localScale;
        Vector3 targetScale = hideScale;
        Vector3 startPosition = menuObj.transform.position;
        Vector3 targetPosition = initMenuTransform.position;

        float elapsed = 0f;

        while (elapsed < appearDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (appearDuration * 0.5f);

            menuObj.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            menuObj.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            yield return null;
        }

        menuObj.transform.position = targetPosition;
        menuObj.transform.localScale = targetScale;
        menuObj.SetActive(false);
        Debug.Log("Меню полностью скрыто");
    }

    private void OnDestroy()
    {
        
        StopAllCoroutines();
    }

    private void SetMenuInteraction(bool enabled)
    {
        if (_menuInteraction == null) return;
        _menuInteraction.interactable = enabled;
        _menuInteraction.blocksRaycasts = enabled;
    }
}
