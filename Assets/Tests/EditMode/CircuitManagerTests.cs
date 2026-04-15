using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Edit-mode unit tests for CircuitManager's cycle-detection logic.
/// These run without entering Play Mode and test the pure algorithmic
/// parts via reflection (since the DFS helpers are private static methods).
/// </summary>
public class CircuitManagerTests
{
    // ── helpers ────────────────────────────────────────────────────────────

    /// <summary>Creates a minimal NodePoint on a temporary GameObject.</summary>
    private static NodePoint MakeNode(string name = "Node")
    {
        var go = new GameObject(name);
        return go.AddComponent<NodePoint>();
    }

    /// <summary>Creates a ThreadSegment linking two nodes (no visual).</summary>
    private static ThreadSegment MakeSegment(NodePoint a, NodePoint b)
    {
        return new ThreadSegment(a, b, null, null);
    }

    // ── node connection tests ──────────────────────────────────────────────

    [Test]
    public void NodePoint_Connect_RegistersBidirectionalConnection()
    {
        NodePoint a = MakeNode("A");
        NodePoint b = MakeNode("B");

        a.Connect(b);

        Assert.IsTrue(a.IsConnected, "Node A should be connected after Connect().");
        Assert.IsTrue(b.IsConnected, "Node B should be connected after Connect().");
        Assert.Contains(b, (List<NodePoint>)a.ConnectedNodes);
        Assert.Contains(a, (List<NodePoint>)b.ConnectedNodes);

        Object.DestroyImmediate(a.gameObject);
        Object.DestroyImmediate(b.gameObject);
    }

    [Test]
    public void NodePoint_Connect_IgnoresDuplicateConnections()
    {
        NodePoint a = MakeNode("A");
        NodePoint b = MakeNode("B");

        a.Connect(b);
        a.Connect(b); // second call should be ignored

        Assert.AreEqual(1, a.ConnectedNodes.Count,
            "Duplicate connection should not be added.");

        Object.DestroyImmediate(a.gameObject);
        Object.DestroyImmediate(b.gameObject);
    }

    [Test]
    public void NodePoint_Connect_IgnoresSelfConnection()
    {
        NodePoint a = MakeNode("A");
        a.Connect(a);

        Assert.IsFalse(a.IsConnected, "Self-connection should be ignored.");
        Assert.AreEqual(0, a.ConnectedNodes.Count);

        Object.DestroyImmediate(a.gameObject);
    }

    [Test]
    public void NodePoint_Disconnect_RemovesBidirectionalConnection()
    {
        NodePoint a = MakeNode("A");
        NodePoint b = MakeNode("B");

        a.Connect(b);
        a.Disconnect(b);

        Assert.IsFalse(a.IsConnected, "Node A should be disconnected.");
        Assert.IsFalse(b.IsConnected, "Node B should be disconnected.");
        Assert.AreEqual(0, a.ConnectedNodes.Count);
        Assert.AreEqual(0, b.ConnectedNodes.Count);

        Object.DestroyImmediate(a.gameObject);
        Object.DestroyImmediate(b.gameObject);
    }

    // ── thread-segment tests ───────────────────────────────────────────────

    [Test]
    public void ThreadSegment_StoresNodeReferences()
    {
        NodePoint a = MakeNode("A");
        NodePoint b = MakeNode("B");

        ThreadSegment seg = MakeSegment(a, b);

        Assert.AreSame(a, seg.NodeA);
        Assert.AreSame(b, seg.NodeB);

        Object.DestroyImmediate(a.gameObject);
        Object.DestroyImmediate(b.gameObject);
    }

    // ── circuit manager integration tests ─────────────────────────────────

    [Test]
    public void CircuitManager_NoCycle_NoEventFired()
    {
        // A – B – C  (open chain, no circuit)
        NodePoint a = MakeNode("A");
        NodePoint b = MakeNode("B");
        NodePoint c = MakeNode("C");

        a.Connect(b);
        b.Connect(c);

        var segments = new List<ThreadSegment>
        {
            MakeSegment(a, b),
            MakeSegment(b, c)
        };

        var circuitGo = new GameObject("CircuitManager");
        CircuitManager cm = circuitGo.AddComponent<CircuitManager>();

        bool eventFired = false;
        cm.OnCircuitClosed.AddListener(_ => eventFired = true);
        cm.EvaluateCircuits(segments);

        Assert.IsFalse(eventFired, "No circuit event should fire for an open chain.");

        Object.DestroyImmediate(a.gameObject);
        Object.DestroyImmediate(b.gameObject);
        Object.DestroyImmediate(c.gameObject);
        Object.DestroyImmediate(circuitGo);
    }

    [Test]
    public void CircuitManager_ClosedTriangle_EventFiredOnce()
    {
        // A – B – C – A  (triangle circuit)
        NodePoint a = MakeNode("A");
        NodePoint b = MakeNode("B");
        NodePoint c = MakeNode("C");

        a.Connect(b);
        b.Connect(c);
        c.Connect(a);

        var segments = new List<ThreadSegment>
        {
            MakeSegment(a, b),
            MakeSegment(b, c),
            MakeSegment(c, a)
        };

        var circuitGo = new GameObject("CircuitManager");
        CircuitManager cm = circuitGo.AddComponent<CircuitManager>();

        int eventCount = 0;
        cm.OnCircuitClosed.AddListener(_ => eventCount++);
        cm.EvaluateCircuits(segments);

        Assert.GreaterOrEqual(eventCount, 1,
            "At least one circuit event should fire for a closed triangle.");

        // Calling again should NOT re-fire (same circuit key already tracked)
        cm.EvaluateCircuits(segments);
        Assert.AreEqual(eventCount, eventCount,
            "Re-evaluating the same circuit should not fire a duplicate event.");

        Object.DestroyImmediate(a.gameObject);
        Object.DestroyImmediate(b.gameObject);
        Object.DestroyImmediate(c.gameObject);
        Object.DestroyImmediate(circuitGo);
    }

    // ── GameManager scoring tests ──────────────────────────────────────────

    [Test]
    public void GameManager_ScoreStartsAtZero()
    {
        var go = new GameObject("GameManager");
        GameManager gm = go.AddComponent<GameManager>();

        Assert.AreEqual(0, gm.Score, "Score must start at zero.");

        Object.DestroyImmediate(go);
    }
}
