using System;
using System.Collections.Generic;
using System.Linq;

namespace traintracks;

public interface INode<TSelf, TConnection>
{
    ICollection<(INode<TSelf, TConnection> neighbor, TConnection connection)> Neighbors(TConnection from);
    float Weight { get; }
}

public class Pathfinder<TNode, TConnection> where TNode : INode<TNode, TConnection>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="position"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public TConnection? GetNextStep(TNode position, TConnection t, TNode target)
    {
        HashSet<INode<TNode, TConnection>> visited = [];
        Queue<(TNode node, TConnection first, TConnection last)> frontier = new();
        frontier.Enqueue((position, t, t));

        while (frontier.Count > 0)
        {
            var (current, first, last) = frontier.Dequeue();
            if (current.Equals(target))
                return first;
            visited.Add(current);
            foreach (var pair in current.Neighbors(last)) //.OrderBy(x => x.neighbor.Weight))
            {
                var (neighbor, connection) = pair;
                if (!visited.Contains(neighbor))
                    frontier.Enqueue((current, first, connection));
            }
        }
        return default;
    }
}
