using System;
using UnityEngine;

public class NewSelectionService : ISelectionService
{
    public SelectionTarget? Current { get; set; }

    public event Action<SelectionTarget?> Changed;

    public void Select(SelectionTarget target)
    {
        Current = target;


        Debug.Log($"////////Selected NewSelectionService {target.PartId}");
        Changed?.Invoke(Current);
    }

    public void Clear()
    {
        Current = null;
        Changed?.Invoke(null);
    }
}