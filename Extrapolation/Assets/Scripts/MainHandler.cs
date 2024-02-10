using SimpleFileBrowser;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.IO;
using Myst3;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using RuntimeInspectorNamespace;
using System.Collections.ObjectModel;
using DynamicPanels;
using UnityEngine.EventSystems;

/// <summary>
/// Handles most of the app's logic.
/// </summary>
public class MainHandler : MonoBehaviour
{
    [Tooltip("Main UI. Will be disabled when contextual windows pop up.")]
    public CanvasGroup mainUiGroup;

    [Tooltip("Pop-up box used to ask the user to pick from a list of options.")]
    public QuestionBox questionBox;

    [Tooltip("Instantiated for each 360 node.")]
    public NodeRenderer nodePrefab;

    [Tooltip("Instantiated for each point of interest, in the 3D space.")]
    public PointOfInterest poiPrefab;

    [Tooltip("Instantiated for each point of interest on a sphere.")]
    public PoiOnNode poiOnNodePrefab;

    [Tooltip("Parent for instantiated node objects.")]
    public Transform nodesParent;

    [Tooltip("Parent for instantiated point of interest objects.")]
    public Transform pointsOfInterestParent;

    public Button saveProjectButton;

    [Tooltip("Toggle to run/pause the solver.")]
    public Toggle solverToggle;

    [Tooltip("Text data to show current solver status.")]
    public TextMeshProUGUI solverData;

    [Tooltip("The hierarchy of currently loaded nodes.")]
    public CustomRuntimeHierarchy nodeHierarchy;

    [Tooltip("The hierarchy of currently loaded points of interest.")]
    public CustomRuntimeHierarchy poiHierarchy;

    [Tooltip("The prefab that's instantiated when the user requests a new 3D view.")]
    public NodeViewPanel threedeeViewPrefab;

    [Tooltip("The handler for dynamic panels.")]
    public DynamicPanelsCanvas panelsCanvas;

    [Tooltip("The prefab for points of interest as seen in NodeViewPanels.")]
    public UiPointOfInterest uiPoiPrefab;

    [Tooltip("Position solver.")]
    public ThreadedSphericalPoseSolver solver;

    public NodeRenderer ActiveNode
    {
        get => nodeHierarchy.hierarchy.CurrentSelection.FirstOrDefault()?.GetComponent<NodeRenderer>();
        set
        {
            if (value == null)
                nodeHierarchy.hierarchy.Select((Transform)null);
            else if (value.transform != ActiveNode)
                nodeHierarchy.hierarchy.Select(value.transform);
        }
    }

    public PointOfInterest ActivePoi
    {
        get => poiHierarchy.hierarchy.CurrentSelection.FirstOrDefault()?.GetComponent<PointOfInterest>();
        set
        {
            if (value == null)
                poiHierarchy.hierarchy.Select((Transform)null);
            else if (value.transform != ActiveNode)
                poiHierarchy.hierarchy.Select(value.transform);
        }
    }

    public IReadOnlyList<NodeRenderer> HoveredNodes
    {
        get => _hoveredNodes;
        set
        {
            if (value != null)
                nodeHierarchy.GetComponent<CustomRuntimeHierarchy>().SetHighlighted(value.Select(nr => nr.transform).ToList());
            else
            {
                nodeHierarchy.GetComponent<CustomRuntimeHierarchy>().SetHighlighted(null);
                value = new List<NodeRenderer>();
            }
            foreach (NodeRenderer node in _hoveredNodes)
                node.hoverOutline.gameObject.SetActive(false);
            _hoveredNodes = value;
            foreach (NodeRenderer node in _hoveredNodes)
                node.hoverOutline.gameObject.SetActive(true);
        }
    }
    IReadOnlyList<NodeRenderer> _hoveredNodes = new List<NodeRenderer>();

