using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FocusablePanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool Active { get; set; }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Active = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Active = false;
    }
}
