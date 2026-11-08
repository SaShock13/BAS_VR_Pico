using System.Collections.Generic;
using UnityEngine;

public class SocketPreviewSystem
{
    private readonly Material _validMaterial;
    private readonly Material _invalidMaterial;

    private GameObject _previewRoot;

    public SocketPreviewSystem(
        Material validMaterial,
        Material invalidMaterial)
    {
        _validMaterial = validMaterial;
        _invalidMaterial = invalidMaterial;
    }

    //public void ShowPreview(
    //    DronePartView sourcePart,
    //    Transform previewAnchor,
    //    bool isValid)
    //{
    //    HidePreview();

    //    if (sourcePart == null)
    //        return;

    //    _previewRoot = new GameObject("SocketPreview");

    //    _previewRoot.transform.SetPositionAndRotation(
    //        previewAnchor.position,
    //        previewAnchor.rotation);

    //    MeshFilter[] meshFilters =
    //        sourcePart.GetComponentsInChildren<MeshFilter>(true);

    //    foreach (MeshFilter sourceMeshFilter in meshFilters)
    //    {
    //        if (sourceMeshFilter.sharedMesh == null)
    //            continue;

    //        CreateGhostMesh(
    //            sourceMeshFilter,
    //            isValid);
    //    }
    //}

    public void HidePreview()
    {
        if (_previewRoot != null)
        {
            _previewRoot.SetActive(false);
            Object.Destroy(_previewRoot);

            _previewRoot = null;
        }
    }

    //private void CreateGhostMesh(
    //    MeshFilter sourceMeshFilter,
    //    bool isValid)
    //{
    //    GameObject ghostObject =
    //        new GameObject(sourceMeshFilter.name + "_Preview");

    //    ghostObject.transform.SetParent(
    //        _previewRoot.transform,
    //        false);

    //    // Копируем локальный transform
    //    ghostObject.transform.localPosition =
    //        sourceMeshFilter.transform.localPosition;

    //    ghostObject.transform.localRotation =
    //        sourceMeshFilter.transform.localRotation;

    //    ghostObject.transform.localScale =
    //        sourceMeshFilter.transform.localScale;

    //    // MeshFilter
    //    MeshFilter meshFilter =
    //        ghostObject.AddComponent<MeshFilter>();

    //    meshFilter.sharedMesh =
    //        sourceMeshFilter.sharedMesh;

    //    // MeshRenderer
    //    MeshRenderer meshRenderer =
    //        ghostObject.AddComponent<MeshRenderer>();

    //    meshRenderer.material =
    //        isValid
    //            ? _validMaterial
    //            : _invalidMaterial;

    //    // Отключаем shadows
    //    meshRenderer.shadowCastingMode =
    //        UnityEngine.Rendering.ShadowCastingMode.Off;

    //    meshRenderer.receiveShadows = false;

    //    // Preview layer (опционально)
    //    // ghostObject.layer = LayerMask.NameToLayer("Preview");
    //}

    public void ShowPreview(
    DronePartView sourcePart,
    Transform previewAnchor,
    bool isValid)
    {
        HidePreview();

        if (sourcePart == null)
            return;

        _previewRoot = new GameObject("SocketPreview");

        _previewRoot.transform.SetPositionAndRotation(
            previewAnchor.position,
            previewAnchor.rotation);

        _previewRoot.transform.localScale =
            sourcePart.transform.lossyScale;

        var previewTransforms = new Dictionary<Transform, Transform> { { sourcePart.transform, _previewRoot.transform } };

        MeshFilter[] meshFilters =
            sourcePart.GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter sourceMeshFilter in meshFilters)
        {
            if (sourceMeshFilter.sharedMesh == null)
                continue;

            // Hidden helper meshes must not become visible ghost geometry.
            if (!sourceMeshFilter.gameObject.activeInHierarchy ||
                !sourceMeshFilter.TryGetComponent<MeshRenderer>(out var sourceRenderer) ||
                !sourceRenderer.enabled || sourceRenderer.forceRenderingOff)
                continue;

            CreateGhostMesh(
                previewTransforms,
                sourceMeshFilter,
                isValid);
        }
    }

    private void CreateGhostMesh(
    Dictionary<Transform, Transform> previewTransforms,
    MeshFilter sourceMeshFilter,
    bool isValid)
    {
        GameObject ghostObject =
            new GameObject(sourceMeshFilter.name + "_Preview");

        // Preserve nested scales and rotations without cloning gameplay components.
        ghostObject.transform.SetParent(
            GetPreviewTransform(sourceMeshFilter.transform, previewTransforms), false);

        // MeshFilter
        MeshFilter meshFilter =
            ghostObject.AddComponent<MeshFilter>();

        meshFilter.sharedMesh =
            sourceMeshFilter.sharedMesh;

        // MeshRenderer
        MeshRenderer meshRenderer =
            ghostObject.AddComponent<MeshRenderer>();

        meshRenderer.sharedMaterial =
            isValid
                ? _validMaterial
                : _invalidMaterial;

        meshRenderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        meshRenderer.receiveShadows = false;
    }
    private Transform GetPreviewTransform(
        Transform source, Dictionary<Transform, Transform> previewTransforms)
    {
        if (previewTransforms.TryGetValue(source, out var preview))
            return preview;

        var parent = GetPreviewTransform(source.parent, previewTransforms);
        preview = new GameObject(source.name).transform;
        preview.SetParent(parent, false);
        preview.localPosition = source.localPosition;
        preview.localRotation = source.localRotation;
        preview.localScale = source.localScale;
        previewTransforms.Add(source, preview);
        return preview;
    }
}