    public IReadOnlyList<PointOfInterest> HoveredPois
    {
        get => _hoveredPois;
        set
        {
            if (value != null)
                poiHierarchy.GetComponent<CustomRuntimeHierarchy>().SetHighlighted(value.Select(poi => poi.transform).ToList());
            else
                poiHierarchy.GetComponent<CustomRuntimeHierarchy>().SetHighlighted(null);
        }
    }
    IReadOnlyList<PointOfInterest> _hoveredPois = new List<PointOfInterest>();

    public Action<NodeRenderer, string, string> OnNodeRenamed;
    public Action<PointOfInterest, string, string> OnPoiRenamed;
    public Action<PoiOnNode> OnPoiLinkedToNode;
    public Action<PoiOnNode> OnPoiUnlinkedFromNode;
    public Action<PoiOnNode, Vector3, Vector3> OnPonMoved;

    /// <summary>
    /// Info about the currently loaded project.
    /// </summary>
    Project _project = null;

    /// <summary>
    /// Keeps track of loaded node textures.
    /// </summary>
    readonly List<Texture2D> _curTextures = new();

    /// <summary>
    /// List of edit operations. So we can support Ctrl-Z, detect unsaved project, etc.
    /// </summary>
    readonly Stack<IEditOperation> _editOperations = new();

    /// <summary>
    /// List of redo edit operations. So we can support Ctrl-Y, etc.
    /// </summary>
    readonly Stack<IEditOperation> _redoOperations = new();

    /// <summary>
    /// Extension for the saved project file.
    /// </summary>
    const string _projectFileExtension = ".alignproj";

    void Start()
    {
        nodeHierarchy.hierarchy.OnSelectionChanged += OnNodeSelectionChanged;
        nodeHierarchy.OnHighlightChanged += OnNodeHighlightChanged;
        poiHierarchy.hierarchy.OnSelectionChanged += OnPoiSelectionChanged;
        poiHierarchy.OnHighlightChanged += OnPoiHighlightChanged;
        Application.wantsToQuit += OnWantsToQuit;
        PanelNotificationCenter.OnTabClosed += OnTabClosed;

        nodeHierarchy.hierarchy.GameObjectFilter += OnFilterNodeHierarchy;
        poiHierarchy.hierarchy.GameObjectFilter += OnFilterPoiHierarchy;

        // Both hierarchies shouldn't be closeable, but the plugin doesn't allow to choose which tabs can be closed or not...
        // We'll cheat.
        nodeHierarchy
            .GetComponentInParent<Panel>()
            .GetTab((RectTransform)nodeHierarchy.GetComponentInParent<FocusablePanel>().transform)
            .transform.Find("TabContent/CloseButton").gameObject.SetActive(false);
        poiHierarchy
            .GetComponentInParent<Panel>()
            .GetTab((RectTransform)poiHierarchy.GetComponentInParent<FocusablePanel>().transform)
            .transform.Find("TabContent/CloseButton").gameObject.SetActive(false);

        solver.Running = false;
        if (solverToggle.isOn)
            solverToggle.isOn = false;
        saveProjectButton.interactable = false;
    }

    void Update()
    {
        GameObject curObj = EventSystem.current.currentSelectedGameObject;
        #pragma warning disable IDE0270
        // Null coalescing might not work because of Unity's annoying overload of the null operator. Just properly null the variable then.
        if (curObj == null)
            curObj = null;
        #pragma warning restore IDE0270
        InputField inputField = curObj?.GetComponent<InputField>();
        TMP_InputField inputFieldTMPro = curObj?.GetComponent<TMP_InputField>();
        bool eitherFocused = (inputField != null && inputField.isFocused) || (inputFieldTMPro != null && inputFieldTMPro.isFocused);
        if (!eitherFocused)
        {
            // We're not editing text, so support ctrl-z/y etc.
            // Note: if you're in the editor, make sure you disable Unity intercepting those events by clicking the appropriate button in the game view.
            bool ctrl = Input.GetKey(KeyCode.LeftControl);
            bool shift = Input.GetKey(KeyCode.LeftShift);
            bool z = Input.GetKeyDown(KeyCode.Z);
            bool y = Input.GetKeyDown(KeyCode.Y);
            if (ctrl && !shift && z)
                EditUndo();
            else if (ctrl && shift && z)
                EditRedo();
            else if (ctrl && !shift && y)
                EditRedo();
        }
        solverData.text = $"Average error: {solver.CurError}\nConvergence iteration: {solver.IterationNumber}";
        if (solver.Running && solver.Computing && _project != null)
        {
            _project.Unsaved = true;
            saveProjectButton.interactable = true;
        }
    }

