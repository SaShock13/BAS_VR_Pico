using UnityEngine;

public class PartHighlightService
{
    private DronePartView _hovered;

    public void Enter(DronePartView view)
    {
        if (_hovered == view)
            return;

        Exit();

        _hovered = view;
        _hovered.Highlight(true);
    }

    public void Exit()
    {
        if (_hovered == null)
            return;

        _hovered.Highlight(false);
        _hovered = null;
    }
}
