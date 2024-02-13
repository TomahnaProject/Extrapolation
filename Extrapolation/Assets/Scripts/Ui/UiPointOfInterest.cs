using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UiPointOfInterest : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerClickHandler
{
    public TextMeshProUGUI label;

    public List<Image> crossOutlines;
    public List<Image> crossColors;
    public NodeViewPanel parent;
    public PoiOnNode pointOnNode;

    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            foreach (Image outline in crossOutlines)
                outline.color = _selected ? Color.white : Color.black;
        }
    }

    Vector3 _startDirection;
    Vector2 _mouseDragOffset;
    bool _allowDrag;
    bool _selected = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _allowDrag = Input.GetMouseButton(NodeViewPanel.mouseSelectButton);
        if (_allowDrag)
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
        if (!_allowDrag)
            return;
        transform.position = eventData.position + _mouseDragOffset;
        pointOnNode.Direction = parent.RenderCam.ScreenPointToRay(transform.localPosition).direction;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_allowDrag)
            return;
        parent.mainHandler.EditDo(new MovePointOnNodeOperation(pointOnNode, _startDirection, pointOnNode.Direction));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Input.GetMouseButtonUp(NodeViewPanel.mouseSelectButton))
            parent.mainHandler.ActivePoi = pointOnNode.Point;
    }
}
