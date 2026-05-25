using System;

public interface ISelectionService 
{
    public SelectionTarget? Current { get;  set; }

    public event Action<SelectionTarget?> Changed;

    public void Select(SelectionTarget target);

    public void Clear();
}
