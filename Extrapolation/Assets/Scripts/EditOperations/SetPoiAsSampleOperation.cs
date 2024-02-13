using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetPoiAsSampleOperation : IEditOperation
{
    public bool CanUndo => true;

    readonly PointOfInterest _poi;
    readonly bool _oldIsSample;
    readonly bool _isSample;

    public SetPoiAsSampleOperation(PointOfInterest poi, bool isSample)
    {
        _poi = poi;
        _oldIsSample = _poi.IsSample;
        _isSample = isSample;
    }

    public void Do(MainHandler handler)
    {
        handler.SetPoiAsSample(_poi, _isSample);
    }

    public void Undo(MainHandler handler)
    {
        handler.SetPoiAsSample(_poi, _oldIsSample);
    }
}
