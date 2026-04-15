using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Phase 3: Detects closed circuits formed by connected NodePoints and
/// fires off celebration events: particle bursts and a grey-to-colour
/// material transition on nearby environment objects.
/// </summary>
public class CircuitManager : MonoBehaviour
{
    // ── serialised config ──────────────────────────────────────────────────

    [Header("Circuit Detection")]
    [Tooltip("Minimum number of nodes required to form a valid circuit.")]
    [SerializeField] private int minCircuitSize = 3;

    [Header("Particles")]
    [Tooltip("Particle system prefab instantiated at each node of a closed circuit.")]
    [SerializeField] private ParticleSystem celebrationParticlePrefab;

    [Tooltip("How long the celebration particles last before being destroyed (seconds).")]
    [SerializeField] private float particleLifetime = 3f;

    [Header("Colour Restoration")]
    [Tooltip("Radius around each circuit node to search for environment renderers.")]
    [SerializeField] private float colorRadius = 8f;

    [Tooltip("How long the grey-to-colour lerp takes (seconds).")]
    [SerializeField] private float colorTransitionDuration = 1.5f;

    [Header("Events")]
    [Tooltip("Invoked with the list of node positions whenever a circuit closes.")]
    public UnityEngine.Events.UnityEvent<List<Vector3>> OnCircuitClosed;

    // ── private state ──────────────────────────────────────────────────────

    /// <summary>Keeps track of which node sets have already triggered events.</summary>
    private readonly HashSet<string> _completedCircuitKeys = new HashSet<string>();

    // ── public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Analyses all <paramref name="segments"/> for closed loops and triggers
    /// celebration effects for any newly discovered circuit.
    /// Called by <see cref="ThreadManager"/> after every successful connection.
    /// </summary>
    public void EvaluateCircuits(IReadOnlyList<ThreadSegment> segments)
    {
        // Build an adjacency list from all segments
        Dictionary<NodePoint, List<NodePoint>> adjacency = BuildAdjacency(segments);

        // Try to find cycles using DFS from every node
        foreach (NodePoint start in adjacency.Keys)
        {
            List<NodePoint> cycle = FindCycle(start, adjacency);
            if (cycle == null || cycle.Count < minCircuitSize) continue;

            string key = MakeCycleKey(cycle);
            if (_completedCircuitKeys.Contains(key)) continue;

            _completedCircuitKeys.Add(key);
            OnCircuitDiscovered(cycle);
        }
    }

    // ── private helpers ────────────────────────────────────────────────────

    private static Dictionary<NodePoint, List<NodePoint>> BuildAdjacency(
        IReadOnlyList<ThreadSegment> segments)
    {
        var adj = new Dictionary<NodePoint, List<NodePoint>>();
        foreach (ThreadSegment seg in segments)
        {
            if (!adj.ContainsKey(seg.NodeA)) adj[seg.NodeA] = new List<NodePoint>();
            if (!adj.ContainsKey(seg.NodeB)) adj[seg.NodeB] = new List<NodePoint>();

            if (!adj[seg.NodeA].Contains(seg.NodeB)) adj[seg.NodeA].Add(seg.NodeB);
            if (!adj[seg.NodeB].Contains(seg.NodeA)) adj[seg.NodeB].Add(seg.NodeA);
        }
        return adj;
    }

    /// <summary>
    /// Iterative DFS that returns the first cycle found starting from
    /// <paramref name="start"/>, or null if none exists.
    /// </summary>
    private static List<NodePoint> FindCycle(
        NodePoint start,
        Dictionary<NodePoint, List<NodePoint>> adjacency)
    {
        var parent  = new Dictionary<NodePoint, NodePoint>();
        var visited = new HashSet<NodePoint>();
        var stack   = new Stack<NodePoint>();

        stack.Push(start);
        parent[start] = null;

        while (stack.Count > 0)
        {
            NodePoint current = stack.Pop();
            if (visited.Contains(current)) continue;
            visited.Add(current);

            if (!adjacency.ContainsKey(current)) continue;

            foreach (NodePoint neighbour in adjacency[current])
            {
                if (!visited.Contains(neighbour))
                {
                    parent[neighbour] = current;
                    stack.Push(neighbour);
                }
                else if (parent.ContainsKey(current) && parent[current] != neighbour)
                {
                    // Found a back-edge → reconstruct the cycle
                    return ReconstructCycle(current, neighbour, parent);
                }
            }
        }
        return null;
    }

    private static List<NodePoint> ReconstructCycle(
        NodePoint cycleEnd,
        NodePoint cycleStart,
        Dictionary<NodePoint, NodePoint> parent)
    {
        var cycle = new List<NodePoint> { cycleStart };
        NodePoint node = cycleEnd;

        while (node != null && node != cycleStart)
        {
            cycle.Insert(1, node);
            if (!parent.TryGetValue(node, out node)) break;
        }
        return cycle;
    }

    private static string MakeCycleKey(List<NodePoint> cycle)
    {
        var ids = new List<int>();
        foreach (NodePoint n in cycle) ids.Add(n.GetInstanceID());
        ids.Sort();
        return string.Join(",", ids);
    }

    // ── celebration events ─────────────────────────────────────────────────

    private void OnCircuitDiscovered(List<NodePoint> cycle)
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (NodePoint n in cycle) positions.Add(n.transform.position);

        // Fire UnityEvent for external listeners (UI, audio, etc.)
        OnCircuitClosed?.Invoke(positions);

        // Start colour restoration coroutine for every node in the circuit
        foreach (NodePoint node in cycle)
        {
            StartCoroutine(RestoreColorAroundNode(node));
            SpawnCelebrationParticles(node.transform.position);
        }

        Debug.Log($"[CircuitManager] Circuit closed with {cycle.Count} nodes!");
    }

    private void SpawnCelebrationParticles(Vector3 position)
    {
        if (celebrationParticlePrefab == null) return;

        ParticleSystem ps = Instantiate(
            celebrationParticlePrefab,
            position,
            Quaternion.identity);

        ps.Play();
        Destroy(ps.gameObject, particleLifetime);
    }

    /// <summary>
    /// Searches for grey-looking environment renderers near a node and
    /// gradually lerps their albedo from greyscale to the node's colour.
    /// </summary>
    private IEnumerator RestoreColorAroundNode(NodePoint node)
    {
        Collider[] nearby = Physics.OverlapSphere(node.transform.position, colorRadius);
        var colorRestorers = new List<ColorRestorer>();

        foreach (Collider col in nearby)
        {
            ColorRestorer cr = col.GetComponent<ColorRestorer>();
            if (cr != null && !cr.IsRestored)
                colorRestorers.Add(cr);
        }

        float elapsed = 0f;
        while (elapsed < colorTransitionDuration)
        {
            float t = elapsed / colorTransitionDuration;
            foreach (ColorRestorer cr in colorRestorers)
                cr.SetLerpAmount(t, node.NodeColor);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap to full colour
        foreach (ColorRestorer cr in colorRestorers)
            cr.CompleteRestore(node.NodeColor);
    }
}
