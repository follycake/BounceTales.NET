using BounceTales.Microedition.Lcdui;
using System;
using System.Runtime.InteropServices;
using Gd = Godot;

namespace BounceTales.Godot;

public class GodotRenderImage : GodotImage
{
    internal readonly Gd.Viewport _viewport;
    internal readonly Gd.Node2D _canvas;
    private readonly bool _ownsViewport;
    private GodotGraphics _graphics;
    
    public GodotRenderImage(Gd.Viewport viewport, Gd.Node2D canvas, bool ownsViewport) : base(viewport.GetTexture())
    {
        _viewport = viewport;
        _canvas = canvas;
        _ownsViewport = ownsViewport;
        Game.PreUpdate += PreUpdate;
    }

    private void PreUpdate()
    {
        _graphics?.Clear();
    }

    public override Graphics GetGraphics()
    {
        _graphics ??= new GodotGraphics(this);
        return _graphics;
    }

    protected override void Dispose(bool disposing)
    {
        Game.PreUpdate -= PreUpdate;
        if (disposing)
        {
            _graphics?.Clear();
            if (_ownsViewport)
            {
                _viewport.GetParent().RemoveChild(_viewport);
                _viewport.Dispose();
            }
        }
        _graphics = null;
        base.Dispose(disposing);
    }
}

public class GodotImage(Gd.Texture2D texture) : Image
{
    public override int Width => _texture.GetWidth();
    public override int Height => _texture.GetHeight();
    internal readonly Gd.Texture2D _texture = texture;
    
    public override void GetRGB(Span<Color32> rgbData)
    {
        using Gd.Image image = _texture.GetImage();
        if (image.GetFormat() != Gd.Image.Format.Rgba8)
            image.Convert(Gd.Image.Format.Rgba8);
        image.GetData().CopyTo(MemoryMarshal.AsBytes(rgbData));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _texture.Dispose();
        base.Dispose(disposing);
    }
}
