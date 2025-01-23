using System.Runtime.CompilerServices;
using System.Text;

namespace MacroHexCompiler.NumericalReflection;

public static class NumberSolver {
    private static Dictionary<float, (string, float)> _cache = new();
    
    private static readonly (int dx, int dy)[] Neighbors = [
        (1, 0), (0, 1), (-1, 1), (-1, 0), (0, -1), (1, -1)
    ];

    private static readonly int[][] DirectionTransitions = [
        [4, 5, 0, 1, 2],
        [5, 0, 1, 2, 3],
        [0, 1, 2, 3, 4],
        [1, 2, 3, 4, 5],
        [2, 3, 4, 5, 0],
        [3, 4, 5, 0, 1]
    ];

    public static (string Path, bool Exact, float diff) Solve(float target, int maxDepth) {
        if (_cache.TryGetValue(target, out (string, float) value)) {
            Compiler.VerbosePrint("Number Cache hit");
            return (value.Item1, value.Item2 == 0f, value.Item2);
        }
        
        bool isPositive = target >= 0;
        State initial = GenerateInitialState(isPositive);
        (string Path, float Diff, bool Exact) best = (initial.Path, Diff: Math.Abs(target), Exact: false);

        PriorityQueue<State, float> queue = new();
        HashSet<State> visited = [];

        queue.Enqueue(initial, best.Diff);

        while (queue.Count > 0) {
            State current = queue.Dequeue();

            _cache.TryAdd(current.Value, (current.Path, 0));
            if (UpdateBest(ref best, current, target))
                if (best.Exact)
                    break;

            if (current.Depth >= maxDepth) continue;

            ExploreMoves(current, isPositive, target, queue, visited);
        }

        if (!best.Exact)
            _cache[target] = (best.Path, best.Diff);
        return (best.Path, best.Exact, best.Diff);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static State GenerateInitialState(bool isPositive) {
        HashSet<Edge> edges = [];
        string moves = isPositive ? "aqaa" : "dedd";
        int x = 0, y = 0, dir = isPositive ? 2 : 4;
        StringBuilder path = new();

        foreach (char move in moves) {
            int newDir = DirectionTransitions[dir][GetMoveIndex(move)];
            (int dx, int dy) = Neighbors[newDir];
            int newX = x + dx;
            int newY = y + dy;

            edges.Add(new Edge(x, y, newX, newY));
            path.Append(move);

            x = newX;
            y = newY;
            dir = newDir;
        }

        return new State(x, y, dir, 0, 0, edges, path.ToString());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool UpdateBest(ref (string Path, float Diff, bool Exact) best,
        State current, float target) {
        float diff = Math.Abs(current.Value - target);
        if (diff >= best.Diff) return false;

        best.Path = current.Path;
        best.Diff = diff;
        best.Exact = diff == 0;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ExploreMoves(State current, bool isPositive, float target,
        PriorityQueue<State, float> queue,
        HashSet<State> visited) {
        foreach (char move in new[] { 'a', 'q', 'w', 'e', 'd' }) {
            int newDir = DirectionTransitions[current.Direction][GetMoveIndex(move)];
            (int dx, int dy) = Neighbors[newDir];
            int newX = current.X + dx;
            int newY = current.Y + dy;
            Edge edge = new(current.X, current.Y, newX, newY);

            if (current.UsedEdges.Contains(edge)) continue;

            float newValue = CalculateValue(current.Value, move, isPositive);
            State newState = new(
                newX, newY, newDir, newValue, current.Depth + 1,
                [..current.UsedEdges, edge],
                current.Path + move
            );

            if (!visited.Add(newState)) continue;

            queue.Enqueue(newState, Math.Abs(newValue - target));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetMoveIndex(char move) {
        return move switch {
            'a' => 0,
            'q' => 1,
            'w' => 2,
            'e' => 3,
            'd' => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(move), move, null)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float CalculateValue(float current, char move, bool isPositive) {
        return move switch {
            'w' => current + (isPositive ? 1 : -1),
            'q' => current + (isPositive ? 5 : -5),
            'e' => current + (isPositive ? 10 : -10),
            'a' => current * 2,
            'd' => current / 2,
            _ => throw new ArgumentOutOfRangeException(nameof(move), move, null)
        };
    }
}