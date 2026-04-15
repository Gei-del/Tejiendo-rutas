using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a connection node in the city grid.
/// Nodes are the "anchors" where threads can be attached.
/// Each node belongs to one of three types that define which colour of
/// thread can be used to connect it.
/// </summary>
public class NodePoint : MonoBehaviour
{
    // ── public API ─────────────────────────────────────────────────────────

    public enum NodeType { Energy, Data, Social }

    [Header("Node Settings")]
    [Tooltip("Semantic type of this node – determines thread colour.")]
    [SerializeField] private NodeType nodeType = NodeType.Energy;

    [Tooltip("Thread colour assigned to this node type (used as fallback " +
             "if ThreadManager has no explicit colour mapping).")]
    [SerializeField] private Color nodeColor = Color.yellow;

    [Tooltip("Visual renderer that changes colour when the node is connected.")]
    [SerializeField] private Renderer nodeRenderer;

    [Header("Audio / Feedback")]
    [Tooltip("Played when a thread connects to this node.")]
    [SerializeField] private AudioClip connectSound;

    /// <summary>Whether this node currently has at least one active thread.</summary>
    public bool IsConnected { get; private set; }

    /// <summary>Semantic category of this node.</summary>
    public NodeType Type => nodeType;

    /// <summary>Base colour associated with this node's type.</summary>
    public Color NodeColor => nodeColor;

    /// <summary>All nodes that are directly connected to this one.</summary>
    public IReadOnlyList<NodePoint> ConnectedNodes => _connectedNodes;

    // ── private state ──────────────────────────────────────────────────────

    private readonly List<NodePoint> _connectedNodes = new List<NodePoint>();
    private AudioSource _audio;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    // ── lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        _audio = GetComponent<AudioSource>();

        if (nodeRenderer == null)
            nodeRenderer = GetComponentInChildren<Renderer>();

        // Start grey / unlit
        SetEmission(Color.gray * 0.3f);
    }

    // ── connection management ──────────────────────────────────────────────

    /// <summary>
    /// Registers a bidirectional connection between this node and
    /// <paramref name="other"/>.  Safe to call multiple times; duplicate
    /// connections are ignored.
    /// </summary>
    public void Connect(NodePoint other)
    {
        if (other == null || other == this) return;
        if (_connectedNodes.Contains(other)) return;

        _connectedNodes.Add(other);
        other._connectedNodes.Add(this);

        IsConnected = true;
        other.IsConnected = true;

        OnConnected();
        other.OnConnected();
    }

    /// <summary>
    /// Removes a previously registered connection.
    /// </summary>
    public void Disconnect(NodePoint other)
    {
        if (other == null) return;
        _connectedNodes.Remove(other);
        other._connectedNodes.Remove(this);

        if (_connectedNodes.Count == 0) IsConnected = false;
        if (other._connectedNodes.Count == 0) other.IsConnected = false;
    }

    // ── feedback ───────────────────────────────────────────────────────────

    private void OnConnected()
    {
        SetEmission(nodeColor);

        if (_audio != null && connectSound != null)
            _audio.PlayOneShot(connectSound);
    }

    private void SetEmission(Color color)
    {
        if (nodeRenderer == null) return;

        // Use MaterialPropertyBlock to avoid creating extra material instances
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        nodeRenderer.GetPropertyBlock(block);
        block.SetColor(EmissionColorId, color);
        nodeRenderer.SetPropertyBlock(block);
    }

    // ── gizmos ─────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Gizmos.color = IsConnected ? nodeColor : Color.gray;
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
}
