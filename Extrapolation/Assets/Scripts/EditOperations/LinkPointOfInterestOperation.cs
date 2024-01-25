using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Link an existing point of interest to a node.
/// </summary>
public class LinkPointOfInterestOperation : IEditOperation
{
    readonly PointOfInterest _poi;
    readonly NodeRenderer _node;
    readonly Vector3 _direction;
    PoiOnNode _poiOnNode;

    public LinkPointOfInterestOperation(PointOfInterest poi, NodeRenderer node, Vector3 direction)
    {
        _poi = poi;
        _node = node;
        _direction = direction;
    }

    public bool CanUndo => true;

    public void Do(MainHandler handler)
    {
        _poiOnNode = handler.LinkPointOfInterest(_poi, _node, _direction);
    }

    public void Undo(MainHandler handler)
    {
        handler.UnlinkPointOfInterest(_poiOnNode);
    }
}
