using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A point of interest in the 3D space.
/// </summary>
public class PointOfInterest : MonoBehaviour
{
    /// <summary>
    /// ID of this POI. Used by various systems to track the POI, so should only be assigned once, and never be a duplicate of another POI's id.
    /// </summary>
    public int Id
    {
        get => _id;
        set
        {
            if (_id == 0)
                _id = value;
            else
                throw new InvalidOperationException("Cannot change already initialized POI ID.");
        }
    }
    int _id = 0;

    /// <summary>
    /// Which nodes can see this PoI and thus contribute to solving its actual position.
    /// </summary>
    public List<PoiOnNode> linkedNodes = new();

    /// <summary>
    /// Whether we have a vague idea where this point is located.
    /// </summary>
    public bool positionInitialized = false;

    public bool Selected
    {
        // TODO
        get;
        set;
    }

    /// <summary>
    /// Whether this point of interest can move node, or whether it's just used to sample a low-accuracy point of the environment.
    /// </summary>
    public bool IsSample { get; set; }
}
