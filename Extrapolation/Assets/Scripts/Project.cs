using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// A currently active project of node alignments using points of interest.
/// </summary>
public class Project
{
    /// <summary>
    /// Name of the project (also used in the file name when saving).
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Nodes of this project in the scene.
    /// </summary>
    public List<NodeRenderer> Nodes { get; set; }

    /// <summary>
    /// The folder the project is saved in.
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// Whether this project has unsaved changes.
    /// </summary>
    public bool Unsaved { get; set; }

    /// <summary>
    /// All the points of interest in the scene.
    /// </summary>
    public List<PointOfInterest> PointsOfInterest { get; set; } = new();

    /// <summary>
    /// All the links between points of interest and nodes.
    /// </summary>
    public List<PoiOnNode> PoiOnNodes { get; set; } = new();

    /// <summary>
    /// The next generated ID for a point of interest.
    /// This increases linearly as the user places more POIs. Ids are used to keep track of POIs, so they should always be distinct.
    /// </summary>
    public int NextPoiId { get; set; } = 1;
}
