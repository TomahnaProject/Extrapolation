using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Create a new point of interest, and link it to a specific node.
/// </summary>
public class CreatePointOfInterestOperation : IEditOperation
{
    readonly NodeRenderer _node;
    readonly Vector3 _direction;
    PoiOnNode _poiOnNode;

    public CreatePointOfInterestOperation(NodeRenderer node, Vector3 direction)
    {
        _node = node;
        _direction = direction;
    }

    public bool CanUndo => true;

    public void Do(MainHandler handler)
    {
        _poiOnNode = handler.CreatePointOfInterest(_node, _direction);
    }

    public void Undo(MainHandler handler)
    {
        handler.UnlinkPointOfInterest(_poiOnNode);
    }
}
