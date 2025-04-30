using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace traintracks;

public static class TimingHelper
{
    public static CoroutineHandle RunCoroutine(this Node node, IEnumerator<float> coroutine, [CallerArgumentExpression(nameof(coroutine))] string name = "")
        => Timing.RunCoroutine(node, coroutine, name);
}

public struct CoroutineHandle(Node node, IEnumerator<float> coroutine, string name)
{
    public string Name { get; private set; } = name;
    public readonly Node Node = node;
    private IEnumerator<float> Coroutine = coroutine;
    public bool IsRunning { get; private set; } = false;

    public readonly List<(IEnumerator<float> enumerator, string name)> ThenEnumerators { get; } = [];

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

    public override readonly string ToString() => $"Coroutine[{Node}, {Name}]";

    //public override bool Equals([NotNullWhen(true)] object? obj)
    //{
    //    return obj is CoroutineHandle ch && ch.Id == Id && ch.Node == Node;
    //}

    public readonly CoroutineHandle Then(IEnumerator<float> then, [CallerArgumentExpression(nameof(then))] string name ="")
    {
        ThenEnumerators.Add((then, name));
        return this;
    }

    public CoroutineHandle ThenNext()
    {
        (Coroutine, Name) = ThenEnumerators[0];
        ThenEnumerators.RemoveAt(0);
        IsRunning = true;
        return this;
    }

    public bool HasThenNext() => ThenEnumerators.Count > 0;
}

public partial class TreeTiming : Node
{
    private readonly List<(CoroutineHandle handle, double nextTime)> handles = [];
    public double Elapsed { get; private set; }

    public void RunCoroutine(CoroutineHandle handle)
    {
        handle.Start();
        handles.Add((handle, 0));
    }

    public override void _Process(double delta)
    {
        Elapsed += delta;

        var iter = handles.ToArray();
        handles.Clear();
        foreach (var pair in iter)
        {
            var (handle, nextTime) = pair;
            if (!handle.Node.IsInsideTree())
            {
                handle.Stop();
            }
            else if (!handle.IsRunning)
            {
            }
            else if (handle.Node.CanProcess() && Elapsed >= nextTime)
            {
                if (handle.MoveNext(out float next))
                    handles.Add((handle, Elapsed + next));
                else if (handle.HasThenNext())
                    handles.Add((handle.ThenNext(), nextTime));
                else
                    handle.Stop();
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
        var tree = new TreeTiming();
        Trees[root] = tree;
        if (!root.IsInsideTree())
        {
            void onReady()
            {
                root.AddChild(tree);
                root.Ready -= onReady;
            }
            root.Ready += onReady;
        }
        else
            root.AddChild(tree);
        return tree;
    }

    public static CoroutineHandle RunCoroutine(Node node, IEnumerator<float> coroutine, [CallerArgumentExpression(nameof(coroutine))] string name = "")
    {
        var handle = new CoroutineHandle(node, coroutine, name);
        if (!node.IsInsideTree())
        {
            void onReady()
            {
                _RunCoroutine(handle);
                node.Ready -= onReady;
            }
            node.Ready += onReady;
            return handle;
        }
        _RunCoroutine(handle);
        return handle;
    }

    private static void _RunCoroutine(CoroutineHandle handle)
    {
        var root = handle.Node.GetTree().Root.GetChild(0);
        if (!Trees.TryGetValue(root, out var tree))
        {
            tree = CreateTree(root);
        }
        tree.RunCoroutine(handle);
    }

    public static void StopCoroutine(CoroutineHandle handle)
    {
        if (!handle.IsRunning)
            return;

        if (!handle.Node.IsInsideTree())
        {
            handle.Stop();
            return;
        }

        var root = handle.Node.GetTree().Root.GetChild(0);
        if (!Trees.TryGetValue(root, out TreeTiming? tree))
            return;
        tree.StopCoroutine(handle);
    }
}
