using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NodeHierarchyField : MonoBehaviour
{
    public CanvasGroup statsGroup;
    public Text statsLabel;

    void Start()
    {
        statsGroup.alpha = 0;
        statsGroup.blocksRaycasts = statsGroup.interactable = false;
    }
}
