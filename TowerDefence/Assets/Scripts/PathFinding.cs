﻿using System.Collections.Generic;
using UnityEngine;

public class PathFinding : MonoBehaviour
{
    public delegate float CostFunction(float distance, float cost, float crowFliesDistance, int steps, int turns);

    public static float StandardCostFunction(float distance, float cost, float crowFliesDistance, int steps, int turns)
    {
        return distance + cost + crowFliesDistance + turns;
    }

    public static float NoAdditionalsCostFunction(float distance, float cost, float crowFliesDistance, int steps, int turns)
    {
        return distance + crowFliesDistance;
    }

    public enum PathResult
    {
        Success,
        AtDestination,
        FailureNoPath,
        FailureTooManyTries,
        FailureTooFar,
    }

    [System.Serializable]
    public struct Path
    {
        public bool FoundPath { get; private set; }
        public PathResult Result { get; private set; }
        public List<Node> Nodes { get; private set; }
        public float Distance { get; private set; }
        public float Cost { get; private set; }
        public float CrowFliesDistance { get; private set; }
        public int Steps { get; private set; }
        public int Turns { get; private set; }
        public Node Start => Nodes != null && Nodes.Count > 0 ? Nodes[0] : null;
        public Node End => Nodes != null && Nodes.Count > 0 ? Nodes[Nodes.Count - 1] : null;

        public Path(PathResult result)
        {
            FoundPath = false;
            Result = result;
            Nodes = null;
            Distance = 0;
            Cost = 0;
            CrowFliesDistance = 0;
            Steps = 0;
            Turns = 0;
            Debug.LogWarning("Path Result: " + result);
        }

        public Path(List<Node> nodes, float pathDistance, float pathCrowFliesDistance, float pathCost, int pathSteps, int pathTurns)
        {
            Result = PathResult.Success;
            FoundPath = true;
            Nodes = nodes;
            Distance = pathDistance;
            CrowFliesDistance = pathCrowFliesDistance;
            Cost = pathCost;
            Steps = pathSteps;
            Turns = pathTurns;
        }

        public Path(Path path)
        {
            FoundPath = path.FoundPath;
            Result = path.Result;
            Nodes = path.Nodes == null ? null : new List<Node>(path.Nodes);
            Distance = path.Distance;
            Cost = path.Cost;
            CrowFliesDistance = path.CrowFliesDistance;
            Steps = path.Steps;
            Turns = path.Turns;
        }

        public float TotalCost(CostFunction function)
        {
            return function(Distance, Cost, CrowFliesDistance, Steps, Turns);
        }
    
        public void SetRendererPoints(LineRenderer renderer)
        {
            if (Nodes == null)
            {
                renderer.SetPositions(new Vector3[0]);
            }
            else
            {
                int length = Nodes.Count;
                renderer.positionCount = length;
                for (int i = 0; i < length; i++)
                {
                    renderer.SetPosition(i, new Vector3(Nodes[i].x + 0.5f, 0.05f, Nodes[i].y + 0.5f));
                }
            }
        }
    }

    public static float Distance(Node n1, Node n2)
    {
        return Distance(n1.x, n1.y, n2.x, n2.y);
    }

    public static float Distance(float x1, float y1, float x2, float y2)
    {
        return Mathf.Sqrt(SquareDistance(x1, y1, x2, y2));
    }

    public static float SquareDistance(float x1, float y1, float x2, float y2)
    {
        return (x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1);
    }

    public static readonly float ROOT_2 = Mathf.Sqrt(2);

