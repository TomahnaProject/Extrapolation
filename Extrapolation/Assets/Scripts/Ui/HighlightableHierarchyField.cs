using System.Collections;
using System.Collections.Generic;
using RuntimeInspectorNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HighlightableHierarchyField : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image highlightBackground;
    public TMP_InputField renameInputField;

    private bool m_isHighlighted;
    public bool IsHighlighted
    {
        get => m_isHighlighted;
        set
        {
            m_isHighlighted = value;
            if (m_isHighlighted)
                highlightBackground.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);
            else
                highlightBackground.color = Color.clear;
        }
    }
    
    public HierarchyField field;
    public CustomRuntimeHierarchy hierarchy;

    void Start()
    {
        if (field == null)
            field = GetComponent<HierarchyField>();
        hierarchy = field.Hierarchy.GetComponent<CustomRuntimeHierarchy>();
        hierarchy.RegisterField(this);
        renameInputField.gameObject.SetActive(false);
    }

    public void StartRenaming()
    {
        string curObjName = field.Data.BoundTransform.name;
        renameInputField.gameObject.SetActive(true);
        renameInputField.text = curObjName;
        EventSystem.current.SetSelectedGameObject(renameInputField.gameObject);
        renameInputField.selectionAnchorPosition = 0;
        renameInputField.selectionFocusPosition = curObjName.Length;
        renameInputField.onEndEdit.AddListener(OnEndRename);
    }

    void OnEndRename(string newName)
    {
        renameInputField.onEndEdit.RemoveListener(OnEndRename);
        renameInputField.gameObject.SetActive(false);
        if (!EventSystem.current.alreadySelecting)
            EventSystem.current.SetSelectedGameObject(null);
        if (field.Data.BoundTransform.name != newName)
            hierarchy.mainHandler.EditDo(new RenameObjectOperation(field.Data.BoundTransform.gameObject, newName, (obj) => hierarchy.hierarchy.RefreshNameOf(obj.transform)));
    }

    void OnDestroy()
    {
        hierarchy?.UnregisterField(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (field.Data.BoundTransform != null)
            hierarchy.SetHighlighted(new List<Transform>() { field.Data.BoundTransform });
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (field.Data.BoundTransform != null)
            hierarchy.SetHighlighted(null);
    }
}
