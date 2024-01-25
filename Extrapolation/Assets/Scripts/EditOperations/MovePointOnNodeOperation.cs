using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Changes a point on node's <see cref="PoiOnNode.Direction"/>.
/// </summary>
public class MovePointOnNodeOperation : IEditOperation
{
    readonly PoiOnNode _pon;
    Vector3 _oldDirection;
    Vector3 _newDirection;

    public MovePointOnNodeOperation(PoiOnNode pon, Vector3 oldDirection, Vector3 newDirection)
    {
        _pon = pon;
        _oldDirection = oldDirection;
        _newDirection = newDirection;
    }

    public bool CanUndo => true;

    public void Do(MainHandler handler)
    {
        handler.MovePointOnNode(_pon, _oldDirection, _newDirection);
    }

    public void Undo(MainHandler handler)
    {
        handler.MovePointOnNode(_pon, _newDirection, _oldDirection);
    }
}
