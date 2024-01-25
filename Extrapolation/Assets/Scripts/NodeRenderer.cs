using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 360° node object.
/// </summary>
public class NodeRenderer : MonoBehaviour
{
    [Tooltip("Sphere that renders the cube, with its 6 material slots.")]
    public Renderer sphere;

    [Tooltip("Object shown when the node is hovered.")]
    public Renderer hoverOutline;

    [Tooltip("Object shown when the node is selected.")]
    public Renderer selectOutline;

    [Tooltip("ID of the sphere (created from the game files, not user-changeable).")]
    public string id;

    [Tooltip("Name of the 6 texture files corresponding to the 6 faces of the cube.")]
    public string[] texturePaths;

    [Tooltip("Where PoiOnNodes for this node are instantiated.")]
    public Transform poiOnNodesParent;

    /// <summary>
    /// All the <see cref="PointOfInterest"/> we can see from this node.
    /// </summary>
    public List<PoiOnNode> pointsOfInterest = new();

    [Tooltip("Whether a random position was assigned to this node.")]
    public bool positionInitialized = false;

    public Texture2D getTexture(FaceIndex index)
    {
        MaterialPropertyBlock block = new();
        sphere.GetPropertyBlock(block, (int)index);
        return (Texture2D)block.GetTexture("_MainTex");
    }
    public void setTexture(FaceIndex index, Texture2D tex)
    {
        MaterialPropertyBlock block = new();
        sphere.GetPropertyBlock(block, (int)index);
        block.SetTexture("_MainTex", tex);
        sphere.SetPropertyBlock(block, (int)index);
    }
}
