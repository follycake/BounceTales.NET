namespace BounceTales;

public struct Matrix
{
    public static readonly Matrix Identity = new()
    {
        M00 = LP32.ONE,
        M11 = LP32.ONE
    };

    public Vector2I Translation
    {
        readonly get => new(TranslationX, TranslationY);
        set
        {
            TranslationX = value.X;
            TranslationY = value.Y;
        }
    }

    public int M00;
    public int M01;
    public int TranslationX;
    public int M10;
    public int M11;
    public int TranslationY;

    public static void MultMatrices(Matrix a, Matrix b, out Matrix dest)
    {
        int _m01 = (int)(a.M00 * (long)b.M01 + a.M01 * (long)b.M11 >> 16);
        int _m10 = (int)(a.M10 * (long)b.M00 + a.M11 * (long)b.M10 >> 16);
        int _m00 = (int)(a.M00 * (long)b.M00 + a.M01 * (long)b.M10 >> 16);
        int _m11 = (int)(a.M10 * (long)b.M01 + a.M11 * (long)b.M11 >> 16);
        int _tx = (int)((a.M00 * (long)b.TranslationX + a.M01 * (long)b.TranslationY >> 16) + a.TranslationX);
        int _ty = (int)((a.M10 * (long)b.TranslationX + a.M11 * (long)b.TranslationY >> 16) + a.TranslationY);
        dest.M00 = _m00;
        dest.M01 = _m01;
        dest.TranslationX = _tx;
        dest.M10 = _m10;
        dest.M11 = _m11;
        dest.TranslationY = _ty;
    }

    public void SetRotation(float f)
    {
        int cos = LP32.FP64ToLP32(Math.Cos(f));
        int sin = LP32.FP64ToLP32(Math.Sin(f));
        M00 = cos;
        M01 = -sin;
        M10 = sin;
        M11 = cos;
    }

    public void SetScale(int sx, int sy)
    {
        M00 = sx;
        M11 = sy;
    }

    public readonly Vector2I MulVector(Vector2I v)
    {
        return MulVector(v.X, v.Y);
    }

    // TODO: Integrate Vector2I better.
    public readonly Vector2I MulVector(int x, int y)
    {
        return new Vector2I(
            (int)(M00 * (long)x + M01 * (long)y >> 16) + TranslationX,
            (int)(M10 * (long)x + M11 * (long)y >> 16) + TranslationY
        );
    }

    public readonly Vector2I MulDirection(Vector2I v)
    {
        return MulDirection(v.X, v.Y);
    }

    // TODO: Integrate Vector2I better.
    public readonly Vector2I MulDirection(int x, int y)
    {
        return new Vector2I(
            (int)(M00 * (long)x + M01 * (long)y >> 16),
            (int)(M10 * (long)x + M11 * (long)y >> 16)
        );
    }

    public void Mul(in Matrix other)
    {
        int _m01 = (int)(M00 * (long)other.M01 + M01 * (long)other.M11 >> 16);
        int _m10 = (int)(M10 * (long)other.M00 + M11 * (long)other.M10 >> 16);
        int _m00 = (int)(M00 * (long)other.M00 + M01 * (long)other.M10 >> 16);
        int _m11 = (int)(M10 * (long)other.M01 + M11 * (long)other.M11 >> 16);
        int _tx = (int)((M00 * (long)other.TranslationX + M01 * (long)other.TranslationY >> 16) + TranslationX);
        int _ty = (int)((M10 * (long)other.TranslationX + M11 * (long)other.TranslationY >> 16) + TranslationY);
        M01 = _m01;
        M10 = _m10;
        M00 = _m00;
        M11 = _m11;
        TranslationX = _tx;
        TranslationY = _ty;
    }

    public readonly void Invert(out Matrix dest)
    {
        long j = M00 * (long)M11 - M01 * (long)M10 >> 16;
        if (j != 0)
        {
            dest.M00 = (int)(((long)M11 << 16) / j);
            dest.M01 = (int)(((long)M10 << 16) / j);
            dest.M10 = (int)(((long)M01 << 16) / j);
            dest.M11 = (int)(((long)M00 << 16) / j);
            dest.TranslationX = -(int)(TranslationX * (long)dest.M00 + TranslationY * (long)dest.M01 >> 16);
            dest.TranslationY = -(int)(TranslationX * (long)dest.M10 + TranslationY * (long)dest.M11 >> 16);
        }
        else
            throw new ArithmeticException("Non-invertible matrix.");
    }

    public override readonly string ToString()
    {
        return M00 + " " + M01 + " " + TranslationX + "\n" + M10 + " " + M11 + " " + TranslationY;
    }
}
