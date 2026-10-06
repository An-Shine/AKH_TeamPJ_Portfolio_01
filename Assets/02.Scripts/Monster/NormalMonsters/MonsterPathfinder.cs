using System;
using System.Collections.Generic;
using UnityEngine;

// 장애물 앞에서만 작은 격자를 탐색합니다. 씬에 붙이는 컴포넌트가 아닙니다.
internal sealed class MonsterPathfinder
{
    private const float DiagonalCost = 1.41421356f;
    private const int SearchesPerFrame = 2;

    private static readonly Vector2Int[] Steps =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1),
        new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    private static int _searchFrame = -1;
    private static int _searchCount;

    private readonly Func<Vector2, bool> _canOccupy;
    private readonly Func<Vector2, Vector2, bool> _canTravel;
    private readonly Func<Vector2, Vector2, bool> _canSeeTarget;
    private readonly List<Vector2> _path = new List<Vector2>();
    private readonly Dictionary<Vector2Int, bool> _walkable =
        new Dictionary<Vector2Int, bool>();
    private readonly Dictionary<Vector2Int, Node> _nodes =
        new Dictionary<Vector2Int, Node>();
    private readonly List<OpenNode> _open = new List<OpenNode>();

    private float _cellSize;
    private float _nextSearchTime;
    private Vector2 _lastTarget;
    private bool _hasSearched;
    private int _waypoint;
    private Vector2Int _minimumCell;
    private Vector2Int _maximumCell;

    private struct Node
    {
        public Vector2Int Parent;
        public float Cost;
        public bool Closed;
    }

    private struct OpenNode
    {
        public Vector2Int Cell;
        public float Cost;
        public float Estimate;

        public float Score => Cost + Estimate;
    }

    public MonsterPathfinder(
        Func<Vector2, bool> canOccupy,
        Func<Vector2, Vector2, bool> canTravel,
        Func<Vector2, Vector2, bool> canSeeTarget)
    {
        _canOccupy = canOccupy;
        _canTravel = canTravel;
        _canSeeTarget = canSeeTarget;
    }

    public Vector2 GetDirection(
        Vector2 origin,
        Vector2 target,
        float cellSize,
        float repathInterval,
        float searchMargin,
        int maxVisitedNodes,
        float waypointTolerance)
    {
        Vector2 toTarget = target - origin;

        if (toTarget.sqrMagnitude <= 0.000001f)
        {
            return Vector2.zero;
        }

        if (_canTravel(origin, target))
        {
            _path.Clear();
            _hasSearched = false;
            return toTarget.normalized;
        }

        float safeCellSize = Mathf.Max(0.1f, cellSize);

        if (!Mathf.Approximately(_cellSize, safeCellSize))
        {
            _cellSize = safeCellSize;
            _path.Clear();
            _hasSearched = false;
        }

        bool targetMoved = (target - _lastTarget).sqrMagnitude >= _cellSize * _cellSize;
        bool needsPath = !_hasSearched || _waypoint >= _path.Count || targetMoved;

        if (needsPath && (!_hasSearched || Time.time >= _nextSearchTime) &&
            TryReserveSearch())
        {
            _hasSearched = true;
            _lastTarget = target;
            bool found = BuildPath(origin, target, Mathf.Max(_cellSize, searchMargin),
                Mathf.Clamp(maxVisitedNodes, 64, 8192));
            _nextSearchTime = Time.time + Mathf.Max(found ? 0.1f : 1f, repathInterval);
        }

        float tolerance = Mathf.Clamp(waypointTolerance, 0.02f, _cellSize * 0.4f);

        while (_waypoint < _path.Count &&
            (_path[_waypoint] - origin).sqrMagnitude <= tolerance * tolerance)
        {
            _waypoint++;
        }

        if (_waypoint >= _path.Count)
        {
            return Vector2.zero;
        }

        // 바로 갈 수 있는 몇 지점만 건너뛰며 벽과 모서리를 가로지르지 않습니다.
        int lastLookAhead = Mathf.Min(_path.Count - 1, _waypoint + 3);

        for (int i = lastLookAhead; i > _waypoint; i--)
        {
            if (_canTravel(origin, _path[i]))
            {
                _waypoint = i;
                break;
            }
        }

        if (!_canTravel(origin, _path[_waypoint]))
        {
            // 문이 닫히는 등 경로가 막히면 다음 탐색 차례를 기다립니다.
            _path.Clear();
            _nextSearchTime = 0f;
            return Vector2.zero;
        }

        return (_path[_waypoint] - origin).normalized;
    }

    public float GetRemainingStepDistance(Vector2 origin, Vector2 target)
    {
        Vector2 destination = _waypoint < _path.Count ? _path[_waypoint] : target;
        return (destination - origin).magnitude;
    }

    private static bool TryReserveSearch()
    {
        if (_searchFrame != Time.frameCount)
        {
            _searchFrame = Time.frameCount;
            _searchCount = 0;
        }

        if (_searchCount >= SearchesPerFrame)
        {
            return false;
        }

        _searchCount++;
        return true;
    }

    private bool BuildPath(Vector2 origin, Vector2 target, float margin, int maxVisitedNodes)
    {
        _path.Clear();
        _waypoint = 0;
        _walkable.Clear();
        _nodes.Clear();
        _open.Clear();

        Vector2Int originCell = WorldToCell(origin);
        Vector2Int targetCell = WorldToCell(target);
        int padding = Mathf.CeilToInt(margin / _cellSize);
        _minimumCell = new Vector2Int(
            Mathf.Min(originCell.x, targetCell.x) - padding,
            Mathf.Min(originCell.y, targetCell.y) - padding);
        _maximumCell = new Vector2Int(
            Mathf.Max(originCell.x, targetCell.x) + padding,
            Mathf.Max(originCell.y, targetCell.y) + padding);

        if (!TryFindEndpoint(origin, true, out Vector2Int start) ||
            !TryFindEndpoint(target, false, out Vector2Int goal))
        {
            return false;
        }

        _nodes[start] = new Node { Parent = start, Cost = 0f };
        Push(new OpenNode { Cell = start, Cost = 0f, Estimate = Estimate(start, goal) });
        int visited = 0;

        while (_open.Count > 0 && visited < maxVisitedNodes)
        {
            OpenNode current = Pop();
            Node currentNode = _nodes[current.Cell];

            if (currentNode.Closed || current.Cost > currentNode.Cost)
            {
                continue;
            }

            currentNode.Closed = true;
            _nodes[current.Cell] = currentNode;
            visited++;

            if (current.Cell == goal)
            {
                Reconstruct(start, goal);

                if (_canTravel(CellToWorld(goal), target))
                {
                    _path.Add(target);
                }

                return _path.Count > 0;
            }

            foreach (Vector2Int step in Steps)
            {
                Vector2Int next = current.Cell + step;

                if (!IsWalkable(next))
                {
                    continue;
                }

                bool diagonal = step.x != 0 && step.y != 0;

                if (diagonal &&
                    (!IsWalkable(current.Cell + new Vector2Int(step.x, 0)) ||
                    !IsWalkable(current.Cell + new Vector2Int(0, step.y))))
                {
                    continue;
                }

                if (!_canTravel(CellToWorld(current.Cell), CellToWorld(next)))
                {
                    continue;
                }

                float cost = currentNode.Cost + (diagonal ? DiagonalCost : 1f);

                if (_nodes.TryGetValue(next, out Node nextNode) &&
                    (nextNode.Closed || cost >= nextNode.Cost))
                {
                    continue;
                }

                _nodes[next] = new Node { Parent = current.Cell, Cost = cost };
                Push(new OpenNode { Cell = next, Cost = cost, Estimate = Estimate(next, goal) });
            }
        }

        return false;
    }

    private bool TryFindEndpoint(Vector2 position, bool isStart, out Vector2Int result)
    {
        Vector2Int center = WorldToCell(position);
        float bestDistance = float.PositiveInfinity;
        result = center;
        bool found = false;

        for (int y = -3; y <= 3; y++)
        {
            for (int x = -3; x <= 3; x++)
            {
                Vector2Int cell = center + new Vector2Int(x, y);
                Vector2 point = CellToWorld(cell);
                float distance = (point - position).sqrMagnitude;

                if (distance >= bestDistance || !IsWalkable(cell))
                {
                    continue;
                }

                bool reachable = isStart
                    ? _canTravel(position, point)
                    : _canSeeTarget(point, position);

                if (!reachable)
                {
                    continue;
                }

                bestDistance = distance;
                result = cell;
                found = true;
            }
        }

        return found;
    }

    private bool IsWalkable(Vector2Int cell)
    {
        if (cell.x < _minimumCell.x || cell.x > _maximumCell.x ||
            cell.y < _minimumCell.y || cell.y > _maximumCell.y)
        {
            return false;
        }

        if (!_walkable.TryGetValue(cell, out bool walkable))
        {
            walkable = _canOccupy(CellToWorld(cell));
            _walkable[cell] = walkable;
        }

        return walkable;
    }

    private Vector2Int WorldToCell(Vector2 position)
    {
        return new Vector2Int(Mathf.RoundToInt(position.x / _cellSize),
            Mathf.RoundToInt(position.y / _cellSize));
    }

    private Vector2 CellToWorld(Vector2Int cell)
    {
        return new Vector2(cell.x * _cellSize, cell.y * _cellSize);
    }

    private static float Estimate(Vector2Int from, Vector2Int to)
    {
        int x = Mathf.Abs(from.x - to.x);
        int y = Mathf.Abs(from.y - to.y);
        int diagonal = Mathf.Min(x, y);
        return Mathf.Max(x, y) + (DiagonalCost - 1f) * diagonal;
    }

    private void Reconstruct(Vector2Int start, Vector2Int goal)
    {
        Vector2Int current = goal;
        _path.Add(CellToWorld(current));

        while (current != start)
        {
            current = _nodes[current].Parent;
            _path.Add(CellToWorld(current));
        }

        _path.Reverse();
    }

    private void Push(OpenNode node)
    {
        _open.Add(node);
        int index = _open.Count - 1;

        while (index > 0)
        {
            int parent = (index - 1) / 2;

            if (!ComesBefore(node, _open[parent]))
            {
                break;
            }

            _open[index] = _open[parent];
            index = parent;
        }

        _open[index] = node;
    }

    private OpenNode Pop()
    {
        OpenNode result = _open[0];
        int lastIndex = _open.Count - 1;
        OpenNode last = _open[lastIndex];
        _open.RemoveAt(lastIndex);

        if (_open.Count == 0)
        {
            return result;
        }

        int index = 0;

        while (index * 2 + 1 < _open.Count)
        {
            int child = index * 2 + 1;

            if (child + 1 < _open.Count && ComesBefore(_open[child + 1], _open[child]))
            {
                child++;
            }

            if (!ComesBefore(_open[child], last))
            {
                break;
            }

            _open[index] = _open[child];
            index = child;
        }

        _open[index] = last;
        return result;
    }

    private static bool ComesBefore(OpenNode left, OpenNode right)
    {
        return left.Score < right.Score ||
            (Mathf.Approximately(left.Score, right.Score) && left.Estimate < right.Estimate);
    }
}