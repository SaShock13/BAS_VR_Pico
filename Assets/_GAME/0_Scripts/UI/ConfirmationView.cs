using System;
using TMPro;
using UnityEngine;

public sealed class ConfirmationView : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text message;
    private Action _pending;

    public void Show(string text, Action confirmed)
    {
        if (!isActiveAndEnabled) return;
        message.text = text;
        _pending = confirmed;
        panel.SetActive(true);
    }

    public void Confirm()
    {
        var confirmed = _pending;
        Cancel();
        confirmed?.Invoke();
    }

    public void Cancel()
    {
        _pending = null;
        if (panel != null) panel.SetActive(false);
    }

    private void OnDisable() => Cancel();
}
