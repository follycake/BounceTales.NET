using System.Diagnostics;
using BigGustave;

namespace BounceTales.Microedition.Lcdui;

public struct Color(byte r, byte g, byte b, byte a = 255) : IEquatable<Color>
{
    public static readonly Color Zero = new(0, 0, 0, 0);

    public byte R = r;
    public byte G = g;
    public byte B = b;
    public byte A = a;

    public Color(float r, float g, float b, float a) : this((byte)MathF.Round(r / 255f), (byte)MathF.Round(g / 255f), (byte)MathF.Round(b / 255f), (byte)MathF.Round(a / 255f))
    {
    }

    public readonly int ToARGB()
    {
        return A << 24 | R << 16 | G << 8 | B;
    }

    public static Color FromRGB(int rgb, byte a = 255)
    {
        return new Color((byte)((rgb & 0xFF0000) >> 16), (byte)((rgb & 0x00FF00) >> 8), (byte)(rgb & 0x0000FF), a);
    }

    // TODO: Reduce number of calls to FromARGB. Translate hex to the Color constructor.
    public static Color FromARGB(int argb)
    {
        return new Color((byte)((argb & 0xFF0000) >> 16), (byte)((argb & 0x00FF00) >> 8), (byte)(argb & 0x0000FF), (byte)((argb & 0xFF000000) >> 24));
    }

    public static Color FromARGB(uint argb)
    {
        return new Color((byte)((argb & 0xFF0000) >> 16), (byte)((argb & 0x00FF00) >> 8), (byte)(argb & 0x0000FF), (byte)((argb & 0xFF000000) >> 24));
    }

    public static Color AlphaBlend(Color a, Color b)
    {
        float t = b.A / 255f;
        float invT = 1f - t;
        return new Color((byte)MathF.Round(a.R * invT + b.R * t), (byte)MathF.Round(a.G * invT + b.G * t), (byte)MathF.Round(a.B * invT + b.B * t), (byte)Math.Clamp(a.A + b.A, 0, 255));
    }

    public static Color Subtract(Color a, Color b)
    {
        return new Color((byte)(a.R - b.R), (byte)(a.G - b.G), (byte)(a.B - b.B), (byte)(a.A - b.A));
    }

    public override readonly bool Equals(object obj)
    {
        return obj is Color color && Equals(color);
    }

    public readonly bool Equals(Color other)
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

    public static bool operator ==(Color left, Color right) => left.Equals(right);
    public static bool operator !=(Color left, Color right) => !(left == right);
}

public abstract class Image : IDisposable
{
    internal static int notDisposedCount;

    public abstract int Width { get; }
    public abstract int Height { get; }

    private bool _disposed;

    protected Image()
    {
        Interlocked.Increment(ref notDisposedCount);
    }

    public static Image CreateImage(ReadOnlySpan<Color> data, int width, int height)
    {
        return GameRuntime.MidLet.Graphics.CreateImage(data, width, height);
    }

    public static Image CreateRenderImage(int width, int height)
    {
        return GameRuntime.MidLet.Graphics.CreateRenderImage(width, height);
    }

    public static Image CreateImage(Stream stream)
    {
        try
        {
            Png png = Png.Open(stream);
            int w = png.Width;
            int h = png.Height;

            Color[] data = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Pixel pixel = png.GetPixel(x, y);
                    data[y * w + x] = new Color
                    {
                        R = pixel.R,
                        G = pixel.G,
                        B = pixel.B,
                        A = pixel.A
                    };
                }
            }
            return CreateImage(data, w, h);
        }
        catch (IOException)
        {
            Trace.WriteLine("Unexpected IOException in Image.CreateImage()");
            throw;
        }
    }

    public static Image CreateImage(byte[] imageData, int imageOffset, int imageLength)
    {
        using MemoryStream stream = new(imageData, imageOffset, imageLength, false);
        return CreateImage(stream);
    }

    public static Image CreateImage(string name)
    {
        if (!name.StartsWith('/'))
            name = "/" + name;
        using Stream stream = GameRuntime.MidLet.System.GetResourceAsStream(name);
        return CreateImage(stream);
    }

    public virtual Graphics GetGraphics() => throw new NotImplementedException("This Image object doesn't support graphics.");

    // Should always copy, not reference.
    public abstract void GetRGB(Span<Color> rgbData);

    public void SavePng(Stream stream)
    {
        Color[] data = new Color[Width * Height];
        GetRGB(data);

        PngBuilder builder = PngBuilder.Create(Width, Height, true);
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Color color = data[x + y * Width];
                builder.SetPixel(new Pixel(color.R, color.G, color.B, color.A, false), x, y);
            }
        }
        builder.Save(stream);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
                Interlocked.Decrement(ref notDisposedCount);
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    ~Image()
    {
        Dispose(disposing: false);
    }
}
