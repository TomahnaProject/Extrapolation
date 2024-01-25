using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEditOperation
{
    public void Do(MainHandler handler);
    public void Undo(MainHandler handler);
    
    // Whether this operation is reversible. Ideally it should always return true, but for now this just makes some things much more convenient.
    public bool CanUndo { get; }
}
