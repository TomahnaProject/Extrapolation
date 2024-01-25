using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rename any object in any runtime hierarchy.
/// </summary>
public class RenameObjectOperation : IEditOperation
{
    readonly string _oldName, _newName;
    readonly GameObject _obj;
    readonly Action<GameObject> _callback;

    public RenameObjectOperation(GameObject obj, string newName, Action<GameObject> callback = null)
    {
        _obj = obj;
        _oldName = obj.name;
        _newName = newName;
        _callback = callback;
    }

    public RenameObjectOperation(GameObject obj, string oldName, string newName, Action<GameObject> callback = null)
    {
        _obj = obj;
        _oldName = oldName;
        _newName = newName;
        _callback = callback;
    }

    public bool CanUndo => true;

    public void Do(MainHandler handler)
    {
        _obj.name = _newName;
        handler.OnHierarchyItemRenamed(_obj, _oldName, _newName);
        _callback?.Invoke(_obj);
    }

    public void Undo(MainHandler handler)
    {
        _obj.name = _oldName;
        handler.OnHierarchyItemRenamed(_obj, _newName, _oldName);
        _callback?.Invoke(_obj);
    }
}