    bool OnFilterNodeHierarchy(Transform transform) => transform.GetComponent<NodeRenderer>() != null;
    bool OnFilterPoiHierarchy(Transform transform) => transform.GetComponent<PointOfInterest>() != null;

    void OnTabClosed(PanelTab tab)
    {
        if (tab.Content.GetComponent<NodeViewPanel>() != null)
            tab.Destroy();
        else
            throw new NotSupportedException($"Not sure why we can close tab '{tab.Label}' ?");
    }

    bool OnWantsToQuit()
    {
        if (_project != null && _project.Unsaved)
        {
            HandleQuitting();
            return false;
        }
        return true;
    }

    async void HandleQuitting()
    {
        string result = await questionBox.Show("Save changes", "Your project has unsaved changes. Do you want to save ?", "Save", "Discard", "Cancel");
        if (result == "Save")
        {
            SaveProject();
            print("Saved, now quitting.");
            Application.Quit();
        }
        else if (result == "Discard")
        {
            CloseProject();
            print("Discarded, now quitting.");
            Application.Quit();
        }
    }

    void OnNodeSelectionChanged(ReadOnlyCollection<Transform> selection)
    {
        if (_project == null)
            return;
        List<NodeRenderer> activeNodes = selection.Select(n => n.GetComponent<NodeRenderer>()).ToList();
        foreach (NodeRenderer node in _project.Nodes)
            node.selectOutline.gameObject.SetActive(activeNodes.Contains(node));
    }

    void OnPoiSelectionChanged(ReadOnlyCollection<Transform> selection)
    {

    }

    void OnNodeHighlightChanged(ReadOnlyCollection<Transform> selection)
    {
        if (_project == null)
            return;
        List<NodeRenderer> activeNodes = selection.Select(n => n.GetComponent<NodeRenderer>()).ToList();
        foreach (NodeRenderer node in _project.Nodes)
            node.hoverOutline.gameObject.SetActive(activeNodes.Contains(node));
        _hoveredNodes = activeNodes;
    }

    void OnPoiHighlightChanged(ReadOnlyCollection<Transform> selection)
    {
        if (_project == null)
            return;
        _hoveredPois = selection.Select(poi => poi.GetComponent<PointOfInterest>()).ToList();
    }

    public void OnHierarchyItemRenamed(GameObject obj, string oldName, string newName)
    {
        if (obj.TryGetComponent(out NodeRenderer node))
            OnNodeRenamed?.Invoke(node, oldName, newName);
        else if (obj.TryGetComponent(out PointOfInterest poi))
            OnPoiRenamed?.Invoke(poi, oldName, newName);
    }

    #region Edit history

    public void EditDo(IEditOperation operation)
    {
        print($"Do: {operation}");
        _editOperations.Push(operation);
        operation.Do(this);
        _project.Unsaved = true;
        saveProjectButton.interactable = true;
    }

    public void EditUndo()
    {
        if (_editOperations.TryPeek(out IEditOperation peekOperation))
        {
            if (!peekOperation.CanUndo)
            {
                Debug.LogWarning($"Undoing operation {peekOperation} is not supported, sorry :/");
                return;
            }
        }
        if (_editOperations.TryPop(out IEditOperation operation))
        {
            print($"Undo: {operation}");
            _redoOperations.Push(operation);
            operation.Undo(this);
            _project.Unsaved = true;
            saveProjectButton.interactable = true;
        }
    }

