using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Phase 2: Thread / rope mechanic using Unity's LineRenderer.
/// Manages the pool of active thread segments and notifies the
/// CircuitManager when a closed loop is formed.
/// Attach this component to the same GameObject as PlayerController.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ThreadManager : MonoBehaviour
{
    // ── serialised config ──────────────────────────────────────────────────

    [Header("Thread Resource")]
    [Tooltip("Total amount of thread the player starts with.")]
    [SerializeField] private float maxThreadLength = 50f;

    [Header("Line Appearance")]
    [Tooltip("Width at the start (player end) of a forming thread.")]
    [SerializeField] private float lineStartWidth = 0.08f;

    [Tooltip("Width at the end (node end) of a forming thread.")]
    [SerializeField] private float lineEndWidth = 0.04f;

    [Tooltip("Colour map: index matches NodePoint.NodeType enum value.")]
    [SerializeField] private Color[] threadColors = { Color.yellow, Color.cyan, Color.magenta };

    [Header("References")]
    [SerializeField] private CircuitManager circuitManager;

    // ── public properties ──────────────────────────────────────────────────

    /// <summary>Thread remaining as a 0-1 fraction.</summary>
    public float ThreadFraction => _threadRemaining / maxThreadLength;

    /// <summary>Exact remaining thread length.</summary>
    public float ThreadRemaining => _threadRemaining;

    /// <summary>Whether the player currently has a thread in progress.</summary>
    public bool IsConnecting => _anchorNode != null;

    // ── private state ──────────────────────────────────────────────────────

    private LineRenderer _previewLine;          // the "live" segment being drawn
    private float _threadRemaining;
    private NodePoint _anchorNode;              // node the current thread started from

    // Shared material for all thread line renderers (avoids per-segment material instances)
    private Material _sharedLineMaterial;

    // All completed thread segments (persisted in the scene)
    private readonly List<ThreadSegment> _segments = new List<ThreadSegment>();

    // ── lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        _threadRemaining = maxThreadLength;
        _sharedLineMaterial = new Material(Shader.Find("Sprites/Default"));

        _previewLine = GetComponent<LineRenderer>();
        ConfigureLine(_previewLine, Color.white);
        _previewLine.enabled = false;

        if (circuitManager == null)
            circuitManager = FindObjectOfType<CircuitManager>();
    }

    private void Update()
    {
        if (_anchorNode != null)
            UpdatePreview();
    }

    // ── public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to begin or complete a thread connection to
    /// <paramref name="target"/>.
    /// </summary>
    public void TryConnect(NodePoint target)
    {
        if (target == null) return;

        if (_anchorNode == null)
        {
            // Start a new thread from this node
            BeginThread(target);
        }
        else if (target != _anchorNode)
        {
            // Attempt to complete the thread to the second node
            float dist = Vector3.Distance(_anchorNode.transform.position, target.transform.position);
            if (dist > _threadRemaining)
            {
                Debug.Log("[ThreadManager] Not enough thread remaining.");
                return;
            }

            CompleteThread(_anchorNode, target, dist);
        }
    }

    /// <summary>
    /// Cancels the thread currently being drawn without consuming resources.
    /// </summary>
    public void CancelCurrentThread()
    {
        _anchorNode = null;
        _previewLine.enabled = false;
    }

    // ── private helpers ────────────────────────────────────────────────────

    private void BeginThread(NodePoint startNode)
    {
        _anchorNode = startNode;

        Color lineColor = GetColorForNode(startNode);
        ConfigureLine(_previewLine, lineColor);
        _previewLine.SetPosition(0, startNode.transform.position);
        _previewLine.SetPosition(1, startNode.transform.position);
        _previewLine.enabled = true;
    }

    /// <summary>
    /// Updates the preview line's end point to follow the player each frame.
    /// </summary>
    private void UpdatePreview()
    {
        Vector3 endPos = transform.position;

        // Clamp end point so it never exceeds remaining thread
        Vector3 toPlayer = endPos - _anchorNode.transform.position;
        if (toPlayer.magnitude > _threadRemaining)
            endPos = _anchorNode.transform.position + toPlayer.normalized * _threadRemaining;

        _previewLine.SetPosition(0, _anchorNode.transform.position);
        _previewLine.SetPosition(1, endPos);
    }

    private void CompleteThread(NodePoint from, NodePoint to, float cost)
    {
        _threadRemaining -= cost;

        // Register the connection on both nodes
        from.Connect(to);

        // Spawn a permanent visual segment
        ThreadSegment segment = SpawnSegment(from, to, GetColorForNode(from));
        _segments.Add(segment);

        // Reset preview
        _anchorNode = null;
        _previewLine.enabled = false;

        // Check for completed circuits
        circuitManager?.EvaluateCircuits(_segments);
    }

    /// <summary>
    /// Creates a child GameObject with its own LineRenderer to represent
    /// one permanent thread segment between two nodes.
    /// </summary>
    private ThreadSegment SpawnSegment(NodePoint a, NodePoint b, Color color)
    {
        GameObject go = new GameObject($"Thread_{a.name}_{b.name}");
        go.transform.SetParent(transform.parent);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        ConfigureLine(lr, color);
        lr.SetPosition(0, a.transform.position);
        lr.SetPosition(1, b.transform.position);

        return new ThreadSegment(a, b, go, lr);
    }

    private void ConfigureLine(LineRenderer lr, Color color)
    {
        lr.positionCount = 2;
        lr.startWidth = lineStartWidth;
        lr.endWidth = lineEndWidth;
        lr.useWorldSpace = true;
        lr.sharedMaterial = _sharedLineMaterial;
        lr.startColor = color;
        lr.endColor = color * 0.7f;
    }

    private Color GetColorForNode(NodePoint node)
    {
        int index = (int)node.Type;
        if (threadColors != null && index < threadColors.Length)
            return threadColors[index];
        return node.NodeColor;
    }
}

/// <summary>
/// Plain data container for a completed thread segment.
/// </summary>
[System.Serializable]
public class ThreadSegment
{
    public NodePoint NodeA { get; }
    public NodePoint NodeB { get; }
    public GameObject Visual { get; }
    public LineRenderer Line { get; }

    public ThreadSegment(NodePoint a, NodePoint b, GameObject visual, LineRenderer line)
    {
        NodeA = a;
        NodeB = b;
        Visual = visual;
        Line = line;
    }
}
