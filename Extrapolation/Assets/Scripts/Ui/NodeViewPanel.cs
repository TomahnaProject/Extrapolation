using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DynamicPanels;
using RuntimeInspectorNamespace;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

/// <summary>
/// Handles a 3D view / node view and associated input.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(FocusablePanel))]
public class NodeViewPanel : MonoBehaviour,
    IBeginDragHandler, IDragHandler, /* IEndDragHandler,*/
    IPointerClickHandler, IPointerMoveHandler,
    IPointerEnterHandler, IPointerExitHandler,
    IScrollHandler,
    IDropHandler
{
    public MainHandler mainHandler;
    [Tooltip("Where to instantiate the POI gizmos.")]
    public Transform poiInstantiateParent;

    public PanelTab Tab => GetComponentInParent<Panel>().GetTab((RectTransform)transform);

    [Tooltip("Culling mask for the render cam.")]
    public LayerMask cullMask;
    public float viewNoNodeDragSensitivity = 2;
    public float viewMoveSensitivity = 5;
    [Tooltip("Shows the rendered 3D view.")]
    public RawImage threeDeeView;

    [Tooltip("Color for displayed points of interest lines.")]
    public Color poiLineColor = Color.cyan;

    [Tooltip("Color for PoI lines that have a low enough angular error.")]
    public Color poiLineColorMatching = Color.green;
    [Tooltip("Color for PoI lines that have a high angular error.")]
    public Color poiLineColorNonMatching = Color.red;

    public Camera RenderCam => _renderCam;

    /// <summary>
    /// Current node which we're viewing from, if any.
    /// Changing it will change the <see cref="_renderCam"/>'s position.
    /// </summary>
    public NodeRenderer Node
    {
        get => _node;
        set
        {
            if (_renderCam != null)
            {
                if (value != null)
                {
                    // Set the new node, and move camera to it.
                    Vector3 startOffset = _renderCam.transform.position - value.transform.position;
                    SetCamReferenceNode(value);
                    if (_camAnimCoroutine != null)
                        StopCoroutine(_camAnimCoroutine);
                    _camAnimCoroutine = StartCoroutine(AnimateCamPos(
                        startOffset,
                        Vector3.zero,
                        resetFov: _node == null));
                    _renderCam.transform.position = value.transform.position;
                    if (Tab != null)
                        Tab.Label = value.name;
                    ClearUiPois();
                }
                else if (_node != null)
                {
                    // Unset node, and move camera away from the old node.
                    Vector3 startPos = _renderCam.transform.position;
                    SetCamReferenceNode(null);
                    if (_camAnimCoroutine != null)
                        StopCoroutine(_camAnimCoroutine);
                    _camAnimCoroutine = StartCoroutine(AnimateCamPos(
                        startPos,
                        _node.transform.position - _renderCam.transform.forward * 0.5f,
                        resetFov: true));
                    if (Tab != null)
                        Tab.Label = "3D view";
                    ClearUiPois();
                }
                if (value != null)
                    _renderCam.cullingMask &= ~(1 << LayerMask.NameToLayer("Nodes"));
                else
                    _renderCam.cullingMask |= 1 << LayerMask.NameToLayer("Nodes");
            }
            _node = value;
            if (_node != null)
                LoadUiPois();
        }
    }

    /// <summary>
    /// Camera we render from.
    /// </summary>
    Camera _renderCam;
    PoiRenderCam _poiCam;
    ParentConstraint _camParentConstraint;

    /// <summary>
    /// Texture the camera renders to.
    /// </summary>
    RenderTexture _renderTex;

    /// <summary>
    /// Current node which we're viewing from, if any.
    /// </summary>
    NodeRenderer _node;

    /// <summary>
    /// Our own transform.
    /// </summary>
    RectTransform _rectTransform;

    FocusablePanel _focus;

    Vector3 _lastMousePosition;

    float _camHeading, _camPitch;

    Coroutine _camAnimCoroutine;

    /// <summary>
    /// Multiplier for sensitivity when moving in the viewport. This is user-controlled via the scroll wheel, when not in node view.
    /// </summary>
    float _viewportCustomMoveSensitivity = 1;

    readonly Dictionary<PoiOnNode, UiPointOfInterest> _uiPointsOfInterest = new();

    public const int mouseDragViewButton = 1;
    public const int mouseSelectButton = 0;

    void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        int rectWidth = Mathf.RoundToInt(_rectTransform.rect.width);
        int rectHeight = Mathf.RoundToInt(_rectTransform.rect.height);
        _renderTex = new RenderTexture(rectWidth, rectHeight, 0, DefaultFormat.LDR);
        _renderCam = new GameObject(name + "_camera").AddComponent<Camera>();
        _renderCam.fieldOfView = 80;
        _renderCam.nearClipPlane = 0.001f;
        _renderCam.farClipPlane = 1000;
        _renderCam.backgroundColor = Color.grey;
        _renderCam.aspect = rectWidth / (float)rectHeight;
        _renderCam.cullingMask = cullMask;
        // Add a parent constraint. The camera will follow whichever node it's assigned to, even if said node is moved by the position solver.
        _camParentConstraint = _renderCam.gameObject.AddComponent<ParentConstraint>();
        _camParentConstraint.rotationAxis = Axis.None;
        _poiCam = _renderCam.gameObject.AddComponent<PoiRenderCam>();
        _poiCam.OnCameraRender += OnCameraRender;
        threeDeeView.texture = _renderCam.targetTexture = _renderTex;
        _focus = GetComponent<FocusablePanel>();

        if (_node != null)
        {
            _renderCam.transform.position = _node.transform.position;
            SetCamReferenceNode(_node);
            _camParentConstraint.SetTranslationOffset(0, Vector3.zero);
            _renderCam.transform.rotation = Quaternion.identity;
        }
        else
        {
            _renderCam.transform.position = Vector3.one * 5;
            _renderCam.transform.LookAt(Vector3.zero);
        }

        mainHandler.OnPoiSelectionChanged += OnPoiSelectionChanged;
        mainHandler.OnNodeRenamed += OnNodeRenamed;
        mainHandler.OnPoiLinkedToNode += OnPoiLinkedToNode;
        mainHandler.OnPoiRenamed += OnPoiRenamed;
        mainHandler.OnPoiUnlinkedFromNode += OnPoiUnlinkedFromNode;
        mainHandler.OnPonMoved += OnPonMoved;
    }

    void Update()
    {
        int rectWidth = Mathf.RoundToInt(_rectTransform.rect.width);
        int rectHeight = Mathf.RoundToInt(_rectTransform.rect.height);
        if (_renderTex.width != rectWidth || _renderTex.height != rectHeight)
        {
            _renderTex.Release();
            _renderTex.width = rectWidth;
            _renderTex.height = rectHeight;
            _renderCam.aspect = rectWidth / (float)rectHeight;
            _renderTex.Create();
            UpdatePoiPositions();
        }

        if (_focus.Active && !mainHandler.CurrentlyEditingTextField)
            ProcessInput();
    }

    public bool ProcessInput()
    {
        Vector3 movementInput = new(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"),
            Input.GetAxisRaw("UpDown")
        );
        movementInput *= _viewportCustomMoveSensitivity;
        if (_node == null)
        {
            float sensitivity = viewMoveSensitivity;
            if (Input.GetKey(KeyCode.LeftShift))
                sensitivity *= 5;
            else if (Input.GetKey(KeyCode.LeftAlt))
                sensitivity /= 5;
            _renderCam.transform.position +=
                (_renderCam.transform.right * movementInput.x +
                _renderCam.transform.forward * movementInput.y +
                Vector3.up * movementInput.z)
                * Time.deltaTime * sensitivity;
        }
        else
        {
            if (!Input.GetKey(KeyCode.LeftControl) && (
                Input.GetKeyDown(KeyCode.D) ||
                Input.GetKeyDown(KeyCode.S) ||
                Input.GetKeyDown(KeyCode.RightArrow) ||
                Input.GetKeyDown(KeyCode.DownArrow)
                ))
            {
                List<NodeRenderer> nodes = GetNodesList();
                int index = nodes.IndexOf(Node);
                if (index + 1 < nodes.Count)
                    Node = nodes[index + 1];
            }

            else if (!Input.GetKey(KeyCode.LeftControl) && (
                Input.GetKeyDown(KeyCode.A) ||
                Input.GetKeyDown(KeyCode.W) ||
                Input.GetKeyDown(KeyCode.LeftArrow) ||
                Input.GetKeyDown(KeyCode.UpArrow)
                ))
            {
                List<NodeRenderer> nodes = GetNodesList();
                int index = nodes.IndexOf(Node);
                if (index > 0)
                    Node = nodes[index - 1];
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            NodeRenderer activeNode = mainHandler.ActiveNode;
            if (_node == null)
                Node = activeNode;
            else if (activeNode != null && activeNode != _node)
                Node = activeNode;
            else
                Node = null;
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            PointOfInterest curPoi = mainHandler.ActivePoi;
            if (_node != null && curPoi != null)
            {
                PoiOnNode pon = curPoi.linkedNodes.FirstOrDefault(pon => pon.Node == _node);
                if (pon != null)
                    mainHandler.EditDo(new UnlinkPointOfInterestOperation(pon));
            }
        }

        return false;
    }

    private void LoadUiPois()
    {
        foreach (PoiOnNode pon in _node.pointsOfInterest)
            LoadPoi(pon);
    }

    private void ClearUiPois()
    {
        foreach (UiPointOfInterest uiPoi in _uiPointsOfInterest.Values)
            Destroy(uiPoi.gameObject);
        _uiPointsOfInterest.Clear();
    }

    void SetCamReferenceNode(NodeRenderer node)
    {
        if (node != null)
        {
            _camParentConstraint.SetSources(new List<ConstraintSource>()
            {
                new()
                {
                    sourceTransform = node.transform,
                    weight = 1
                }
            });
            _camParentConstraint.constraintActive = true;
        }
        else
        {
            _camParentConstraint.SetSources(new List<ConstraintSource>());
            _camParentConstraint.constraintActive = false;
        }
    }

    /// <summary>
    /// Animates the render camera from one position to the other.
    /// </summary>
    /// <param name="start">Start position if the node constraint is inactive, start offset otherwise.</param>
    /// <param name="end">End position if the node constraint is inactive, end offset otherwise.</param>
    /// <param name="resetFov">Whether to reset FOV or preserve it.</param>
    /// <returns>Iterator for the anim.</returns>
    IEnumerator AnimateCamPos(Vector3 start, Vector3 end, bool resetFov)
    {
        float begin = Time.time;
        float duration = 0.2f;
        float progress;
        float beginFov = _renderCam.fieldOfView;
        do
        {
            progress = Mathf.Min(Time.time - begin, duration);
            float factor = Mathf.SmoothStep(0, 1, progress / duration);
            Vector3 pos = Vector3.Lerp(start, end, factor);
            if (_camParentConstraint.constraintActive)
                _camParentConstraint.SetTranslationOffset(0, pos);
            else
                _renderCam.transform.position = pos;
            if (resetFov)
                _renderCam.fieldOfView = Mathf.Lerp(beginFov, 80, factor);
            yield return null;
        } while (progress < duration);
        _camAnimCoroutine = null;
    }

    /// <summary>
    /// Converts pointer coordinates from app space (in pixel for the full UI) to camera viewport space.
    /// </summary>
    /// <param name="pointer">Source pointer position.</param>
    /// <returns>Pointer position in the viewport, (0 to 1) on both axis.</returns>
    Vector3 PointerPosToViewport01Pos(Vector3 pointer)
    {
        Vector3[] corners = new Vector3[4];
        _rectTransform.GetWorldCorners(corners);
        float posX = Mathf.InverseLerp(corners[0].x, corners[2].x, pointer[0]);
        float posY = Mathf.InverseLerp(corners[0].y, corners[2].y, pointer[1]);
        return new Vector3(posX, posY, 0);
    }

    /// <summary>
    /// Converts pointer coordinates from app space (in pixel for the full UI) to camera viewport space, centered.
    /// </summary>
    /// <param name="pointer">Source pointer position.</param>
    /// <returns>Pointer position in the viewport, (-1 to 1) on Y axis, keeping aspect ratio for X axis.</returns>
    Vector3 PointerPosToViewportCenteredPos(Vector3 pointer)
    {
        Vector3[] corners = new Vector3[4];
        _rectTransform.GetWorldCorners(corners);
        float width = corners[2].x - corners[0].x;
        float height = corners[2].y - corners[0].y;
        float ratio = width / height;
        static float InverseLerpUnclamped(float a, float b, float value) => (value - a) / (b - a);
        float posX = InverseLerpUnclamped(corners[0].x, corners[2].x, pointer[0]) * 2 - 1;
        float posY = InverseLerpUnclamped(corners[0].y, corners[2].y, pointer[1]) * 2 - 1;
        posX *= ratio;
        return new Vector3(posX, posY, 0);
    }

    void UpdatePoiPositions()
    {
        foreach (var kvp in _uiPointsOfInterest)
        {
            PoiOnNode pon = kvp.Key;
            UiPointOfInterest uiPoi = kvp.Value;
            SetUiElementPosFromDirection(pon.Direction, uiPoi.transform);
        }
    }

    void LoadPoi(PoiOnNode pon)
    {
        UiPointOfInterest uiPoi = Instantiate(mainHandler.uiPoiPrefab, poiInstantiateParent);
        uiPoi.pointOnNode = pon;
        uiPoi.parent = this;
        _uiPointsOfInterest[pon] = uiPoi;
        uiPoi.label.text = pon.Point.name;
        SetUiElementPosFromDirection(pon.Direction, uiPoi.transform);
    }

    void SetUiElementPosFromDirection(Vector3 direction, Transform uiElement)
    {
        if (Vector3.Dot(direction, _renderCam.transform.forward) < 0)
        {
            uiElement.gameObject.SetActive(false);
        }
        else
        {
            uiElement.gameObject.SetActive(true);
            uiElement.localPosition = _renderCam.WorldToScreenPoint(_node.transform.position + direction);
        }
    }

    List<NodeRenderer> GetNodesList()
    {
        List<NodeRenderer> nodes = new();
        for (int i = 0; i < mainHandler.nodesParent.childCount; i++)
        {
            NodeRenderer component = mainHandler.nodesParent.GetChild(i).GetComponent<NodeRenderer>();
            if (component != null)
                nodes.Add(component);
        }
        return nodes;
    }

    void OnDestroy()
    {
        if (_renderCam != null)
        {
            _renderCam.targetTexture = null;
            Destroy(_renderCam.gameObject);
        }
        if (threeDeeView != null)
            threeDeeView.texture = null;
        _renderTex.Release();
        Destroy(_renderTex);
        if (mainHandler != null)
        {
            mainHandler.OnPoiSelectionChanged -= OnPoiSelectionChanged;
            mainHandler.OnNodeRenamed -= OnNodeRenamed;
            mainHandler.OnPoiLinkedToNode -= OnPoiLinkedToNode;
            mainHandler.OnPoiRenamed -= OnPoiRenamed;
            mainHandler.OnPoiUnlinkedFromNode -= OnPoiUnlinkedFromNode;
            mainHandler.OnPonMoved -= OnPonMoved;
        }
    }

    #region Main handler events

    void OnPoiSelectionChanged(IReadOnlyList<PointOfInterest> list)
    {
        foreach (var kvp in _uiPointsOfInterest)
            kvp.Value.Selected = list.Contains(kvp.Key.Point);
    }

    void OnNodeRenamed(NodeRenderer node, string oldName, string newName)
    {
        if (node == _node)
            Tab.Label = newName;
    }

    void OnPoiLinkedToNode(PoiOnNode pon)
    {
        if (pon.Node == _node)
            LoadPoi(pon);
    }

    void OnPoiUnlinkedFromNode(PoiOnNode pon)
    {
        if (_uiPointsOfInterest.TryGetValue(pon, out UiPointOfInterest uiPoi))
        {
            Destroy(uiPoi.gameObject);
            _uiPointsOfInterest.Remove(pon);
        }
    }

    void OnPonMoved(PoiOnNode pon, Vector3 oldDirection, Vector3 newDirection)
    {
        if (_uiPointsOfInterest.TryGetValue(pon, out UiPointOfInterest uiPoi))
            SetUiElementPosFromDirection(pon.Direction, uiPoi.transform);
    }

    void OnPoiRenamed(PointOfInterest poi, string oldName, string newName)
    {
        PoiOnNode pon = _uiPointsOfInterest.Keys.FirstOrDefault(pon => pon.Point == poi);
        if (pon != null)
            _uiPointsOfInterest[pon].label.text = newName;
    }

    public void RecenterCamera()
    {
        var allPositions = GetNodesList().Select(n => n.transform.position).ToList();
        if (allPositions.Count == 0)
            return;
        Bounds b = new(allPositions[0], Vector3.zero);
        foreach (Vector3 pos in allPositions.Skip(1))
            b.Encapsulate(pos);
        Vector3 camDirection = new(1, 0.75f, -1);
        _renderCam.transform.position = b.center + camDirection * b.extents.magnitude;
        _renderCam.transform.LookAt(b.center);
    }

    #endregion

    #region UI events

    void OnCameraRender()
    {
        if (_node != null)
        {
            MaterialPropertyBlock block = new();
            for (int i = 0; i < 6; i++)
            {
                _node.sphere.GetPropertyBlock(block, i);
                _node.sphere.sharedMaterials[i].SetTexture("_MainTex", block.GetTexture("_MainTex"));
                _node.sphere.sharedMaterials[i].SetPass(0);
                Graphics.DrawMeshNow(_node.sphere.GetComponent<MeshFilter>().sharedMesh, _node.sphere.transform.localToWorldMatrix, i);
            }
        }
        _poiCam.LineMaterial.SetPass(0);

        bool showAllRelations = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        PointOfInterest activePoi = mainHandler.ActivePoi;
        HashSet<PoiOnNode> linesToDraw = new();
        if (showAllRelations)
        {
            foreach (PoiOnNode pon in mainHandler.AllPon)
            {
                if (_node == null || pon.Node != _node)
                    linesToDraw.Add(pon);
            }
        }
        else
        {
            if (_node == null)
            {
                NodeRenderer activeNode = mainHandler.ActiveNode;
                if (activeNode != null)
                {
                    // We are not in node view, but a node is selected. Draw the direction of its associated PoIs.
                    foreach (PoiOnNode pon in activeNode.pointsOfInterest)
                        linesToDraw.Add(pon);
                }
            }
            if (activePoi != null)
            {
                // A PoI is selected. Draw all its relations (except those linked to the node we may be focusing).
                foreach (PoiOnNode pon in activePoi.linkedNodes)
                {
                    if (_node != pon.Node)
                        linesToDraw.Add(pon);
                }
            }
        }

        GL.Begin(GL.LINES);
        Color poiLineDeactivatedColor = poiLineColor;
        Color poiLineDimColor = poiLineColor;
        poiLineDeactivatedColor.a = 0;
        poiLineDimColor.a = 0.5f;
        foreach (PoiOnNode pon in linesToDraw)
        {
            NodeRenderer node = pon.Node;
            if (pon.Point.positionInitialized)
            {
                // Solver has a position for that point - draw the line from the node to the point itself.
                Vector3 originalDirection = pon.Direction;
                Vector3 solvedDirection = pon.Point.transform.position - node.transform.position;
                float angle = Vector3.Angle(originalDirection, solvedDirection);
                Color poiLineSolvedColor = (angle < MainHandler.poiAngularErrorMarginDegrees) ? poiLineColorMatching : poiLineColorNonMatching;
                GL.Color(poiLineSolvedColor);
                GL.Vertex(node.transform.position);
                GL.Vertex(pon.Point.transform.position);

                // Also draw the original direction vector for comparison.
                float length = solvedDirection.magnitude;
                GL.Color(poiLineDimColor);
                GL.Vertex(node.transform.position);
                GL.Color(poiLineDeactivatedColor);
                GL.Vertex(node.transform.position + originalDirection * length);
            }
            else
            {
                GL.Color(Color.cyan);
                GL.Vertex(node.transform.position);
                GL.Color(poiLineDeactivatedColor);
                GL.Vertex(node.transform.position + pon.Direction * 2);
            }
        }
        GL.End();

        // Draw orientation gizmo.
        GL.PushMatrix();

        int width = _renderTex.width;
        int height = _renderTex.height;
        int gizmoRenderHeight = 150;
        int gizmoRenderWidth = gizmoRenderHeight;
        GL.Viewport(new Rect(width - gizmoRenderWidth, height - gizmoRenderHeight, gizmoRenderWidth, gizmoRenderHeight));
        GL.LoadProjectionMatrix(Matrix4x4.Perspective(45, gizmoRenderWidth / (float)gizmoRenderHeight, 0.1f, 10f));
        Matrix4x4 modelView = Matrix4x4.Translate(new Vector3(0, 0, -3)) * Matrix4x4.Rotate(Quaternion.Inverse(_renderCam.transform.rotation));
        // Need to negate third row of the matrix... Go figure. Maybe something with right vs left handed coordinates, and gl/dx...
        modelView.m20 *= -1;
        modelView.m21 *= -1;
        modelView.m22 *= -1;
        GL.modelview = modelView;

        GL.Begin(GL.LINES);
        GL.Color(Color.red);
        GL.Vertex(Vector3.zero);
        GL.Vertex(Vector3.right);
        GL.Vertex(Vector3.right);
        GL.Vertex(Vector3.right + new Vector3(-1, 0, 1) * 0.1f);
        GL.Vertex(Vector3.right);
        GL.Vertex(Vector3.right + new Vector3(-1, 0, -1) * 0.1f);

        GL.Color(Color.green);
        GL.Vertex(Vector3.zero);
        GL.Vertex(Vector3.up);
        GL.Vertex(Vector3.up);
        GL.Vertex(Vector3.up + new Vector3(-1, -1, 0) * 0.1f);
        GL.Vertex(Vector3.up);
        GL.Vertex(Vector3.up + new Vector3(1, -1, 0) * 0.1f);

        GL.Color(Color.blue);
        GL.Vertex(Vector3.zero);
        GL.Vertex(Vector3.forward);
        GL.Vertex(Vector3.forward);
        GL.Vertex(Vector3.forward + new Vector3(-1, 0, -1) * 0.1f);
        GL.Vertex(Vector3.forward);
        GL.Vertex(Vector3.forward + new Vector3(1, 0, -1) * 0.1f);
        GL.End();

        GL.PopMatrix();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _lastMousePosition = Input.mousePosition;
        if (Input.GetMouseButton(mouseDragViewButton))
        {
            _camHeading = _renderCam.transform.localEulerAngles.y;
            _camPitch = _renderCam.transform.localEulerAngles.x;
            while (_camPitch > 90)
                _camPitch -= 180;
            while (_camPitch < -90)
                _camPitch += 180;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Input.GetMouseButton(mouseDragViewButton))
        {
            Vector3 diff = PointerPosToViewportCenteredPos(Input.mousePosition) - PointerPosToViewportCenteredPos(_lastMousePosition);
            float sensitivity = _renderCam.fieldOfView / 2;
            if (_node == null)
                sensitivity *= viewNoNodeDragSensitivity; // move a bit faster outside node.
            else
            {
                // reverse direction within node, since we're likely to zoom in until it feels like we're panning the node.
                sensitivity *= -1;
                // shift/alt affect sensitivity only in node view, to help with panning.
                if (Input.GetKey(KeyCode.LeftShift))
                    sensitivity *= 5;
                else if (Input.GetKey(KeyCode.LeftAlt))
                    sensitivity /= 5;
            }
            _camHeading += diff.x * sensitivity;
            _camPitch = Mathf.Clamp(_camPitch - diff.y * sensitivity, -89, 89);
            _renderCam.transform.localEulerAngles = new Vector3(_camPitch, _camHeading, 0);
            _lastMousePosition = Input.mousePosition;
            if (_node != null)
                UpdatePoiPositions();
        }
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (_node == null)
        {
            if (Physics.Raycast(_renderCam.ViewportPointToRay(PointerPosToViewport01Pos(Input.mousePosition)), out RaycastHit hit))
            {
                NodeRenderer hitNode = hit.collider.GetComponent<NodeRenderer>();
                if (hitNode != null)
                    mainHandler.HoveredNodes = new List<NodeRenderer>() { hitNode };
                else
                    mainHandler.HoveredNodes = null;
            }
            else
                mainHandler.HoveredNodes = null;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_node == null)
        {
            if (Input.GetMouseButtonUp(mouseSelectButton))
            {
                if (Physics.Raycast(_renderCam.ViewportPointToRay(PointerPosToViewport01Pos(Input.mousePosition)), out RaycastHit hit))
                {
                    NodeRenderer hitNode = hit.collider.GetComponent<NodeRenderer>();
                    if (hitNode != null)
                        mainHandler.ActiveNode = hitNode;
                }
                else
                    mainHandler.ActiveNode = null;
            }
        }
        else
        {
            if (Input.GetMouseButtonUp(mouseSelectButton))
            {
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    // Create new POI.
                    Ray ray = _renderCam.ViewportPointToRay(PointerPosToViewport01Pos(Input.mousePosition));
                    CreatePointOfInterestOperation op = new(_node, ray.direction);
                    mainHandler.EditDo(op);
                }
                else
                {
                    // PointOfInterest activePoi = mainHandler.ActivePoi;
                    // if (activePoi != null)
                    // {
                    //     PoiOnNode existing = _node.pointsOfInterest.FirstOrDefault(pon => pon.Point == activePoi);
                    //     if (existing != null)

                    // }
                    mainHandler.ActivePoi = null;
                }
            }
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (_node == null)
        {
            Vector3 movementInput = new(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"),
                Input.GetAxisRaw("UpDown"));
            if (movementInput.sqrMagnitude != 0 || Input.GetMouseButton(mouseDragViewButton))
            {
                // If we're "controlling" (moving/rotating) the cam, change the cam translation speed.
                _viewportCustomMoveSensitivity = Mathf.Clamp(_viewportCustomMoveSensitivity * (1 + eventData.scrollDelta.y * 0.1f), 0.1f, 10);
                print($"OnScroll {_viewportCustomMoveSensitivity}");
            }
            else
            {
                // If we're just hovering the 3D view, zoom by moving the camera along its forward axis.
                _renderCam.transform.position += _renderCam.transform.forward * eventData.scrollDelta.y * 0.1f * _viewportCustomMoveSensitivity;
            }
        }
        else
        {
            // Just zoom by changing FOV.
            _renderCam.fieldOfView = Mathf.Clamp(Mathf.Pow(_renderCam.fieldOfView, 1 + eventData.scrollDelta.y * -0.1f), 10, 120);
            UpdatePoiPositions();
        }
    }

    public void OnPointerEnter(PointerEventData _)
    {
        if (_node)
        {
            mainHandler.HoveredNodes = new List<NodeRenderer>() { _node };
            mainHandler.HoveredPois = _node.pointsOfInterest.Select(pon => pon.Point).ToList();
        }
    }

    public void OnPointerExit(PointerEventData _)
    {
        if (_node)
        {
            mainHandler.HoveredNodes = null;
            mainHandler.HoveredPois = null;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (_node != null)
        {
            PointOfInterest poi = RuntimeInspectorUtils.GetAssignableObjectFromDraggedReferenceItem<PointOfInterest>(eventData);
            if (poi && (_node == null || !_node.pointsOfInterest.Any(pon => pon.Point == poi)))
            {
                // Create new POI.
                Ray ray = _renderCam.ViewportPointToRay(PointerPosToViewport01Pos(Input.mousePosition));
                LinkPointOfInterestOperation op = new(poi, _node, ray.direction);
                mainHandler.EditDo(op);
            }
        }
    }

    #endregion
}
