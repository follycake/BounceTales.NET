using BounceTales.Microedition.Lcdui;
using Rl = Raylib_cs;

namespace BounceTales.Raylib;

public unsafe class RaylibRenderImage : RaylibImage
{
    internal Rl.RenderTexture2D _renderTexture;

    private RaylibRenderImage(Rl.RenderTexture2D renderTexture) : base(renderTexture.Texture)
    {
        _renderTexture = renderTexture;
    }

    public RaylibRenderImage(int width, int height) : this(Rl.Raylib.LoadRenderTexture(width, height))
    {
    }

    public override Graphics GetGraphics()
    {
        return new RaylibGraphics(this);
    }

    public override void GetRGB(Span<Color32> rgbData)
    {
        if (rgbData.Length < Width * Height)
            throw new ArgumentException("Destination is too small.", nameof(rgbData));
        Rl.Image temp = Rl.Raylib.LoadImageFromTexture(_renderTexture.Texture);
        Rl.Raylib.ImageFlipVertical(ref temp);
        new ReadOnlySpan<Color32>(temp.Data, temp.Width * temp.Height).CopyTo(rgbData);
        Rl.Raylib.UnloadImage(temp);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Rl.Raylib.UnloadRenderTexture(_renderTexture);
        _renderTexture.Id = 0;
    }
}

public unsafe class RaylibImage : Image
{
    public override int Width => _texture.Width;
    public override int Height => _texture.Height;
    internal Rl.Texture2D _texture;

    protected RaylibImage(Rl.Texture2D texture)
    {
        _texture = texture;
    }

    public RaylibImage(ReadOnlySpan<Color32> data, int width, int height)
    {
        fixed (Color32* ptr = data)
        {
            Rl.Image image = new()
            {
                Width = width,
                Height = height,
                Format = Rl.PixelFormat.UncompressedR8G8B8A8,
                Mipmaps = 1,
                Data = ptr
            };
            _texture = Rl.Raylib.LoadTextureFromImage(image);
        }
    }

    public override void GetRGB(Span<Color32> rgbData)
    {
        if (rgbData.Length < Width * Height)
            throw new ArgumentException("Destination is too small.", nameof(rgbData));
        Rl.Image temp = Rl.Raylib.LoadImageFromTexture(_texture);
        new ReadOnlySpan<Color32>(temp.Data, temp.Width * temp.Height).CopyTo(rgbData);
        Rl.Raylib.UnloadImage(temp);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Rl.Raylib.UnloadTexture(_texture);
        _texture.Id = 0;
    }
}