    public void EditRedo()
    {
        if (_redoOperations.TryPop(out IEditOperation operation))
        {
            print($"Redo: {operation}");
            _editOperations.Push(operation);
            operation.Do(this);
            _project.Unsaved = true;
            saveProjectButton.interactable = true;
        }
    }

    #endregion

    #region UI callbacks

    /// <summary>
    /// Called by the UI when the "Import M3" button is clicked.
    /// </summary>
    public async void OnImportM3Clicked()
    {
        print("OnImportM3Clicked");

        using IDisposable uilock = LockUi();
        if (_project != null)
        {
            string answer = await questionBox.Show("Save project", "Save project before creating a new one ?", "Save", "Don't save", "Cancel");
            if (answer == "Save")
                SaveProject();
            else if (answer == "Cancel")
            {
                print("Import M3 cancelled.");
                return;
            }
            CloseProject();
        }

        Myst3GameDescription description;
        string m3path;
        // First find the M3 game folder.
        do
        {
            await WrapAsync(FileBrowser.WaitForLoadDialog(
                pickMode: FileBrowser.PickMode.Folders,
                allowMultiSelection: false,
                title: "Choose your Myst 3 folder",
                loadButtonText: "Load Myst 3"));

            if (!FileBrowser.Success)
            {
                print("Import M3 cancelled.");
                return;
            }

            m3path = FileBrowser.Result[0];
            print($"Import M3 - path: {m3path}");

            Myst3MetaEngineDetection gameDetection = new();
            description = gameDetection.Detect(m3path);
            bool isNull = description == null;
            bool isUnsupported = description.flags.HasFlag(ADGameFlags.ADGF_UNSUPPORTED);
            if (isNull || isUnsupported)
            {
                string detailedMessage =
                    isNull ? "Couldn't detect a valid Myst 3 installation from that path." :
                    isUnsupported ? "This version is marked as unsupported." :
                    "Somehow could not detect a valid Myst 3 installation from that path.";
                string answer = await questionBox.Show("Failed to detect game", detailedMessage, "Ok", "Cancel");
                if (answer == "Cancel")
                {
                    print("Import M3 cancelled.");
                    return;
                }
                description = null;
            }
        } while (description == null);

        print($"Description: {description.platform}, {description.language}, {description.flags}, {description.localizationType}");
        Myst3Loader loader = new(description, m3path);

        // Then pick an Age.
        Dictionary<string, uint> ageNameToId = new()
        {
            { "Tomahna", 3 },   // example (room, node): (301, 2)
            { "J'nanin", 5 },   // example (room, node): (501, 5)
            { "Edanna", 6 },    // example (room, node): (601, 1), or (602, 1)
            { "Amateria", 10 }, // example (room, node): (1001, 1)
            { "Voltaic", 7 },   // example (room, node): (701, 25)
            { "Narayan", 8 }    // example (room, node): (801, 1)
        };
        string age = await questionBox.Show("Age", "Which Age would you like to load ?", ageNameToId.Keys.ToArray());
        loader.LoadAge(ageNameToId[age]);

        // Select where to save the project
        string projectPath;
        FileBrowser.SetFilters(false, new FileBrowser.Filter("Alignment project", _projectFileExtension));
        FileBrowser.SetDefaultFilter(_projectFileExtension);
        await WrapAsync(FileBrowser.WaitForSaveDialog(
            pickMode: FileBrowser.PickMode.Files,
            allowMultiSelection: false,
            title: "Choose where to save the project",
            saveButtonText: $"Save {_projectFileExtension}"));
        if (!FileBrowser.Success)
        {
            print("Import M3 cancelled.");
            // Ensure opened files are all released.
            // TODO: eventually replace the whole scummvm code with a simpler script.
            loader.Shutdown();
            loader = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return;
        }
        projectPath = FileBrowser.Result[0];
        
        if (!FileBrowser.Success)
        {
            print("Import M3 cancelled.");
            // Ensure opened files are all released.
            // TODO: eventually replace the whole scummvm code with a simpler script.
            loader.Shutdown();
            loader = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return;
        }

        print($"Import M3 - project path: {projectPath}");

        string projectName = Path.GetFileNameWithoutExtension(projectPath);
        string projectDirectory = Path.GetDirectoryName(projectPath);
        string texturesPath = Path.Combine(projectDirectory, $"{projectName}_textures");
        Directory.CreateDirectory(texturesPath);

        // Now extract all JPEGs and create cubes.
        List<NodeRenderer> nodeRenderers = new();
        foreach (Myst3Loader.M3Cube cube in loader.Cubes)
        {
            FaceIndex faceIndex = 0;
            string nodeName = $"{cube.roomName}_{cube.nodeID}";
            if (cube.faces.Any(f => f == null))
            {
                Debug.LogError($"Face is null: {nodeName}");
                break;
            }
            NodeRenderer nodeRenderer = Instantiate(nodePrefab, nodesParent);
            nodeRenderer.name = nodeRenderer.id = nodeName;
            nodeRenderer.transform.localPosition = Vector3.forward * nodeRenderers.Count;
            nodeRenderer.texturePaths = new string[6];
            nodeRenderers.Add(nodeRenderer);
            foreach (MemoryStream jpegStream in cube.faces)
            {
                string faceName = nodeName + $"_{faceIndex}.jpg";
                string fileName = Path.Combine(texturesPath, faceName);
                using FileStream file = new(fileName, FileMode.Create, FileAccess.Write);
                jpegStream.CopyTo(file);

                Texture2D tex = new(4, 4);
                tex.LoadImage(jpegStream.ToArray());
                tex.name = faceName;
                tex.wrapMode = TextureWrapMode.Clamp;
                _curTextures.Add(tex);
                nodeRenderer.setTexture(faceIndex, tex);
                nodeRenderer.texturePaths[(int)faceIndex] = faceName;
                
                faceIndex++;
            }
        }

        // And setup our project.
        _project = new()
        {
            Name = Path.GetFileNameWithoutExtension(projectPath),
            Nodes = nodeRenderers,
            Location = Path.GetDirectoryName(projectPath),
        };
        solver.AddAllPoiOnNode(_project.PoiOnNodes);

        // Ensure opened files are all released.
        // TODO: eventually replace the whole scummvm code with a simpler script.
        loader.Shutdown();
        loader = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();

        SaveProject();
    }

