namespace MacroHexCompiler.NumericalReflection;

public readonly struct State : IEquatable<State> {
    public readonly int X;
    public readonly int Y;
    public readonly int Direction;
    public readonly float Value;
    public readonly int Depth;
    public readonly HashSet<Edge> UsedEdges;
    public readonly string Path;

    public State(int x, int y, int dir, float val, int depth,
        HashSet<Edge> edges, string path) {
        X = x;
        Y = y;
        Direction = dir;
        Value = val;
        Depth = depth;
        UsedEdges = edges;
        Path = path;
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