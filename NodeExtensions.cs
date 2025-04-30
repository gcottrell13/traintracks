using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace traintracks;

public static class NodeExtensions
{
    public static IEnumerable<T> GetChildrenByType<T>(this Node node) where T : Node
    {
        foreach (Node child in node.GetChildren())
            if (child is T t)
                yield return t;
    }

    //public static T? FindChild<T>(this Node node, string name, bool recursive = true) where T : Node 
    //    => node.FindChild(name, recursive) is T t ? t : null;

    public static CancellationTokenSource CallAsync(this Node node, Func<CancellationToken, Task> fn)
    {
        var tokenSource2 = new CancellationTokenSource();
        _ = fn(tokenSource2.Token);
        return tokenSource2;
    }

    public static void AddChildAsync(this Node node, Node child) => node.CallDeferred("add_child", child);
}
