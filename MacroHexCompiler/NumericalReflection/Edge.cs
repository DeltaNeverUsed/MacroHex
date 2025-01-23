namespace MacroHexCompiler.NumericalReflection;

public readonly struct Edge : IEquatable<Edge> {
    public readonly int X1;
    public readonly int Y1;
    public readonly int X2;
    public readonly int Y2;

    public Edge(int x1, int y1, int x2, int y2) {
        if (x1 < x2 || (x1 == x2 && y1 <= y2)) {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }
        else {
            X1 = x2;
            Y1 = y2;
            X2 = x1;
            Y2 = y1;
        }
    }

    public bool Equals(Edge other) {
        return X1 == other.X1 && Y1 == other.Y1 && X2 == other.X2 && Y2 == other.Y2;
    }

    public override int GetHashCode() {
        return HashCode.Combine(X1, Y1, X2, Y2);
    }
}