    /// <summary>
    /// Called by the UI when the "Load" button is clicked.
    /// </summary>
    public async void OnOpenProjectClicked()
    {
        print("OnOpenProjectClicked");

        using IDisposable uilock = LockUi();
        if (_project != null)
        {
            string answer = await questionBox.Show("Save project", "Save project before creating a new one ?", "Save", "Don't save", "Cancel");
            if (answer == "Save")
                SaveProject();
            else if (answer == "Cancel")
            {
                print("Open project cancelled.");
                return;
            }
            CloseProject();
        }

        FileBrowser.SetFilters(false, new FileBrowser.Filter("Alignment project", _projectFileExtension));
        FileBrowser.SetDefaultFilter(_projectFileExtension);
        await WrapAsync(FileBrowser.WaitForLoadDialog(
            pickMode: FileBrowser.PickMode.Files,
            allowMultiSelection: false,
            title: "Open project",
            loadButtonText: $"Load {_projectFileExtension}"));

        if (!FileBrowser.Success)
        {
            print("Open project cancelled.");
            return;
        }
        string projectPath = FileBrowser.Result[0];
        print($"Open project - path: {projectPath}");

        OpenProject(projectPath);
    }

    /// <summary>
    /// Called by the UI when the "Save" button is clicked.
    /// </summary>
    public void OnSaveProjectClicked()
    {
        print("OnSaveProjectClicked");
        SaveProject();
    }

