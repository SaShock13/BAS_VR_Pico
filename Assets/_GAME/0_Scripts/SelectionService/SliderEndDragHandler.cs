using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class SliderEndDragHandler :
    MonoBehaviour,
    IEndDragHandler,
    IPointerUpHandler
{
    public event Action Released;

    public void OnEndDrag(
        PointerEventData eventData)
    {
        Released?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Released?.Invoke();
    }
}
