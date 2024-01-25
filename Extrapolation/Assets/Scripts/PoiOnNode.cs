using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoiOnNode : MonoBehaviour
{
    /// <summary>
    /// Pitch as seen from the node.
    /// </summary>
    public float Pitch
    {
        get => transform.localEulerAngles.x;
        set => transform.localEulerAngles = new Vector3(value, Heading, 0);
    }

    /// <summary>
    /// Heading (relative to +Z) as seen from the node.
    /// </summary>
    public float Heading
    {
        get => transform.localEulerAngles.y;
        set => transform.localEulerAngles = new Vector3(Pitch, value, 0);
    }

    public Vector3 Direction
    {
        get => transform.forward;
        set => transform.localEulerAngles = Quaternion.LookRotation(value, Vector3.up).eulerAngles;
    }

    /// <summary>
    /// Node this PoI is seen from.
    /// </summary>
    public NodeRenderer Node { get; set; }

    /// <summary>
    /// Actual PoI we're looking at.
    /// </summary>
    public PointOfInterest Point { get; set; }
}
