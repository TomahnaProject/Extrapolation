using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unlinks a point of interest from a node it's associated with.
/// </summary>
public class UnlinkPointOfInterestOperation : IEditOperation
{
    readonly PoiOnNode _pon;

    public UnlinkPointOfInterestOperation(PoiOnNode pon)
    {
        _pon = pon;
    }

    public bool CanUndo => false;

    public void Do(MainHandler handler)
    {
        handler.UnlinkPointOfInterest(_pon);
    }

    public void Undo(MainHandler handler)
    {
        throw new NotImplementedException();
    }
}