    public static Path PathFind(Node start, Node end, float maxDistance, int maxTries, CostFunction costFunction)
    {
        
        if (start == null || end == null)
            return new Path(PathResult.FailureNoPath);

        if (end.IsImpassable())
            return new Path(PathResult.FailureNoPath);

        if (start == end)
            return new Path(PathResult.AtDestination);

        float d = Distance(start.x, start.y, end.x, end.y);

        if (d > maxDistance)
            return new Path(PathResult.FailureTooFar);

        List<Node> visited = new List<Node>();
        List<Node> open = new List<Node>();
        List<Node> closed = new List<Node>();

        start.pathDistance = 0;
        start.pathCrowFliesDistance = d;

        open.Add(start);
        visited.Add(start);

        int tries = 0;
        while (true)
        {
            //Debug.Log("Try #" + tries);
            tries++;
            if (tries > maxTries)
            {
                foreach (Node p in visited)
                    p.ClearPathFindingData();

                return new Path(PathResult.FailureTooManyTries);
            }

            Node currentNode = null;

            if (open.Count == 0)
            {
                foreach (Node p in visited)
                    p.ClearPathFindingData();

                return new Path(PathResult.FailureNoPath);
            }

            float currentCost = 0;

            foreach (Node node in open)
            {
                if (currentNode == null)
                {
                    currentNode = node;
                    currentCost = costFunction(currentNode.pathDistance, currentNode.pathCost, currentNode.pathCrowFliesDistance, currentNode.pathSteps, currentNode.pathTurns);
                }
                else
                {
                    float nodeCost = costFunction(node.pathDistance, node.pathCost, node.pathCrowFliesDistance, node.pathSteps, node.pathTurns);
                    if (nodeCost < currentCost)
                    {
                        currentCost = nodeCost;
                        currentNode = node;
                    }
                }
            }

            if (currentNode == end)
            {
                break;
            }

            if (currentNode.pathDistance > maxDistance)
            {
                foreach (Node p in visited)
                    p.ClearPathFindingData();

                return new Path(PathResult.FailureTooFar);
            }

            open.Remove(currentNode);
            closed.Add(currentNode);


            for (int i = 0; i < currentNode.neighbours.Length; i++)
            {
                Node neighbour = currentNode.neighbours[i];

                if (neighbour == null) 
                    continue;

                if (neighbour.IsImpassable()) 
                    continue;

                if (currentNode.ExitBlocked(i))
                    continue;

                float distance = i % 2 == 0 ? 1 : ROOT_2;

                float nextPathDistance = currentNode.pathDistance + distance;
                float nextPathCrowFliesDistance = Distance(neighbour, end);
                float nextPathCost = currentNode.pathCost + neighbour.Cost();// + GetWallCost(currentNode.pos.x, currentNode.pos.y, i);
                int nextPathSteps = currentNode.pathSteps + 1;
                int nextPathTurns = currentNode.pathTurns + ((currentNode.pathSteps == 0 || currentNode.pathEndDirection == i) ? 0 : 1);

                float nextTotalCost = costFunction(nextPathDistance, nextPathCost, nextPathCrowFliesDistance, nextPathSteps, nextPathTurns);
                
                if (nextTotalCost < costFunction(neighbour.pathDistance, neighbour.pathCost, neighbour.pathCrowFliesDistance, neighbour.pathSteps, neighbour.pathTurns))
                {
                    open.Remove(neighbour);
                    closed.Remove(neighbour);
                }

                if (!open.Contains(neighbour) && !closed.Contains(neighbour))
                {
                    neighbour.pathDistance = nextPathDistance;
                    neighbour.pathCrowFliesDistance = nextPathCrowFliesDistance;
                    neighbour.pathCost = nextPathCost;
                    neighbour.pathSteps = nextPathSteps;
                    neighbour.pathTurns = nextPathTurns;

                    neighbour.pathParent = currentNode;
                    neighbour.pathEndDirection = i;

                    open.Add(neighbour);
                    if (!visited.Contains(neighbour))
                        visited.Add(neighbour);
                }
            }
        }

        List<Node> nodes = new List<Node>();
        Node current = end;
        while (current.pathParent != null)
        {
            nodes.Insert(0, current);
            //nodes.Add(current);
            //this is backwards.
            current = current.pathParent;
        }
        nodes.Insert(0, start);
        //nodes.Add(start);
        //so is this.

        Path result = new Path(nodes, end.pathDistance, end.pathCrowFliesDistance, end.pathCost, end.pathSteps, end.pathTurns);

        foreach (Node p in visited)
            p.ClearPathFindingData();

        return result;
    }

    public static void DrawPath(Path path)
    {
        Node start = path.Start;
        Node end = path.End;
        if (start != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(new Vector3(start.x + 0.5f, 0, start.y + 0.5f), 0.1f);
        }
        if (end != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(new Vector3(end.x + 0.5f, 0, end.y + 0.5f), 0.1f);
        }

        if (path.FoundPath)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < path.Nodes.Count - 1; i++)
            {
                Node n0 = path.Nodes[i];
                Node n1 = path.Nodes[i + 1];
                Gizmos.DrawLine(new Vector3(n0.x + 0.5f, 0, n0.y + 0.5f), new Vector3(n1.x + 0.5f, 0, n1.y + 0.5f));
            }
        }
    }
}
