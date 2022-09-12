using HeapTree;
using System;
using System.Collections.Generic;

namespace WeightedDirectedGraph
{
    public class Vertex
    {
        public enum Types
        {
            Space,
            Wall,
            Sand
        }

        public Types Type;

        public int X;
        public int Y;
        public List<Edge> Edges;

        public float Distance;
        public float FinalDistance;
        public Vertex Parent;
        public bool IsVisited;

        public Vertex(int x, int y, Types type)
        {
            Edges = new List<Edge>();
            X = x;
            Y = y;
            Type = type;
        }
    }
    public class Edge
    {
        public Vertex StartPoint;
        public Vertex EndPoint;
        public float Weight;

        public Edge(Vertex startPoint, Vertex endPoint, float weight)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
            Weight = weight;
        }
    }
    public class WeightedDirectedGraph<T>
    {
        private readonly List<Vertex> vertices;
        public float D = 1;                    // distance up, down, left, and right
        public float D2 = (float)Math.Sqrt(2); // direction up-right, up-left, down-right, and down-left

        public Dictionary<Vertex.Types, int> TypesToWeightModifier;

        public IReadOnlyList<Vertex> Vertices => vertices;
        public IReadOnlyList<Edge> Edges
        {
            get
            {
                var edges = new List<Edge>();

                foreach (var vertex in vertices)
                {
                    foreach (var edge in vertex.Edges)
                    {
                        edges.Add(edge);
                    }
                }

                return edges;
            }
        }

        public WeightedDirectedGraph(int sandWeight)
        {
            vertices = new List<Vertex>();

            TypesToWeightModifier = new Dictionary<Vertex.Types, int>()
            {
                { Vertex.Types.Space, 0 },
                { Vertex.Types.Wall, -1 },
                { Vertex.Types.Sand, sandWeight}
            };
        }

        public bool AddVertex(Vertex vertex)
        {
            if (vertex == null || vertex.Edges.Count != 0 || vertices.Contains(vertex)) return false;

            vertices.Add(vertex);
            return true;
        }
        public bool RemoveVertex(Vertex vertex)
        {
            if (!vertices.Contains(vertex)) return false;

            foreach (var item in vertices)
            {
                foreach (var edge in item.Edges)
                {
                    if (edge.EndPoint.Equals(vertex))
                    {
                        item.Edges.Remove(edge);
                    }
                }
            }
            vertices.Remove(vertex);
            return true;
        }

        public bool AddEdge(Vertex a, Vertex b, float distance)
        {
            if (!vertices.Contains(a) || !vertices.Contains(b) || GetEdge(a, b) != null) return false;

            a.Edges.Add(new Edge(a, b, distance));
            return true;
        }
        public bool RemoveEdge(Vertex a, Vertex b)
        {
            var edge = GetEdge(a, b);
            if (edge == null) return false;

            edge.StartPoint.Edges.Remove(edge);
            return true;
        }

        public Edge GetEdge(Vertex a, Vertex b)
        {
            if (!vertices.Contains(a) || !vertices.Contains(b)) return null;

            foreach (var edge in a.Edges)
            {
                if (edge.EndPoint == b)
                {
                    return edge;
                }
            }
            return null;
        }

        public void DijkstraPathfinder(Vertex start, Vertex end, out List<Vertex> path, out List<Vertex> visitedNodes)
        {
            path = new List<Vertex>();
            visitedNodes = new List<Vertex>();

            if (start == null || end == null) { return; }
            var PriorityQ = new MinHeapTree<Vertex>(Comparer<Vertex>.Create((a, b) => a.Distance.CompareTo(b.Distance)));

            foreach (var vertex in Vertices)
            {
                vertex.Distance = float.MaxValue;
                vertex.Parent = null;
                vertex.IsVisited = false;
            }

            start.Distance = 0;
            PriorityQ.Insert(start);

            Vertex current;
            while (!end.IsVisited && PriorityQ.Count > 0)
            {
                current = PriorityQ.Pop();
                current.IsVisited = true;
                visitedNodes.Add(current);

                foreach (var edge in current.Edges)
                {
                    var weightModifier = TypesToWeightModifier[edge.EndPoint.Type];
                    if (weightModifier == -1) continue;

                    var tentativeDist = current.Distance + edge.Weight + weightModifier;
                    if (tentativeDist < edge.EndPoint.Distance)
                    {
                        edge.EndPoint.Distance = tentativeDist;
                        edge.EndPoint.Parent = edge.StartPoint;
                        edge.EndPoint.IsVisited = false;
                    }
                    if (!edge.EndPoint.IsVisited && !PriorityQ.Contains(edge.EndPoint))
                    {
                        PriorityQ.Insert(edge.EndPoint);
                    }
                }
            }

            current = end;
            path.Add(current);
            while (current != start)
            {
                if (current == null) { path = new List<Vertex>() { start }; return; }

                path.Add(current.Parent);
                current = current.Parent;
            }

            // Reverse because it moves from the end vertex through parents to the start
            path.Reverse();
        }

        public void AStarPathfinder(Vertex start, Vertex end, float d, float d2, out List<Vertex> path, out List<Vertex> visitedNodes, Func<Vertex, Vertex, float, float, float> heuristic)
        {
            path = new List<Vertex>();
            visitedNodes = new List<Vertex>();

            if (start == null || end == null) { return; }
            var PriorityQ = new MinHeapTree<Vertex>(Comparer<Vertex>.Create((a, b) => a.FinalDistance.CompareTo(b.FinalDistance)));

            foreach (var vertex in Vertices)
            {
                vertex.Distance = float.MaxValue;
                vertex.FinalDistance = float.MaxValue;
                vertex.Parent = null;
                vertex.IsVisited = false;
            }

            start.Distance = 0;
            start.FinalDistance = heuristic.Invoke(start, end, d, d2);
            PriorityQ.Insert(start);

            Vertex current;
            while (!end.IsVisited && PriorityQ.Count > 0)
            {
                current = PriorityQ.Pop();
                current.IsVisited = true;
                visitedNodes.Add(current);

                foreach (var edge in current.Edges)
                {
                    var weightModifier = TypesToWeightModifier[edge.EndPoint.Type];
                    if (weightModifier == -1) continue;

                    var tentativeDist = current.Distance + edge.Weight + weightModifier;
                    if (tentativeDist < edge.EndPoint.Distance)
                    {
                        edge.EndPoint.Distance = tentativeDist;
                        edge.EndPoint.Parent = edge.StartPoint;
                        edge.EndPoint.FinalDistance = tentativeDist + heuristic.Invoke(edge.EndPoint, end, d, d2);
                        edge.EndPoint.IsVisited = false;
                    }
                    if (!edge.EndPoint.IsVisited && !PriorityQ.Contains(edge.EndPoint))
                    {
                        PriorityQ.Insert(edge.EndPoint);
                    }
                }
            }

            current = end;
            path.Add(current);
            while (current != start)
            {
                if (current == null) { path = new List<Vertex>() { start }; return; }

                path.Add(current.Parent);
                current = current.Parent;
            }

            // Reverse because it moves from the end vertex through parents to the start
            path.Reverse();
        }
        public static float Manhattan(Vertex node, Vertex end, float d, float d2)
        {
            var dx = Math.Abs(node.X - end.X);
            var dy = Math.Abs(node.Y - end.Y);
            return d * (dx + dy);
        }
        public static float Diagonal(Vertex node, Vertex end, float d, float d2)
        {
            var dx = Math.Abs(node.X - end.X);
            var dy = Math.Abs(node.Y - end.Y);
            return d * (dx + dy) + (d2 - 2) * Math.Min(dx, dy);
        }
        public static float Euclidean(Vertex node, Vertex end, float d, float d2)
        {
            var dx = Math.Abs(node.X - end.X);
            var dy = Math.Abs(node.Y - end.Y);
            return d * (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}