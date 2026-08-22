namespace BounceTales;

public struct AABB(int minX, int minY, int maxX, int maxY)
{
    public readonly int Width => MaxX - MinX;
    public readonly int Height => MaxY - MinY;

    // TODO: Remove individual fields and just use Vector2I
    public Vector2I Min
    {
        readonly get => new(MinX, MinY);
        set
        {
            MinX = value.X;
            MinY = value.Y;
        }
    }

    public Vector2I Max
    {
        readonly get => new(MaxX, MaxY);
        set
        {
            MaxX = value.X;
            MaxY = value.Y;
        }
    }

    public int MinX = minX; // 0
    public int MinY = minY; // 1

    public int MaxX = maxX; // 2
    public int MaxY = maxY; // 3

    public AABB(Vector2I min, Vector2I max) : this(min.X, min.Y, max.X, max.Y)
    {
    }

    // Moved from GameObject
    public readonly bool ContainsPoint(Vector2I point)
    {
        return point.X >= MinX && point.X <= MaxX && point.Y >= MinY && point.Y <= MaxY;
    }

    // Moved from GameObject
    // ABBB/segment intersection?
    public readonly bool CheckBoundCross(Vector2I start, Vector2I end)
    {
        long dimX = MaxX - MinX;
        long dimY = MaxY - MinY;
        long dimX2 = end.X - start.X;
        long dimY2 = end.Y - start.Y;
        long j5 = start.X + end.X - MinX - MaxX;
        long j6 = start.Y + end.Y - MinY - MaxY;
        long dimX2Abs = dimX2 > 0 ? dimX2 : -dimX2;
        if ((j5 > 0 ? j5 : -j5) > dimX + dimX2Abs)
            return false;
        long dimY2Abs = dimY2 > 0 ? dimY2 : -dimY2;
        if ((j6 > 0 ? j6 : -j6) > dimY + dimY2Abs)
            return false;
        long j9 = j5 * dimY2 - j6 * dimX2;
        if (j9 <= 0)
            j9 = -j9;
        return j9 <= dimX * dimY2Abs + dimY * dimX2Abs;
    }

    // Moved from GameObject
    public static bool Intersects(AABB a, AABB b)
    {
        return a.MaxX >= b.MinX && a.MinX <= b.MaxX && a.MaxY >= b.MinY && a.MinY <= b.MaxY;
    }
}
