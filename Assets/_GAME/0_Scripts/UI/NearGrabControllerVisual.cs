using Unity.XR.CoreUtils.Bindings;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class NearGrabControllerVisual : MonoBehaviour
{
    [SerializeField] private NearFarInteractor interactor;
    [SerializeField] private GameObject[] visualRoots;
    private readonly BindingsGroup _bindings = new BindingsGroup();
    private bool[] _wasActive;
    private bool _hidden;

    private void OnEnable()
    {
        if (interactor == null) return;
        _bindings.AddBinding(interactor.selectionRegion.Subscribe(SetRegion));
        SetRegion(interactor.selectionRegion.Value);
    }

    internal void SetRegion(NearFarInteractor.Region region)
    {
        bool hide = region == NearFarInteractor.Region.Near;
        if (hide == _hidden) return;
        if (!hide) { Restore(); return; }
        if (visualRoots == null) return;
        if (_wasActive == null || _wasActive.Length != visualRoots.Length) _wasActive = new bool[visualRoots.Length];
        _hidden = true;
        for (int i = 0; i < visualRoots.Length; i++)
        {
            var root = visualRoots[i];
            _wasActive[i] = root != null && root.activeSelf;
            if (root != null) root.SetActive(false);
        }
    }

    private void Restore()
    {
        if (!_hidden) return;
        _hidden = false;
        for (int i = 0; i < visualRoots.Length; i++)
            if (visualRoots[i] != null) visualRoots[i].SetActive(_wasActive[i]);
    }

    private void OnDisable() { _bindings.Clear(); Restore(); }
}
