using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Presentation only: allObjects contains visual previews, never live assembly parts.
public class ObjectRowManager : MonoBehaviour
{
    public List<GameObject> allObjects = new List<GameObject>();
    [Min(1)] public int visibleObjectsCount = 5;
    [Min(0.01f)] public float spacing = 250f;
    [Min(0.01f)] public float transitionSpeed = 5f;
    public bool circularScrolling = true;
    [Header("Preview rotation")]
    [SerializeField] private bool rotatePreviews = true;
    [SerializeField, Range(0f, 180f)] private float previewRotationSpeed = 25f;
    public Transform objectsContainer;
    public Transform visibleObjectsParent;
    public Text pageText;
    public GameObject leftArrow;
    public GameObject rightArrow;

    private readonly List<Transform> visible = new List<Transform>(5);
    private readonly List<Transform> rotationTargets = new List<Transform>(5);
    private readonly List<Vector3> targets = new List<Vector3>(5);
    private Coroutine animation;
    private int currentPage;
    private bool initialized;

    private void Update()
    {
        if (!rotatePreviews || previewRotationSpeed <= 0f || visible.Count == 0) return;
        var rotation = Quaternion.AngleAxis(previewRotationSpeed * Time.deltaTime, Vector3.up);
        for (int i = 0; i < rotationTargets.Count; i++)
        {
            var preview = rotationTargets[i];
            if (preview != null && preview.gameObject.activeInHierarchy)
                preview.localRotation = rotation * preview.localRotation;
        }
    }

    private void OnEnable()
    {
        if (initialized) Refresh(false, 0);
    }

    private void Start()
    {
        initialized = true;
        Refresh(false, 0);
    }

    private void OnDisable()
    {
        if (animation != null) StopCoroutine(animation);
        animation = null;
        SnapToTargets();
    }

    public void NextPage() => ChangePage(currentPage + 1, 1);
    public void PreviousPage() => ChangePage(currentPage - 1, -1);
    public void UI_NextPage() => NextPage();
    public void UI_PreviousPage() => PreviousPage();
    public void GoToPage(int page) => ChangePage(page, page >= currentPage ? 1 : -1);
    public int GetCurrentPage() => currentPage;
    public int GetTotalPages() => Mathf.CeilToInt(allObjects.Count / (float)Mathf.Max(1, visibleObjectsCount));

    private void ChangePage(int page, int direction)
    {
        int count = GetTotalPages();
        int nextPage = count == 0 ? 0 : circularScrolling
            ? ((page % count) + count) % count
            : Mathf.Clamp(page, 0, count - 1);
        if (nextPage == currentPage) return;
        currentPage = nextPage;
        Refresh(Application.isPlaying && isActiveAndEnabled, direction);
    }

    // Also used by the editor builder to lay out the saved preview objects.
    public void RefreshLayout() => Refresh(false, 0);

    private void Refresh(bool animate, int direction)
    {
        if (animation != null) StopCoroutine(animation);
        animation = null;
        visible.Clear();
        rotationTargets.Clear();
        targets.Clear();
        int perPage = Mathf.Max(1, visibleObjectsCount);
        currentPage = Mathf.Clamp(currentPage, 0, Mathf.Max(0, GetTotalPages() - 1));
        for (int i = 0; i < allObjects.Count; i++)
        {
            var item = allObjects[i];
            if (item == null) continue;
            item.SetActive(false);
            if (objectsContainer != null) item.transform.SetParent(objectsContainer, false);
        }

        int start = currentPage * perPage;
        int count = Mathf.Min(perPage, allObjects.Count - start);
        for (int i = 0; i < count; i++)
        {
            var item = allObjects[start + i];
            if (item == null) continue;
            var parent = visibleObjectsParent != null ? visibleObjectsParent : transform;
            item.transform.SetParent(parent, false);
            var target = new Vector3((i - (count - 1) * 0.5f) * spacing, 0, 0);
            item.transform.localPosition = target + (animate ? Vector3.right * direction * spacing : Vector3.zero);
            visible.Add(item.transform);
            rotationTargets.Add(item.transform.Find("PreviewRotation") ?? item.transform);
            targets.Add(target);
            item.SetActive(true);
        }
        if (pageText != null) pageText.text = GetTotalPages() == 0 ? "0/0" : $"{currentPage + 1}/{GetTotalPages()}";
        if (leftArrow != null) leftArrow.SetActive(GetTotalPages() > 1 && (circularScrolling || currentPage > 0));
        if (rightArrow != null) rightArrow.SetActive(GetTotalPages() > 1 && (circularScrolling || currentPage + 1 < GetTotalPages()));
        if (animate && visible.Count > 0) animation = StartCoroutine(Animate(direction));
    }

    private IEnumerator Animate(int direction)
    {
        float elapsed = 0;
        float duration = 1f / Mathf.Max(0.01f, transitionSpeed);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));
            for (int i = 0; i < visible.Count; i++)
                visible[i].localPosition = targets[i] + Vector3.right * direction * spacing * (1 - t);
            yield return null;
        }
        SnapToTargets();
        animation = null;
    }

    private void SnapToTargets()
    {
        for (int i = 0; i < visible.Count; i++)
            if (visible[i] != null) visible[i].localPosition = targets[i];
    }

    public void AddObject(GameObject item)
    {
        if (item == null || allObjects.Contains(item)) return;
        allObjects.Add(item);
        Refresh(false, 0);
    }

    public void RemoveObject(GameObject item)
    {
        if (!allObjects.Remove(item)) return;
        if (item != null) item.SetActive(false);
        Refresh(false, 0);
    }

    public void SetCircularScrolling(bool value)
    {
        circularScrolling = value;
        Refresh(false, 0);
    }
}
