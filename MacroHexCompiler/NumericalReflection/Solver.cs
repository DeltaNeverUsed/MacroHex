using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MacroHexCompiler.NumericalReflection;

public static class NumberSolver {
    private static Dictionary<float, (string, float)> _cache = new();
    
    private const int MaxDepth = 30;
    private const float Precision = 0.001f;

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

    public static (string Path, bool Exact, float diff) Solve(float target, float timeout) {
        Stopwatch timer = Stopwatch.StartNew();
        
        if (_cache.TryGetValue(target, out (string, float) value)) {
            Compiler.VerbosePrint("Number Cache hit");
            return (value.Item1, value.Item2 == 0f, value.Item2);
        }

        bool isPositive = target >= 0;
        State initial = GenerateInitialState(isPositive);
        (ImmutableArray<byte> Moves, float Diff, bool Exact) best = (initial.Moves, Diff: Math.Abs(target), Exact: false);

        PriorityQueue<State, float> queue = new();
        HashSet<State> visited = [];

        queue.Enqueue(initial, best.Diff);

        while (queue.Count > 0) {
            State current = queue.Dequeue();

            if (UpdateBest(ref best, current, target))
                if (best.Exact)
                    break;
            if (timer.Elapsed.TotalSeconds >= timeout)
                break;

            if (current.Depth >= MaxDepth) continue;

            ExploreMoves(current, isPositive, target, queue, visited);
        }

        string resultPath = MovesToString(best.Moves);
        _cache.TryAdd(target, (resultPath, best.Diff));
        return (resultPath, best.Exact, best.Diff);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static State GenerateInitialState(bool isPositive) {
        ImmutableHashSet<Edge>.Builder edgesBuilder = ImmutableHashSet.CreateBuilder<Edge>();
        string moves = isPositive ? "aqaa" : "dedd";
        int x = 0, y = 0, dir = isPositive ? 2 : 4;
        ImmutableArray<byte>.Builder movesBuilder = ImmutableArray.CreateBuilder<byte>(moves.Length);

        foreach (char move in moves) {
            int moveIndex = GetMoveIndex(move);
            int newDir = DirectionTransitions[dir][moveIndex];
            (int dx, int dy) = Neighbors[newDir];
            int newX = x + dx;
            int newY = y + dy;

            edgesBuilder.Add(new Edge(x, y, newX, newY));
            movesBuilder.Add((byte)moveIndex);

            x = newX;
            y = newY;
            dir = newDir;
        }

        return new State(
            x, y, dir,
            0, 0,
            edgesBuilder.ToImmutable(),
            movesBuilder.ToImmutable()
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool UpdateBest(ref (ImmutableArray<byte> Moves, float Diff, bool Exact) best,
        State current, float target) {
        float diff = Math.Abs(current.Value - target);
        if (diff >= best.Diff) return false;

        best.Moves = current.Moves;
        best.Diff = diff;
        best.Exact = diff < Precision;
        return true;
    }
    
    private static float GetHeuristic(float target, State state) {
        float current = state.Value;
        float absTarget = Math.Abs(target);
        float absCurrent = Math.Abs(current);
        float diff = Math.Abs(absTarget - absCurrent);

        if (diff < Precision) return 0f;

        // Handle zero value case using optimal additive steps
        if (current == 0f) {
            int steps = (int)(absTarget / 10);  // 'e' steps
            float remainder = absTarget % 10;
            steps += (int)(remainder / 5);      // 'q' steps
            remainder %= 5;
            steps += (int)Math.Ceiling(remainder); // 'w' steps
            return steps;
        }

        // Calculate multiplicative steps to reach same order of magnitude
        int multSteps = 0;
        float scaled = absCurrent;
        bool targetLarger = absTarget > scaled;

        // Scale up/down until within [0.5 * target, 2 * target]
        while ((targetLarger && scaled < absTarget / 2) || 
               (!targetLarger && scaled > absTarget * 2)) {
            if (targetLarger) {
                scaled *= 2;
            } else {
                scaled /= 2;
            }

            multSteps++;
        }

        // Calculate remaining additive steps
        float remaining = Math.Abs(absTarget - scaled);
        int addSteps = (int)Math.Ceiling(remaining / 10); // Use the largest additive step

        return multSteps + addSteps;
    }

    private static void ExploreMoves(State current, bool isPositive, float target,
        PriorityQueue<State, float> queue,
        HashSet<State> visited) {
        foreach (char move in new[] { 'a', 'q', 'w', 'e', 'd' }) {
            int moveIndex = GetMoveIndex(move);
            int newDir = DirectionTransitions[current.Direction][moveIndex];
            (int dx, int dy) = Neighbors[newDir];
            int newX = current.X + dx;
            int newY = current.Y + dy;
            Edge edge = new(current.X, current.Y, newX, newY);

            if (current.UsedEdges.Contains(edge)) continue;

            float newValue = CalculateValue(current.Value, move, isPositive);
            ImmutableHashSet<Edge> newEdges = current.UsedEdges.Add(edge);
            
            State newState = new(
                newX, newY, newDir,
                newValue, current.Depth + 1,
                newEdges,
                current.Moves.Add((byte)moveIndex)
            );

            if (!visited.Add(newState)) continue;

            queue.Enqueue(newState, GetHeuristic(target, newState));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string MovesToString(ImmutableArray<byte> moves) {
        char[] chars = new char[moves.Length];
        for (int i = 0; i < moves.Length; i++) {
            chars[i] = moves[i] switch {
                0 => 'a', 1 => 'q', 2 => 'w', 3 => 'e', 4 => 'd',
                _ => '?'
            };
        }
        return new string(chars);
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