    public void OnAddNew3dView()
    {
        print("OnAddNew3dView");
        NodeViewPanel viewPanel = Instantiate(threedeeViewPrefab, panelsCanvas.transform);
        Panel panel = PanelUtils.CreatePanelFor((RectTransform)viewPanel.transform, panelsCanvas);
        PanelTab tab = panel.GetTab((RectTransform)viewPanel.transform);
        tab.Icon = null;
        tab.Label = "3D view";
        viewPanel.mainHandler = this;
        panel.ResizeTo(new Vector2(250, 250));
        panel.MoveTo(panelsCanvas.transform.position);
    }

    public void OnSolverToggleChanged(bool on)
    {
        print($"OnSolverToggleChanged: {on}");
        solver.Running = on;
    }

    #endregion

    #region Project save/load logic

    /// <summary>
    /// Destroy all nodes, their textures, and points of interest.
    /// </summary>
    void CloseProject()
    {
        solver.Running = false;
        solver.ClearData();

        ActiveNode = null;
        ActivePoi = null;
        foreach (NodeViewPanel nodeView in panelsCanvas.GetComponentsInChildren<NodeViewPanel>())
            nodeView.Node = null;
        foreach (PoiOnNode pon in _project.PoiOnNodes)
            Destroy(pon.gameObject);
        foreach (PointOfInterest poi in _project.PointsOfInterest)
            Destroy(poi.gameObject);
        foreach (NodeRenderer node in _project.Nodes)
            Destroy(node.gameObject);
        foreach (Texture2D texture in _curTextures)
            Destroy(texture);
        _project = null;
        nodeHierarchy.hierarchy.Refresh();
        poiHierarchy.hierarchy.Refresh();
        _editOperations.Clear();
        _redoOperations.Clear();
    }

    /// <summary>
    /// Load saved project, including nodes, their textures and points of interest.
    /// </summary>
    /// <param name="projectPath">Path to the saved project file.</param>
    void OpenProject(string projectPath)
    {
        SerialProject serProj = JsonUtility.FromJson<SerialProject>(File.ReadAllText(projectPath));
        string texturesDirectory = Path.Combine(Path.GetDirectoryName(projectPath), Path.GetFileNameWithoutExtension(projectPath) + "_textures");
        List<NodeRenderer> nodes = new();
        List<PointOfInterest> pois = new();
        List<PoiOnNode> pons = new();
        foreach (SerialCube serCube in serProj.cubes)
        {
            NodeRenderer nodeRenderer = Instantiate(nodePrefab, nodesParent);
            nodeRenderer.id = serCube.id;
            nodeRenderer.name = serCube.name;
            nodeRenderer.transform.localPosition = serCube.position;
            nodeRenderer.texturePaths = new string[6];
            nodes.Add(nodeRenderer);
            FaceIndex faceIndex = 0;
            foreach (string faceName in serCube.faceTextureFiles)
            {
                Texture2D tex = new(4, 4);
                tex.LoadImage(File.ReadAllBytes(Path.Combine(texturesDirectory, faceName)));
                tex.name = faceName;
                tex.wrapMode = TextureWrapMode.Clamp;
                _curTextures.Add(tex);
                nodeRenderer.setTexture(faceIndex, tex);
                nodeRenderer.texturePaths[(int)faceIndex] = faceName;
                faceIndex++;
            }
        }
        foreach (SerialPointOfInterest serPoi in serProj.pointsOfInterest)
        {
            PointOfInterest point = Instantiate(poiPrefab, pointsOfInterestParent);
            point.Id = serPoi.id;
            point.name = serPoi.name;
            point.transform.position = serPoi.position;
            point.positionInitialized = serPoi.positionInitialized;
            pois.Add(point);
        }
        foreach (SerialPoiOnNodes serPon in serProj.poiOnNodes)
        {
            NodeRenderer node = nodes.First(n => n.id == serPon.nodeId);
            PointOfInterest point = pois[serPon.poiId];
            PoiOnNode pointOnNode = Instantiate(poiOnNodePrefab, node.poiOnNodesParent);
            pointOnNode.Direction = serPon.direction;
            pointOnNode.Node = node;
            pointOnNode.Point = point;
            point.linkedNodes.Add(pointOnNode);
            node.pointsOfInterest.Add(pointOnNode);
            pons.Add(pointOnNode);
        }
        if (pois.Select(poi => poi.Id).Distinct().Count() != pois.Count)
            throw new InvalidDataException("Points of interest are not distinct !");
        _project = new Project()
        {
            Name = Path.GetFileNameWithoutExtension(projectPath),
            Location = Path.GetDirectoryName(projectPath),
            Nodes = nodes,
            PointsOfInterest = pois,
            PoiOnNodes = pons,
            Unsaved = false,
            NextPoiId = (pois.Count > 0) ? pois.Max(poi => poi.Id) + 1 : 1
        };
        saveProjectButton.interactable = false;
        solver.AddAllPoiOnNode(_project.PoiOnNodes);
    }

