using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

[RequireComponent(typeof(XRKeyboard))]
public sealed class SpatialKeyboardLanguageView : MonoBehaviour
{
    [SerializeField] private XRKeyboardLayout layout;
    [SerializeField] private XRKeyboardConfig russian;
    [SerializeField] private GameObject[] extraRussianKeys;

    private XRKeyboard _keyboard;

    private void Awake() => _keyboard = GetComponent<XRKeyboard>();

    private void OnEnable() => _keyboard.onLayoutChanged.AddListener(OnLayoutChanged);

    private void Start() => RefreshExtraKeys();

    private void OnDisable() => _keyboard.onLayoutChanged.RemoveListener(OnLayoutChanged);

    private void OnLayoutChanged(KeyboardLayoutEventArgs _) => RefreshExtraKeys();

    private void RefreshExtraKeys()
    {
        bool show = layout.activeKeyMapping == russian;
        foreach (var key in extraRussianKeys) key.SetActive(show);
    }
}
