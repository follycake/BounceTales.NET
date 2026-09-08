using System.Runtime.InteropServices;

namespace BounceTales.Microedition.Lcdui;

[StructLayout(LayoutKind.Sequential)]
public struct Color32(byte r, byte g, byte b, byte a = 255) : IEquatable<Color32>
{
    public static readonly Color32 Zero = new(0, 0, 0, 0);
    public static readonly Color32 Black = new(0, 0, 0);
    public static readonly Color32 Blue = new(0, 0, 255);

    public byte R = r;
    public byte G = g;
    public byte B = b;
    public byte A = a;

    public Color32(float r, float g, float b, float a) : this((byte)MathF.Round(r / 255f), (byte)MathF.Round(g / 255f), (byte)MathF.Round(b / 255f), (byte)MathF.Round(a / 255f))
    {
    }

    public readonly int ToARGB()
    {
        return A << 24 | R << 16 | G << 8 | B;
    }

    public static Color32 FromRGB(int rgb, byte a = 255)
    {
        return new Color32((byte)((rgb & 0xFF0000) >> 16), (byte)((rgb & 0x00FF00) >> 8), (byte)(rgb & 0x0000FF), a);
    }
    
    public static Color32 FromARGB(int argb)
    {
        return new Color32((byte)((argb & 0xFF0000) >> 16), (byte)((argb & 0x00FF00) >> 8), (byte)(argb & 0x0000FF), (byte)((argb & 0xFF000000) >> 24));
    }

    public static Color32 FromARGB(uint argb)
    {
        return new Color32((byte)((argb & 0xFF0000) >> 16), (byte)((argb & 0x00FF00) >> 8), (byte)(argb & 0x0000FF), (byte)((argb & 0xFF000000) >> 24));
    }

    public static Color32 AlphaBlend(Color32 a, Color32 b)
    {
        float t = b.A / 255f;
        float invT = 1f - t;
        return new Color32((byte)MathF.Round(a.R * invT + b.R * t), (byte)MathF.Round(a.G * invT + b.G * t), (byte)MathF.Round(a.B * invT + b.B * t), (byte)Math.Clamp(a.A + b.A, 0, 255));
    }

    public static Color32 Subtract(Color32 a, Color32 b)
    {
        return new Color32((byte)(a.R - b.R), (byte)(a.G - b.G), (byte)(a.B - b.B), (byte)(a.A - b.A));
    }

    public override readonly bool Equals(object obj)
    {
        return obj is Color32 color && Equals(color);
    }

    public readonly bool Equals(Color32 other)
    {
        return R == other.R && G == other.G && B == other.B && A == other.A;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(R, G, B, A);
    }

    public override readonly string ToString()
    {
        return $"Color({R}, {G}, {B}, {A})";
    }

    public static bool operator ==(Color32 left, Color32 right) => left.Equals(right);
    public static bool operator !=(Color32 left, Color32 right) => !(left == right);
}