    /// <summary>
    /// Save the currently open project.
    /// </summary>
    void SaveProject()
    {
        string projectFilePath = Path.Combine(_project.Location, _project.Name + _projectFileExtension);
        print($"Saving project to {projectFilePath}");

        using IDisposable uiLock = LockUi();

        SerialProject sProj = new()
        {
            cubes = _project.Nodes.Select(n => new SerialCube()
            {
                id = n.id,
                name = n.name,
                position = n.transform.localPosition,
                faceTextureFiles = n.texturePaths
            }).ToList(),
            pointsOfInterest = _project.PointsOfInterest.Select(poi => new SerialPointOfInterest()
            {
                id = poi.Id,
                name = poi.name,
                position = poi.transform.position,
                positionInitialized = poi.positionInitialized
            }).ToList(),
            poiOnNodes = _project.PoiOnNodes.Select(pon => new SerialPoiOnNodes()
            {
                nodeId = pon.Node.id,
                poiId = _project.PointsOfInterest.IndexOf(pon.Point),
                direction = pon.Direction
            }).ToList()
        };

        File.WriteAllText(projectFilePath, JsonUtility.ToJson(sProj));

        _project.Unsaved = false;
        saveProjectButton.interactable = false;
    }

    #endregion

    void OnDestroy()
    {
        Application.wantsToQuit -= OnWantsToQuit;
        if (nodeHierarchy.hierarchy != null)
            nodeHierarchy.hierarchy.OnSelectionChanged -= OnNodeSelectionChanged;
        if (poiHierarchy.hierarchy != null)
            poiHierarchy.hierarchy.OnSelectionChanged -= OnPoiSelectionChanged;
        foreach (Texture2D tex in _curTextures)
            Destroy(tex);
    }

    #region Utility

    /// <summary>
    /// Used to keep track of UI locking when pop-up dialogs are open.
    /// </summary>
    class UiLock : IDisposable
    {
        readonly MainHandler _parent;
        public UiLock(MainHandler parent) => _parent = parent;
        public void Dispose() => _parent.PopUiLock();
    }

    int uiLocks = 0;

    /// <summary>
    /// Returns a disposable object which ensure the main UI is locked while pop-up dialogs are open.
    /// </summary>
    IDisposable LockUi()
    {
        uiLocks++;
        mainUiGroup.interactable = false;
        return new UiLock(this);
    }

    /// <summary>
    /// Removes one lock from the UI locking mechanism, unlocking the UI if necessary.
    /// </summary>
    void PopUiLock()
    {
        uiLocks--;
        if (uiLocks == 0)
            mainUiGroup.interactable = true;
    }

    /// <summary>
    /// Wraps a coroutine into an awaitable task, for convenience.
    /// </summary>
    /// <param name="coroutine">The coroutine to wait on.</param>
    /// <returns>An awaitable task. Will complete after the coroutine ends.</returns>
    Task WrapAsync(IEnumerator coroutine)
    {
        TaskCompletionSource<object> tcs = new();
        StartCoroutine(WaitCoroutine(coroutine, tcs));
        return tcs.Task;
    }

