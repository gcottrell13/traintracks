using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace traintracks;

public struct CoroutineHandle(Node root, Node node, IEnumerator<float> coroutine, string name, int id)
{
    public readonly string Name = name;
    private readonly int Id = id;
    public readonly Node Root = root;
    public readonly Node Node = node;
    private readonly IEnumerator<float> Coroutine = coroutine;
    public bool IsRunning { get; private set; } = false;

    public readonly bool MoveNext(out float next)
    {
        next = 0;
        if (!IsRunning)
            return false;
        if (!Coroutine.MoveNext())
            return false;
        next = Coroutine.Current;
        return true;
    }

    public void Start()
    {
        IsRunning = true;
    }

    public void Stop()
    {
        IsRunning = false;
    }

    public override string ToString()
    {
        return $"Coroutine[{Root}, {Node}, {Name}]";
    }

    //public override bool Equals([NotNullWhen(true)] object? obj)
    //{
    //    return obj is CoroutineHandle ch && ch.Id == Id && ch.Node == Node;
    //}
}

public partial class TreeTiming : Node
{
    private readonly List<(CoroutineHandle handle, double nextTime)> handles = [];
    public double Elapsed { get; private set; }

    public Node Root;
    public int count;

    public CoroutineHandle RunCoroutine(Node node, IEnumerator<float> coroutine, string name)
    {
        var handle = new CoroutineHandle(Root, node, coroutine, name, count++);
        handle.Start();
        handles.Add((handle, 0));
        return handle;
    }

    public override void _Process(double delta)
    {
        Elapsed += delta;

        var iter = handles.ToArray();
        handles.Clear();
        foreach (var pair in iter)
        {
            var (handle, nextTime) = pair;
            if (!handle.Node.IsInsideTree() || (handle.Node != Root && !Root.IsAncestorOf(handle.Node)))
            {
            }
            else if (!handle.IsRunning)
            {
            }
            else if (Elapsed >= nextTime)
            {
                if (handle.MoveNext(out float next))
                    handles.Add((handle, Elapsed + next));
            }
            else
            {
                handles.Add(pair);
            }
        }
    }

    public void StopCoroutine(CoroutineHandle handle)
    {
        foreach (var pair in handles)
        {
            if (pair.handle.Equals(handle))
            {
                handle.Stop();
                handles.Remove(pair);
                return;
            }
        }
        // throw new ArgumentException($"tree does not own coroutine handle");
    }
}

public static class Timing
{
    private static readonly Dictionary<Node, TreeTiming> Trees = [];

    public static TreeTiming CreateTree(Node root)
    {
        var tree = new TreeTiming
        {
            Root = root
        };
        Trees[root] = tree;
        return tree;
    }

    public static void RemoveTree(Node root)
    {
        Trees.Remove(root);
    }

    public static CoroutineHandle RunCoroutine(Node node, IEnumerator<float> coroutine, [CallerArgumentExpression(nameof(coroutine))] string name = "")
    {
        foreach (var root in Trees.ToList())
        {
            if (!root.Key.IsInsideTree())
                Trees.Remove(root.Key);
            else if (root.Key == node || root.Key.IsAncestorOf(node))
                return root.Value.RunCoroutine(node, coroutine, name);
        }
        throw new ApplicationException($"node {node.GetPath()} does not have an initialized {nameof(TreeTiming)}");
    }

    public static void StopCoroutine(CoroutineHandle handle)
    {
        if (!handle.IsRunning)
            return;
        if (!Trees.TryGetValue(handle.Root, out TreeTiming? tree))
            throw new ArgumentException($"Could not find timing tree for node: {handle.Node.GetPath()}");
        tree.StopCoroutine(handle);
    }
}
