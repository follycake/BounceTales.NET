using System.Numerics;

namespace BounceTales;

public struct Vector2I(int x, int y) : IEquatable<Vector2I>
{
    public static readonly Vector2I Zero = new(0, 0);

    public int X = x;
    public int Y = y;

    public static Vector2I Min(Vector2I a, Vector2I b) => new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
    public static Vector2I Max(Vector2I a, Vector2I b) => new(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

    public override readonly bool Equals(object obj)
    {
        return obj is Vector2I i && Equals(i);
    }

    public readonly bool Equals(Vector2I other)
    {
        return X == other.X && Y == other.Y;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public override readonly string ToString()
    {
        return $"Vector2I({X}, {Y})";
    }

    public static bool operator ==(Vector2I left, Vector2I right) => left.Equals(right);
    public static bool operator !=(Vector2I left, Vector2I right) => !(left == right);

    public static Vector2I operator <<(Vector2I a, int b) => new(a.X << b, a.Y << b);
    public static Vector2I operator >>(Vector2I a, int b) => new(a.X >> b, a.Y >> b);

    public static Vector2I operator +(Vector2I a, Vector2I b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2I operator -(Vector2I a, Vector2I b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2I operator *(Vector2I a, int b) => new(a.X * b, a.Y * b);

    public static Vector2 operator *(Vector2I a, float b) => new(a.X * b, a.Y * b);
    public static implicit operator Vector2(Vector2I a) => new(a.X, a.Y);
}
