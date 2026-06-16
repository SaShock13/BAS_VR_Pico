using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class SliderEndDragHandler :
    MonoBehaviour,
    IEndDragHandler
{
    public event Action Released;

    public void OnEndDrag(
        PointerEventData eventData)
    {
        Released?.Invoke();
    }
}