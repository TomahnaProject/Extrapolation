using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PoiHierarchyField : MonoBehaviour
{
    public CanvasGroup statsGroup;
    public Text statsLabel;
    public Toggle isSampleToggle;

    bool _isSettingSampleToggle = false;

    void Start()
    {
        statsGroup.alpha = 0;
        statsGroup.blocksRaycasts = statsGroup.interactable = false;
    }

    public void SetIsSample(bool isSample)
    {
        _isSettingSampleToggle = true;
        isSampleToggle.isOn = isSample;
        _isSettingSampleToggle = false;
    }

    public void OnSampleToggleChanged(bool on)
    {
        if (_isSettingSampleToggle)
            return;
        HighlightableHierarchyField field = GetComponent<HighlightableHierarchyField>();
        field.hierarchy.mainHandler.EditDo(
            new SetPoiAsSampleOperation(field.field.Data.BoundTransform.GetComponent<PointOfInterest>(), on));
    }
}
