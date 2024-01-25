using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deletes a point of interest and associated <see cref="PoiOnNode"/>.
/// </summary>
public class DeletePointOfInterestOperation : IEditOperation
{
    private PointOfInterest _poi;

    public DeletePointOfInterestOperation(PointOfInterest poi)
    {
        _poi = poi;
    }

    public bool CanUndo => false;

    public void Do(MainHandler handler)
    {
        handler.DeletePointOfInterest(_poi);
    }

    public void Undo(MainHandler handler)
    {
        throw new System.NotImplementedException();
    }
}
