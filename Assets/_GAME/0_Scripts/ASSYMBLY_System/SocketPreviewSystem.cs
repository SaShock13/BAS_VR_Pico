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
            previewAnchor.lossyScale;

        MeshFilter[] meshFilters =
            sourcePart.GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter sourceMeshFilter in meshFilters)
        {
            if (sourceMeshFilter.sharedMesh == null)
                continue;

            CreateGhostMesh(
                sourcePart.transform,
                sourceMeshFilter,
                isValid);
        }
    }

    private void CreateGhostMesh(
    Transform sourceRoot,
    MeshFilter sourceMeshFilter,
    bool isValid)
    {
        GameObject ghostObject =
            new GameObject(sourceMeshFilter.name + "_Preview");

        ghostObject.transform.SetParent(
            _previewRoot.transform,
            false);

        // ВЫЧИСЛЯЕМ ПОЗИЦИЮ ОТНОСИТЕЛЬНО ROOT
        Vector3 localPosition =
            sourceRoot.InverseTransformPoint(
                sourceMeshFilter.transform.position);

        Quaternion localRotation =
            Quaternion.Inverse(sourceRoot.rotation) *
            sourceMeshFilter.transform.rotation;

        // scale
        Vector3 localScale =
            sourceMeshFilter.transform.localScale;

        ghostObject.transform.localPosition =
            localPosition;

        ghostObject.transform.localRotation =
            localRotation;

        ghostObject.transform.localScale =
            localScale;

        // MeshFilter
        MeshFilter meshFilter =
            ghostObject.AddComponent<MeshFilter>();

        meshFilter.sharedMesh =
            sourceMeshFilter.sharedMesh;

        // MeshRenderer
        MeshRenderer meshRenderer =
            ghostObject.AddComponent<MeshRenderer>();

        meshRenderer.material =
            isValid
                ? _validMaterial
                : _invalidMaterial;

        meshRenderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        meshRenderer.receiveShadows = false;
    }
}