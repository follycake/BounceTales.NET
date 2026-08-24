using System.Diagnostics;
using BigGustave;

namespace BounceTales.Microedition.Lcdui;

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

    public static Image CreateImage(ReadOnlySpan<Color32> data, int width, int height)
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

            Color32[] data = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Pixel pixel = png.GetPixel(x, y);
                    data[y * w + x] = new Color32
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
    public abstract void GetRGB(Span<Color32> rgbData);

    public void SavePng(Stream stream)
    {
        Color32[] data = new Color32[Width * Height];
        GetRGB(data);

        PngBuilder builder = PngBuilder.Create(Width, Height, true);
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Color32 color = data[x + y * Width];
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