    /// <summary>
    /// Coroutine that waits on another, completing a task when it's done.
    /// </summary>
    /// <param name="coroutine">The coroutine to wait on.</param>
    /// <param name="tcs">The task source that will complete after the coroutine.</param>
    /// <returns>A coroutine's enumerator.</returns>
    IEnumerator WaitCoroutine(IEnumerator coroutine, TaskCompletionSource<object> tcs)
    {
        yield return StartCoroutine(coroutine);
        tcs.SetResult(null);
    }

    public PoiOnNode CreatePointOfInterest(NodeRenderer node, Vector3 direction)
    {
        PointOfInterest point = Instantiate(poiPrefab, pointsOfInterestParent);
        PoiOnNode pointOnNode = Instantiate(poiOnNodePrefab, node.poiOnNodesParent);
        point.Id = _project.NextPoiId++;
        pointOnNode.Direction = direction;
        pointOnNode.Node = node;
        pointOnNode.Point = point;
        point.linkedNodes.Add(pointOnNode);
        node.pointsOfInterest.Add(pointOnNode);
        point.name = pointsOfInterestParent.childCount.ToString();
        _project.PointsOfInterest.Add(point);
        _project.PoiOnNodes.Add(pointOnNode);
        solver.AddOrUpdatePoiOnNode(pointOnNode);
        OnPoiLinkedToNode?.Invoke(pointOnNode);
        return pointOnNode;
    }

    public PoiOnNode LinkPointOfInterest(PointOfInterest point, NodeRenderer node, Vector3 direction)
    {
        PoiOnNode pointOnNode = Instantiate(poiOnNodePrefab, node.poiOnNodesParent);
        pointOnNode.Direction = direction;
        pointOnNode.Node = node;
        pointOnNode.Point = point;
        point.linkedNodes.Add(pointOnNode);
        node.pointsOfInterest.Add(pointOnNode);
        _project.PoiOnNodes.Add(pointOnNode);
        solver.AddOrUpdatePoiOnNode(pointOnNode);
        OnPoiLinkedToNode?.Invoke(pointOnNode);
        return pointOnNode;
    }

    public void UnlinkPointOfInterest(PoiOnNode pointOnNode)
    {
        OnPoiUnlinkedFromNode?.Invoke(pointOnNode);
        PointOfInterest point = pointOnNode.Point;
        pointOnNode.Node.pointsOfInterest.Remove(pointOnNode);
        point.linkedNodes.Remove(pointOnNode);
        solver.RemovePoiOnNode(pointOnNode);
        _project.PoiOnNodes.Remove(pointOnNode);
        if (point.linkedNodes.IsEmpty())
        {
            if (ActivePoi == point)
                ActivePoi = null;
            _project.PointsOfInterest.Remove(point);
            Destroy(point.gameObject);
        }
        Destroy(pointOnNode.gameObject);
    }

    public void DeletePointOfInterest(PointOfInterest poi)
    {
        if (ActivePoi == poi)
            ActivePoi = null;
        solver.RemovePointOfInterest(poi);
        foreach (PoiOnNode pon in poi.linkedNodes)
        {
            OnPoiUnlinkedFromNode?.Invoke(pon);
            _project.PoiOnNodes.Remove(pon);
            pon.Node.pointsOfInterest.Remove(pon);
            Destroy(pon.gameObject);
        }
        _project.PointsOfInterest.Remove(poi);
        Destroy(poi.gameObject);
    }

    public void MovePointOnNode(PoiOnNode pointOnNode, Vector3 oldDirection, Vector3 newDirection)
    {
        pointOnNode.Direction = newDirection;
        solver.AddOrUpdatePoiOnNode(pointOnNode);
        OnPonMoved?.Invoke(pointOnNode, oldDirection, newDirection);
    }

    public void TryDelete(Transform active)
    {
        if (active.TryGetComponent(out PointOfInterest poi))
            EditDo(new DeletePointOfInterestOperation(poi));
    }

    #endregion
}
