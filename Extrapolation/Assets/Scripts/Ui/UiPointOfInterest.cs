using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UiPointOfInterest : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public TextMeshProUGUI label;
    public NodeViewPanel parent;
    public PoiOnNode pointOnNode;

    Vector3 _startDirection;
    Vector2 _mouseDragOffset;
    bool allowDrag;

    public void OnBeginDrag(PointerEventData eventData)
    {
        allowDrag = Input.GetMouseButton(NodeViewPanel.mouseSelectButton);
        if (allowDrag)
        {
            _startDirection = pointOnNode.Direction;
            _mouseDragOffset = new Vector2(
                transform.position.x - eventData.position.x,
                transform.position.y - eventData.position.y
            );
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!allowDrag)
            return;
        transform.position = eventData.position + _mouseDragOffset;
        pointOnNode.Direction = parent.RenderCam.ScreenPointToRay(transform.localPosition).direction;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!allowDrag)
            return;
        parent.mainHandler.EditDo(new MovePointOnNodeOperation(pointOnNode, _startDirection, pointOnNode.Direction));
    }
}
