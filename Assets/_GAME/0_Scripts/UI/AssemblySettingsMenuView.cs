using UnityEngine;

public sealed class AssemblySettingsMenuView : MonoBehaviour
{
    [SerializeField] private GameObject settingsPage;
    [SerializeField] private GameObject[] mainControls;

    public void OpenSettings() => Show(true);
    public void CloseSettings() => Show(false);
    private void OnEnable() => Show(false);

    private void Show(bool settings)
    {
        settingsPage.SetActive(settings);
        foreach (var control in mainControls) control.SetActive(!settings);
    }
}
