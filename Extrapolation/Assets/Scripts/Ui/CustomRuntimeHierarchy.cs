using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RuntimeInspectorNamespace;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class CustomRuntimeHierarchy : MonoBehaviour
{
    public FocusablePanel focus;
    public RuntimeHierarchy hierarchy;
    public MainHandler mainHandler;

    public delegate void SelectionChangedDelegate(ReadOnlyCollection<Transform> selection);
    public SelectionChangedDelegate OnHighlightChanged;
    public IReadOnlyList<HighlightableHierarchyField> Fields => fields;

    readonly List<HighlightableHierarchyField> fields = new();

    void Awake()
    {
        fields.Clear();
    }

    public bool SetHighlighted(IList<Transform> selection)
    {
        if (selection == null || selection.Count == 0)
            selection = new List<Transform>();
        foreach (HighlightableHierarchyField field in fields)
        {
            bool highlighted = selection.Contains(field.field.Data.BoundTransform);
            field.IsHighlighted = highlighted;
        }
        OnHighlightChanged?.Invoke(new ReadOnlyCollection<Transform>(selection));
        return true;
    }

    public void RegisterField(HighlightableHierarchyField field)
    {
        fields.Add(field);
    }

    public void UnregisterField(HighlightableHierarchyField field)
    {
        fields.Remove(field);
    }

    public void Update()
    {
        if (!focus.Active)
            return;
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (hierarchy.CurrentSelection.Count > 0)
            {
                Transform active = hierarchy.CurrentSelection[0];
                HighlightableHierarchyField field = fields.First(field => field.field.Data.BoundTransform == active);
                field.StartRenaming();
            }
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            Transform active = hierarchy.CurrentSelection[0];
            mainHandler.TryDelete(active);
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            hierarchy.Select((Transform)null);
        }
    }
}
