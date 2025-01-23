using System.Collections.Immutable;

namespace MacroHexCompiler.NumericalReflection;

public readonly struct State : IEquatable<State> {
    public readonly int X;
    public readonly int Y;
    public readonly int Direction;
    public readonly float Value;
    public readonly int Depth;
    public readonly ImmutableHashSet<Edge> UsedEdges;
    public readonly ImmutableArray<byte> Moves;

    public State(int x, int y, int dir, float val, int depth,
        ImmutableHashSet<Edge> edges, ImmutableArray<byte> moves) {
        X = x;
        Y = y;
        Direction = dir;
        Value = val;
        Depth = depth;
        UsedEdges = edges;
        Moves = moves;
    }

    public bool Equals(State other) {
        return X == other.X && Y == other.Y &&
               Direction == other.Direction &&
               Value.Equals(other.Value);
    }

    public override int GetHashCode() {
        return HashCode.Combine(X, Y, Direction, Value);
    }
}