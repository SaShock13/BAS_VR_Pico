using UnityEngine;

public class Outline : MonoBehaviour
{
    [SerializeField]
    private Material _outlineMaterial;

    [SerializeField]
    private float _scale = 1.03f;

    private GameObject _outlineRoot;

    public void Initialize()
    {
        if (_outlineRoot != null)
            return;

        _outlineRoot = new GameObject("Outline");

        _outlineRoot.transform.SetParent(
            transform,
            false);

        CreateOutlineMeshes();

        _outlineRoot.SetActive(false);
    }

    public void SetHighlighted(bool value)
    {
        if (_outlineRoot == null)
            return;

        _outlineRoot.SetActive(value);
    }

    public void SetOutlineMaterial(Material material)
    {
        _outlineMaterial = material;
    }


    private void CreateOutlineMeshes()
    {
        var meshFilters =
            GetComponentsInChildren<MeshFilter>(true);

        foreach (var sourceFilter in meshFilters)
        {
            var sourceRenderer =
                sourceFilter.GetComponent<MeshRenderer>();

            if (sourceRenderer == null)
                continue;

            var outlineObject =
                new GameObject(
                    sourceFilter.name + "_Outline");

            outlineObject.transform.SetParent(
                _outlineRoot.transform,
                false);

            outlineObject.transform.localPosition =
                sourceFilter.transform.localPosition;

            outlineObject.transform.localRotation =
                sourceFilter.transform.localRotation;

            outlineObject.transform.localScale =
                sourceFilter.transform.localScale * _scale;

            var mf =
                outlineObject.AddComponent<MeshFilter>();

            var mr =
                outlineObject.AddComponent<MeshRenderer>();

            mf.sharedMesh =
                sourceFilter.sharedMesh;

            mr.sharedMaterial =
                _outlineMaterial;

            mr.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            mr.receiveShadows = false;
        }
    }
}